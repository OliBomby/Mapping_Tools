using System.Reflection;
using Mapping_Tools.Core.Settings.Models;
using Tmds.DBus.Protocol;

namespace Mapping_Tools.Infrastructure.Platform;

internal sealed class XdgGlobalShortcutPortal : IGlobalShortcutPortal
{
    private const string destination = "org.freedesktop.portal.Desktop";
    private const string desktop_path = "/org/freedesktop/portal/desktop";
    private const string shortcuts_interface = "org.freedesktop.portal.GlobalShortcuts";

    internal static bool IsKdeHost =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FLATPAK_ID")) &&
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAP")) &&
        Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")?.Split(':')
            .Contains("KDE", StringComparer.OrdinalIgnoreCase) == true;

    internal static async Task<bool> IsAvailableAsync(string? busAddress, TimeSpan timeout)
    {
        if (string.IsNullOrEmpty(busAddress)) return false;

        try
        {
            using CancellationTokenSource cancellation = new(timeout);
            using DBusConnection connection = new(busAddress);
            using CancellationTokenRegistration registration = cancellation.Token.Register(connection.Dispose);
            await connection.ConnectAsync().AsTask().WaitAsync(cancellation.Token).ConfigureAwait(false);
            MessageBuffer call;
            {
                using var writer = connection.GetMessageWriter();
                writer.WriteMethodCallHeader(destination, desktop_path, "org.freedesktop.DBus.Properties", "Get", "ss");
                writer.WriteString(shortcuts_interface);
                writer.WriteString("version");
                call = writer.CreateMessage();
            }

            uint version = await connection.CallMethodAsync(call,
                static (message, _) => message.GetBodyReader().ReadVariantValue().GetUInt32())
                .WaitAsync(cancellation.Token).ConfigureAwait(false);
            return version >= 1;
        }
        catch (Exception)
        {
            // Missing interfaces, an unreachable session bus, or a stalled portal
            // mean the caller should use the keyboard hook instead.
            return false;
        }
    }

