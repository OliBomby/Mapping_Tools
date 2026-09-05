using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Settings.Models;
using NonInvasiveKeyboardHookLibrary;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
///     Registers process-wide Windows shortcuts through the same non-invasive
///     keyboard-hook library used by the legacy frontend.
/// </summary>
/// <remarks>
///     Persisted key values originate from WPF's <c>Key</c> enum. This adapter
///     translates that stable legacy format to Win32 virtual-key values without
///     referencing WPF from Infrastructure.
/// </remarks>
public sealed class WindowsGlobalHotkeyService : IGlobalHotkeyService
{
    private readonly Dictionary<string, Binding> bindings =
        new(StringComparer.Ordinal);

    private readonly object gate = new();
    private readonly Func<bool> isWindows;
    private readonly KeyboardHookManager manager = new();
    private bool started;
    private CancellationTokenSource stopping = new();

    /// <summary>Creates the global hotkey adapter using the current platform guard.</summary>
    public WindowsGlobalHotkeyService()
        : this(OperatingSystem.IsWindows)
    {
    }

    internal WindowsGlobalHotkeyService(Func<bool> isWindows)
    {
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc />
    public void SetBinding(
        string id,
        HotkeySettings? hotkey,
        Func<CancellationToken, Task> callback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(callback);
        if (hotkey is not null && hotkey.Key != 0)
        {
            _ = ConvertLegacyKeyToVirtualKey(hotkey.Key);
            _ = ConvertModifiers(hotkey.Modifiers);
        }

        lock (gate)
        {
            if (hotkey is null || hotkey.Key == 0)
                bindings.Remove(id);
            else
                bindings[id] = new Binding(hotkey, callback);

            if (started) ReloadBindings();
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (gate)
        {
            if (started) return;

            if (stopping.IsCancellationRequested)
            {
                stopping.Dispose();
                stopping = new CancellationTokenSource();
            }

            if (!isWindows())
            {
                started = true;
                return;
            }

            ReloadBindings();
            manager.Start();
            started = true;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (gate)
        {
            if (!started) return;

            stopping.Cancel();
            if (isWindows())
            {
                manager.UnregisterAll();
                manager.Stop();
            }

            started = false;
        }
    }

    internal static int ConvertLegacyKeyToVirtualKey(int key)
    {
        if (key is >= 18 and <= 43) return key + 14; // WPF Space through D9 follow the Win32 sequence.

        if (key is >= 44 and <= 72) return key + 21; // WPF A through Apps follow the Win32 sequence.

        if (key is >= 74 and <= 83) return key + 22; // WPF NumPad0-NumPad9 to Win32 numpad keys.

        if (key is >= 90 and <= 113) return key + 22; // WPF F1-F24 to Win32 function keys.

        if (key is >= 116 and <= 121) return key + 44; // WPF left/right modifier keys.

        if (key is >= 122 and <= 139) return key + 44; // Browser, media, and launch keys.

        if (key is >= 140 and <= 148) return key + 46; // OEM1 through ABNT C2.

        if (key is >= 149 and <= 153) return key + 70; // OEM4 through OEM8.

        if (key is >= 157 and <= 171) return key + 83; // IME DBE keys through OEM Clear.

        return key switch
        {
            1 => 0x03, // Cancel
            2 => 0x08, // Backspace
            3 => 0x09, // Tab
            4 => 0x0A, // Line feed
            5 => 0x0C, // Clear
            6 => 0x0D, // Enter
            7 => 0x13, // Pause
            8 => 0x14, // Caps lock
            9 => 0x15, // Kana mode
            10 => 0x17, // Junja mode
            11 => 0x18, // Final mode
            12 => 0x19, // Hanja mode
            13 => 0x1B, // Escape
            14 => 0x1C, // IME convert
            15 => 0x1D, // IME non-convert
            16 => 0x1E, // IME accept
            17 => 0x1F, // IME mode change
            73 => 0x5F, // Sleep
            84 => 0x6A, // Multiply
            85 => 0x6B, // Add
            86 => 0x6C, // Separator
            87 => 0x6D, // Subtract
            88 => 0x6E, // Decimal
            89 => 0x6F, // Divide
            114 => 0x90, // Num Lock
            115 => 0x91, // Scroll Lock
            155 => 0xE5, // IME processed
            154 => 0xE2, // OEM 102
            _ => throw new ArgumentOutOfRangeException(
                nameof(key),
                key,
                "The persisted WPF key is not supported as a global shortcut."),
        };
    }

    private void ReloadBindings()
    {
        if (!isWindows()) return;

        manager.UnregisterAll();
        foreach (var binding in bindings.Values)
        {
            int virtualKey = ConvertLegacyKeyToVirtualKey(binding.Hotkey.Key);
            var modifiers = ConvertModifiers(
                binding.Hotkey.Modifiers);
            manager.RegisterHotkey(
                modifiers,
                virtualKey,
                () => Schedule(binding.Callback));
        }
    }

    private void Schedule(Func<CancellationToken, Task> callback)
    {
        var cancellationToken = stopping.Token;
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await callback(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (
                    cancellationToken.IsCancellationRequested)
                {
                    // Application shutdown cancelled a queued shortcut.
                }
                catch
                {
                    // Application command services own user-visible failure reporting.
                }
            },
            CancellationToken.None);
    }

    private static NonInvasiveKeyboardHookLibrary.ModifierKeys[] ConvertModifiers(int modifiers)
    {
        const int known_modifiers = 1 | 2 | 4 | 8;
        if ((modifiers & ~known_modifiers) != 0)
            throw new ArgumentOutOfRangeException(
                nameof(modifiers),
                modifiers,
                "Only legacy Alt, Control, Shift, and Windows modifiers are supported.");

        List<NonInvasiveKeyboardHookLibrary.ModifierKeys> result = [];
        if ((modifiers & 1) != 0) result.Add(NonInvasiveKeyboardHookLibrary.ModifierKeys.Alt);

        if ((modifiers & 2) != 0) result.Add(NonInvasiveKeyboardHookLibrary.ModifierKeys.Control);

        if ((modifiers & 4) != 0) result.Add(NonInvasiveKeyboardHookLibrary.ModifierKeys.Shift);

        if ((modifiers & 8) != 0) result.Add(NonInvasiveKeyboardHookLibrary.ModifierKeys.WindowsKey);

        return result.ToArray();
    }

    private sealed record Binding(
        HotkeySettings Hotkey,
        Func<CancellationToken, Task> Callback);
}
