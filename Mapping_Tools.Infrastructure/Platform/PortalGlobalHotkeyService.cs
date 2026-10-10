using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Core.Settings.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
///     Registers Linux shortcuts through the desktop's GlobalShortcuts portal,
///     without requiring access to raw input devices.
/// </summary>
public sealed class PortalGlobalHotkeyService : IGlobalHotkeyService, IGlobalHotkeyRegistration
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, Binding> bindings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HotkeySettings> pendingUpdates = new(StringComparer.Ordinal);
    private readonly IGlobalShortcutPortal portal;
    private readonly ILogger<PortalGlobalHotkeyService> logger;
    private readonly bool useStableIds;
    private CancellationTokenSource? listener;
    private Task worker = Task.CompletedTask;
    private bool started;
    private bool warningReported;
    private TaskCompletionSource<Func<CancellationToken, Task<Dictionary<string, HotkeySettings>>>?>? registration;

    /// <inheritdoc />
    public event EventHandler<Exception>? RegistrationFailed;

    /// <summary>Creates a listener for the current user's desktop portal.</summary>
    /// <param name="logger">Optionally records portal availability, permission, and callback failures.</param>
    public PortalGlobalHotkeyService(ILogger<PortalGlobalHotkeyService>? logger = null)
        : this(new XdgGlobalShortcutPortal(), logger ?? NullLogger<PortalGlobalHotkeyService>.Instance,
            XdgGlobalShortcutPortal.IsKdeHost)
    {
    }

    internal PortalGlobalHotkeyService(
        IGlobalShortcutPortal portal,
        ILogger<PortalGlobalHotkeyService> logger,
        bool useStableIds = true)
    {
        this.portal = portal ?? throw new ArgumentNullException(nameof(portal));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.useStableIds = useStableIds;
    }

    /// <summary>Checks whether the Linux desktop provides the GlobalShortcuts portal.</summary>
    /// <returns>Whether the portal is available, allowing up to two seconds for the desktop to respond.</returns>
    public static bool IsAvailable()
    {
        return OperatingSystem.IsLinux() && XdgGlobalShortcutPortal.IsAvailableAsync(
            Tmds.DBus.Protocol.DBusAddress.Session, TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void SetBinding(string id, HotkeySettings? hotkey, Func<CancellationToken, Task> callback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(callback);
        Binding? binding = hotkey is null || hotkey.Key == 0
            ? null
            : new Binding(hotkey, useStableIds ? id : $"{id}:{hotkey.Key}:{hotkey.Modifiers}",
                XdgShortcutTrigger.Convert(hotkey), callback);

        lock (gate)
        {
            bindings.TryGetValue(id, out Binding? previous);
            if (binding is not null && previous?.Hotkey == binding.Hotkey)
                binding = binding with { PortalId = previous.PortalId };
            if (binding is null) bindings.Remove(id);
            else bindings[id] = binding;

            if (started && useStableIds && previous?.Hotkey != binding?.Hotkey)
                pendingUpdates[id] = hotkey ?? new HotkeySettings(0, 0);

            if (started && (previous?.Hotkey != binding?.Hotkey || listener is null)) RestartListener();
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (gate)
        {
            if (started) return;
            started = true;
            RestartListener();
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (gate)
        {
            started = false;
            pendingUpdates.Clear();
            listener?.Cancel();
            listener = null;
            registration?.TrySetResult(null);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, HotkeySettings>> GetRegisteredShortcutsAsync(CancellationToken cancellationToken)
    {
        Task<Func<CancellationToken, Task<Dictionary<string, HotkeySettings>>>?>? pending;
        Dictionary<string, string> owners;
        CancellationToken sessionToken;
        lock (gate)
        {
            if (!started) throw new OperationCanceledException("The desktop shortcut listener is stopped.");
            pending = registration?.Task;
            owners = bindings.ToDictionary(pair => pair.Value.PortalId, pair => pair.Key);
            sessionToken = listener?.Token ?? default;
        }

        var read = pending is null ? null : await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, HotkeySettings> assignments = [];
        if (read is not null)
        {
            try
            {
                assignments = await read(cancellationToken).ConfigureAwait(false);
                CheckAssignments(assignments, owners.Keys, sessionToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                ReportRegistrationFailure(exception, sessionToken);
            }
        }

        lock (gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!started || registration?.Task != pending)
                throw new OperationCanceledException("The desktop shortcut session changed.");

            Dictionary<string, HotkeySettings> result = owners.ToDictionary(pair => pair.Value,
                pair => assignments.GetValueOrDefault(pair.Key, new HotkeySettings(0, 0)));
            foreach (var (owner, hotkey) in result)
            {
                // Keep the live portal ID while adopting its actual assignment, so
                // recapturing this gesture updates the callback without rebinding.
                bindings[owner] = bindings[owner] with { Hotkey = hotkey, Trigger = XdgShortcutTrigger.Convert(hotkey) };
            }

            return result;
        }
    }

    private void RestartListener()
    {
        listener?.Cancel();
        listener = null;
        registration?.TrySetResult(null);
        registration = null;
        warningReported = false;
        if (bindings.Count == 0 && pendingUpdates.Count == 0) return;

        CancellationTokenSource current = new();
        CancellationToken token = current.Token;
        listener = current;
        TaskCompletionSource<Func<CancellationToken, Task<Dictionary<string, HotkeySettings>>>?> ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        registration = ready;
        // KDE can update stable action IDs. Other portals need a new ID to offer
        // a changed preferred trigger rather than restore the previous assignment.
        var shortcuts = bindings.Values.ToDictionary(binding => binding.PortalId, binding => binding.Trigger);
        var updates = new Dictionary<string, HotkeySettings>(pendingUpdates);
        var owners = bindings.ToDictionary(pair => pair.Value.PortalId, pair => pair.Key);
        Task previous = worker;
        worker = Task.Run(async () =>
        {
            // BindShortcuts may be called only once per session. Close the old session
            // before opening its replacement, including when settings change mid-dialog.
            await previous.ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                await portal.RunAsync(shortcuts, updates, id =>
                {
                    if (owners.TryGetValue(id, out string? owner)) OnActivated(owner, token);
                }, (descriptions, read) =>
                {
                    lock (gate)
                    {
                        if (listener?.Token == token) pendingUpdates.Clear();
                    }
                    CheckAssignments(descriptions, owners.Keys, token);
                    ready.TrySetResult(read);
                }, token).ConfigureAwait(false);
            }
            catch (Exception) when (current.IsCancellationRequested)
            {
                // Cancelling also disposes the D-Bus connection and any pending dialog.
            }
            catch (Exception exception)
            {
                ReportRegistrationFailure(exception, token);
            }
            finally
            {
                lock (gate)
                {
                    current.Cancel();
                    ready.TrySetResult(null);
                    if (ReferenceEquals(listener, current)) listener = null;
                    current.Dispose();
                }
            }
        });
    }

    private void CheckAssignments(Dictionary<string, HotkeySettings> assignments, IEnumerable<string> ids,
        CancellationToken token)
    {
        if (ids.Any(id => !assignments.TryGetValue(id, out HotkeySettings? hotkey) || hotkey.Key == 0))
            ReportRegistrationFailure(new InvalidOperationException("The desktop left one or more global shortcuts unassigned."), token);
        else
        {
            lock (gate)
            {
                if (listener?.Token == token) warningReported = false;
            }
        }
    }

    private void ReportRegistrationFailure(Exception exception, CancellationToken token)
    {
        lock (gate)
        {
            if (!started || listener?.Token != token || warningReported) return;
            warningReported = true;
        }

        logger.LogWarning(exception, "Global shortcuts could not be registered or read through the desktop portal.");
        RegistrationFailed?.Invoke(this, exception);
    }

    private void OnActivated(string id, CancellationToken token)
    {
        Func<CancellationToken, Task> callback;
        lock (gate)
        {
            if (!started || listener is null || listener.Token != token ||
                !bindings.TryGetValue(id, out Binding? binding)) return;

            callback = binding.Callback;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await callback(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // A queued shortcut was removed or the application is closing.
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Global shortcut {Shortcut} failed", id);
            }
        });
    }

    private sealed record Binding(
        HotkeySettings Hotkey,
        string PortalId,
        string? Trigger,
        Func<CancellationToken, Task> Callback);
}