    public async Task RunAsync(
        IReadOnlyDictionary<string, string?> shortcuts,
        IReadOnlyDictionary<string, HotkeySettings> updates,
        Action<string> activated,
        Action<Dictionary<string, HotkeySettings>, Func<CancellationToken, Task<Dictionary<string, HotkeySettings>>>> registered,
        CancellationToken cancellationToken)
    {
        using DBusConnection connection = new(DBusAddress.Session ??
            throw new InvalidOperationException("The desktop session bus is unavailable."));
        using CancellationTokenRegistration cancellation = cancellationToken.Register(connection.Dispose);
        await connection.ConnectAsync().ConfigureAwait(false);
        await RegisterApplicationAsync(connection).ConfigureAwait(false);
        if (IsKdeHost)
            await KdeGlobalShortcuts.PrepareAsync(connection, shortcuts.Keys, updates).ConfigureAwait(false);

        var created = await RequestAsync(connection, "CreateSession", "a{sv}", null, shortcuts, cancellationToken)
            .ConfigureAwait(false);
        // The portal specifies this object path as a string for backwards compatibility.
        string session = created["session_handle"].GetString();
        TaskCompletionSource disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using IDisposable subscription = await connection.AddMatchAsync(
            new MatchRule
            {
                Type = MessageType.Signal,
                Sender = destination,
                Path = desktop_path,
                Interface = shortcuts_interface,
                Member = "Activated",
            },
            static (message, _) =>
            {
                var reader = message.GetBodyReader();
                return (Session: reader.ReadObjectPath().ToString(), Id: reader.ReadString());
            },
            notification =>
            {
                if (notification.IsCompletion) disconnected.TrySetException(notification.Exception);
                else if (notification.HasValue && notification.Value.Session == session)
                    activated(notification.Value.Id);
            },
            emitOnCapturedContext: false,
            flags: ObserverFlags.EmitOnConnectionClosed).ConfigureAwait(false);

        var bound = await RequestAsync(connection, "BindShortcuts", "oa(sa{sv})sa{sv}", session, shortcuts, cancellationToken)
            .ConfigureAwait(false);
        registered(ReadShortcuts(bound), async token =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken);
            linked.Token.ThrowIfCancellationRequested();
            // This reader belongs to the live session. Session cancellation stops stale
            // reads, and a concurrent disconnect is reported by the hotkey service.
            // ReSharper disable once AccessToDisposedClosure
            var listed = await RequestAsync(connection, "ListShortcuts", "oa{sv}", session, shortcuts, linked.Token)
                .ConfigureAwait(false);
            return ReadShortcuts(listed);
        });
        await disconnected.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        // Disposing this private connection closes its sessions and unregisters the shortcuts.
    }

    internal static Dictionary<string, HotkeySettings> ReadShortcuts(Dictionary<string, VariantValue> results)
    {
        VariantValue shortcuts = results["shortcuts"];
        Dictionary<string, HotkeySettings> assignments = new(StringComparer.Ordinal);
        for (int index = 0; index < shortcuts.Count; index++)
        {
            VariantValue shortcut = shortcuts.GetItem(index);
            var properties = shortcut.GetItem(1).GetDictionary<string, VariantValue>();
            string description = properties.TryGetValue("trigger_description", out var trigger)
                ? trigger.GetString()
                : string.Empty;
            assignments[shortcut.GetItem(0).GetString()] = XdgShortcutTrigger.ParseDescription(description);
        }

        return assignments;
    }

    private static async Task RegisterApplicationAsync(DBusConnection connection)
    {
        // Sandboxed apps already have an identity. Host apps need a stable identity
        // even when launched from a terminal; it matches Velopack's desktop file.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FLATPAK_ID")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAP"))) return;

        EnsureDesktopEntry();
        MessageBuffer message;
        {
            using var writer = connection.GetMessageWriter();
            writer.WriteMethodCallHeader(destination, desktop_path,
                "org.freedesktop.host.portal.Registry", "Register", "sa{sv}");
            writer.WriteString("MappingTools");
            writer.WriteDictionary(new Dictionary<string, VariantValue>());
            message = writer.CreateMessage();
        }
        try
        {
            await connection.CallMethodAsync(message).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException exception) when (exception.ErrorName is
            "org.freedesktop.DBus.Error.UnknownMethod" or "org.freedesktop.DBus.Error.UnknownInterface")
        {
            // Older portals identify host applications automatically.
        }
    }

    private static void EnsureDesktopEntry()
    {
        string dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME") ??
                          Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        string dataDirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS") ?? "/usr/local/share:/usr/share";
        string executable = Environment.GetEnvironmentVariable("APPIMAGE") ?? Environment.ProcessPath ??
                            throw new InvalidOperationException("The application executable could not be located.");
        string? assembly = Path.GetFileNameWithoutExtension(executable) == "dotnet"
            ? Assembly.GetEntryAssembly()?.Location
            : null;
        LinuxDesktopEntry.EnsureInstalled(
            new[] { dataHome }.Concat(dataDirs.Split(':', StringSplitOptions.RemoveEmptyEntries)), executable, assembly,
            Path.Combine(AppContext.BaseDirectory, "Assets", "mt_logo_256.png"));
    }

    private static async Task<Dictionary<string, VariantValue>> RequestAsync(
        DBusConnection connection,
        string member,
        string signature,
        string? session,
        IReadOnlyDictionary<string, string?> shortcuts,
        CancellationToken cancellationToken)
    {
        string token = "mappingtools_" + Guid.NewGuid().ToString("N");
        string sender = connection.UniqueName!.TrimStart(':').Replace('.', '_');
        string path = $"/org/freedesktop/portal/desktop/request/{sender}/{token}";
        TaskCompletionSource<(uint Code, Dictionary<string, VariantValue> Results)> response =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Subscribe before sending the method call: a portal may respond immediately.
        using IDisposable subscription = await connection.AddMatchAsync(
            new MatchRule
            {
                Type = MessageType.Signal,
                Sender = destination,
                Path = path,
                Interface = "org.freedesktop.portal.Request",
                Member = "Response",
            },
            static (message, _) =>
            {
                var reader = message.GetBodyReader();
                return (reader.ReadUInt32(), reader.ReadDictionaryOfStringToVariantValue());
            },
            notification =>
            {
                if (notification.IsCompletion) response.TrySetException(notification.Exception);
                else if (notification.HasValue) response.TrySetResult(notification.Value);
            },
            emitOnCapturedContext: false,
            flags: ObserverFlags.EmitOnConnectionClosed).ConfigureAwait(false);

        MessageBuffer call = CreateRequest(connection, member, signature, token, session, shortcuts);
        string returnedPath = await connection.CallMethodAsync(call,
            static (message, _) => message.GetBodyReader().ReadObjectPath().ToString()).ConfigureAwait(false);
        if (returnedPath != path)
            throw new InvalidOperationException("The desktop portal returned an unexpected request handle.");

        var result = await response.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (result.Code != 0)
            throw new InvalidOperationException(result.Code == 1
                ? "Global shortcut permission was cancelled. Configure the shortcuts in the desktop's shortcut settings."
                : "The desktop portal could not register the global shortcuts.");

        return result.Results;
    }

    private static MessageBuffer CreateRequest(
        DBusConnection connection,
        string member,
        string signature,
        string token,
        string? session,
        IReadOnlyDictionary<string, string?> shortcuts)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(destination, desktop_path, shortcuts_interface, member, signature);
        Dictionary<string, VariantValue> options = new() { ["handle_token"] = token };
        if (session is null)
        {
            options["session_handle_token"] = "mappingtools_" + Guid.NewGuid().ToString("N");
        }
        else
        {
            writer.WriteObjectPath(session);
        }

        if (member == "BindShortcuts")
        {
            ArrayStart array = writer.WriteArrayStart(DBusType.Struct);
            foreach (var (id, trigger) in shortcuts)
            {
                writer.WriteStructureStart();
                writer.WriteString(id);
                Dictionary<string, VariantValue> properties = new()
                {
                    ["description"] = GetDescription(id),
                };
                if (trigger is not null) properties["preferred_trigger"] = trigger;
                writer.WriteDictionary(properties);
            }
            writer.WriteArrayEnd(array);
            writer.WriteString(""); // An unparented desktop permission dialog.
        }

        writer.WriteDictionary(options);
        return writer.CreateMessage();
    }

    internal static string GetDescription(string id)
    {
        return id.Split(':')[0] switch
        {
            "quick-run" => "QuickRun",
            "quick-undo" => "QuickUndo",
            "better-save" => "BetterSave",
            _ => id,
        };
    }
}
