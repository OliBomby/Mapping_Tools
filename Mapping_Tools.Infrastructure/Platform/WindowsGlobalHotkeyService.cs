using Mapping_Tools.ApplicationServices.QuickRun;
using Mapping_Tools.ApplicationServices.Settings;
using NonInvasiveKeyboardHookLibrary;
using HookModifierKeys = NonInvasiveKeyboardHookLibrary.ModifierKeys;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
/// Registers process-wide Windows shortcuts through the same non-invasive
/// keyboard-hook library used by the legacy frontend.
/// </summary>
/// <remarks>
/// Persisted key values originate from WPF's <c>Key</c> enum. This adapter
/// translates that stable legacy format to Win32 virtual-key values without
/// referencing WPF from Infrastructure.
/// </remarks>
public sealed class WindowsGlobalHotkeyService : IGlobalHotkeyService
{
    private readonly KeyboardHookManager _manager = new();
    private readonly Dictionary<string, Binding> _bindings =
        new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private CancellationTokenSource _stopping = new();
    private bool _started;

    /// <inheritdoc/>
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

        lock (_gate)
        {
            if (hotkey is null || hotkey.Key == 0)
            {
                _bindings.Remove(id);
            }
            else
            {
                _bindings[id] = new Binding(hotkey, callback);
            }

            if (_started)
            {
                ReloadBindings();
            }
        }
    }

    /// <inheritdoc/>
    public void Start()
    {
        lock (_gate)
        {
            if (_started)
            {
                return;
            }

            if (_stopping.IsCancellationRequested)
            {
                _stopping.Dispose();
                _stopping = new CancellationTokenSource();
            }

            ReloadBindings();
            _manager.Start();
            _started = true;
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        lock (_gate)
        {
            if (!_started)
            {
                return;
            }

            _stopping.Cancel();
            _manager.UnregisterAll();
            _manager.Stop();
            _started = false;
        }
    }

    internal static int ConvertLegacyKeyToVirtualKey(int key)
    {
        if (key is >= 34 and <= 43)
        {
            return key + 14; // WPF D0-D9 to Win32 0-9.
        }

        if (key is >= 44 and <= 69)
        {
            return key + 21; // WPF A-Z to Win32 A-Z.
        }

        if (key is >= 74 and <= 83)
        {
            return key + 22; // WPF NumPad0-NumPad9 to Win32 numpad keys.
        }

        if (key is >= 90 and <= 113)
        {
            return key + 22; // WPF F1-F24 to Win32 function keys.
        }

        return key switch
        {
            2 => 0x08,  // Backspace
            3 => 0x09,  // Tab
            6 => 0x0D,  // Enter
            13 => 0x1B, // Escape
            18 => 0x20, // Space
            19 => 0x21, // Page Up
            20 => 0x22, // Page Down
            21 => 0x23, // End
            22 => 0x24, // Home
            23 => 0x25, // Left
            24 => 0x26, // Up
            25 => 0x27, // Right
            26 => 0x28, // Down
            31 => 0x2D, // Insert
            32 => 0x2E, // Delete
            84 => 0x6A, // Multiply
            85 => 0x6B, // Add
            86 => 0x6C, // Separator
            87 => 0x6D, // Subtract
            88 => 0x6E, // Decimal
            89 => 0x6F, // Divide
            114 => 0x90, // Num Lock
            115 => 0x91, // Scroll Lock
            _ => throw new ArgumentOutOfRangeException(
                nameof(key),
                key,
                "The persisted WPF key is not supported as a global shortcut.")
        };
    }

    private void ReloadBindings()
    {
        _manager.UnregisterAll();
        foreach (Binding binding in _bindings.Values)
        {
            int virtualKey = ConvertLegacyKeyToVirtualKey(binding.Hotkey.Key);
            HookModifierKeys[] modifiers = ConvertModifiers(
                binding.Hotkey.Modifiers);
            _manager.RegisterHotkey(
                modifiers,
                virtualKey,
                () => Schedule(binding.Callback));
        }
    }

    private void Schedule(Func<CancellationToken, Task> callback)
    {
        CancellationToken cancellationToken = _stopping.Token;
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

    private static HookModifierKeys[] ConvertModifiers(int modifiers)
    {
        const int knownModifiers = 1 | 2 | 4 | 8;
        if ((modifiers & ~knownModifiers) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(modifiers),
                modifiers,
                "Only legacy Alt, Control, Shift, and Windows modifiers are supported.");
        }

        List<HookModifierKeys> result = [];
        if ((modifiers & 1) != 0)
        {
            result.Add(HookModifierKeys.Alt);
        }

        if ((modifiers & 2) != 0)
        {
            result.Add(HookModifierKeys.Control);
        }

        if ((modifiers & 4) != 0)
        {
            result.Add(HookModifierKeys.Shift);
        }

        if ((modifiers & 8) != 0)
        {
            result.Add(HookModifierKeys.WindowsKey);
        }

        return result.ToArray();
    }

    private sealed record Binding(
        HotkeySettings Hotkey,
        Func<CancellationToken, Task> Callback);
}
