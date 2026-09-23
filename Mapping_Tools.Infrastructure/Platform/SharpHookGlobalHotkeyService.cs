using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Core.Settings.Models;
using SharpHook;
using SharpHook.Data;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
///     Registers process-wide keyboard shortcuts through SharpHook on the
///     supported Windows, macOS, and Linux desktop platforms.
/// </summary>
/// <remarks>
///     Avalonia key values are converted to platform-neutral SharpHook key
///     codes at the registration boundary.
/// </remarks>
public sealed class SharpHookGlobalHotkeyService : IGlobalHotkeyService
{
    private static readonly KeyCode[] digit_keys =
    [
        KeyCode.Vc0,
        KeyCode.Vc1,
        KeyCode.Vc2,
        KeyCode.Vc3,
        KeyCode.Vc4,
        KeyCode.Vc5,
        KeyCode.Vc6,
        KeyCode.Vc7,
        KeyCode.Vc8,
        KeyCode.Vc9,
    ];

    private static readonly KeyCode[] letter_keys =
    [
        KeyCode.VcA,
        KeyCode.VcB,
        KeyCode.VcC,
        KeyCode.VcD,
        KeyCode.VcE,
        KeyCode.VcF,
        KeyCode.VcG,
        KeyCode.VcH,
        KeyCode.VcI,
        KeyCode.VcJ,
        KeyCode.VcK,
        KeyCode.VcL,
        KeyCode.VcM,
        KeyCode.VcN,
        KeyCode.VcO,
        KeyCode.VcP,
        KeyCode.VcQ,
        KeyCode.VcR,
        KeyCode.VcS,
        KeyCode.VcT,
        KeyCode.VcU,
        KeyCode.VcV,
        KeyCode.VcW,
        KeyCode.VcX,
        KeyCode.VcY,
        KeyCode.VcZ,
    ];

    private static readonly KeyCode[] function_keys =
    [
        KeyCode.VcF1,
        KeyCode.VcF2,
        KeyCode.VcF3,
        KeyCode.VcF4,
        KeyCode.VcF5,
        KeyCode.VcF6,
        KeyCode.VcF7,
        KeyCode.VcF8,
        KeyCode.VcF9,
        KeyCode.VcF10,
        KeyCode.VcF11,
        KeyCode.VcF12,
        KeyCode.VcF13,
        KeyCode.VcF14,
        KeyCode.VcF15,
        KeyCode.VcF16,
        KeyCode.VcF17,
        KeyCode.VcF18,
        KeyCode.VcF19,
        KeyCode.VcF20,
        KeyCode.VcF21,
        KeyCode.VcF22,
        KeyCode.VcF23,
        KeyCode.VcF24,
    ];

    private static readonly KeyCode[] numpad_keys =
    [
        KeyCode.VcNumPad0,
        KeyCode.VcNumPad1,
        KeyCode.VcNumPad2,
        KeyCode.VcNumPad3,
        KeyCode.VcNumPad4,
        KeyCode.VcNumPad5,
        KeyCode.VcNumPad6,
        KeyCode.VcNumPad7,
        KeyCode.VcNumPad8,
        KeyCode.VcNumPad9,
    ];

    private readonly Dictionary<string, Binding> bindings =
        new(StringComparer.Ordinal);

    private readonly Lock gate = new();
    private readonly Func<bool> isSupported;
    private IGlobalHook? hook;
    private bool started;
    private CancellationTokenSource stopping = new();

    /// <summary>Creates the global hotkey adapter using the current platform guard.</summary>
    public SharpHookGlobalHotkeyService()
        : this(IsSupportedPlatform)
    {
    }

    internal SharpHookGlobalHotkeyService(Func<bool> isSupported)
    {
        this.isSupported = isSupported ?? throw new ArgumentNullException(nameof(isSupported));
    }

    /// <inheritdoc />
    public void SetBinding(
        string id,
        HotkeySettings? hotkey,
        Func<CancellationToken, Task> callback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(callback);

        Binding? binding = null;
        if (hotkey is not null && hotkey.Key != 0)
        {
            binding = new Binding(
                ConvertKeyToSharpHookKey(hotkey.Key),
                ConvertModifiersToEventMask(hotkey.Modifiers),
                callback);
        }

        bool startHook = false;
        bool stopHook = false;
        lock (gate)
        {
            if (binding is null)
            {
                bindings.Remove(id);
                stopHook = started && bindings.Count == 0;
            }
            else
            {
                bindings[id] = binding;
                startHook = started && hook is null;
            }
        }

        if (stopHook) StopHook();
        else if (startHook) StartHook();
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

            started = true;
        }

        StartHook();
    }

    private void StartHook()
    {
        IGlobalHook? currentHook;
        lock (gate)
        {
            if (!started || hook is not null || bindings.Count == 0 || !isSupported()) return;

            currentHook = new EventLoopGlobalHook();
            currentHook.KeyPressed += OnKeyPressed;
            hook = currentHook;
        }

        try
        {
            IGlobalHook startedHook = currentHook!;
            _ = startedHook.RunAsync(GlobalHookType.Keyboard, true).ContinueWith(
                task => OnHookStopped(startedHook, task),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
        catch
        {
            OnHookStopped(currentHook!, null);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        IGlobalHook? currentHook;
        lock (gate)
        {
            if (!started && hook is null) return;

            stopping.Cancel();
            currentHook = hook;
            hook = null;
            started = false;
        }

        try
        {
            currentHook?.Dispose();
        }
        catch
        {
            // Shutdown must not prevent the application from closing.
        }
    }

    private void StopHook()
    {
        IGlobalHook? currentHook;
        lock (gate)
        {
            currentHook = hook;
            hook = null;
        }

        try
        {
            currentHook?.Dispose();
        }
        catch
        {
            // A hook being removed must not prevent settings from being saved.
        }
    }

    internal static KeyCode ConvertKeyToSharpHookKey(int key)
    {
        if (key is >= 34 and <= 43) return digit_keys[key - 34];

        if (key is >= 44 and <= 69) return letter_keys[key - 44];

        if (key is >= 74 and <= 83) return numpad_keys[key - 74];

        if (key is >= 90 and <= 113) return function_keys[key - 90];

        return key switch
        {
            1 => KeyCode.VcCancel,
            2 => KeyCode.VcBackspace,
            3 => KeyCode.VcTab,
            4 => KeyCode.VcEnter,
            5 => KeyCode.VcNumPadClear,
            6 => KeyCode.VcEnter,
            7 => KeyCode.VcPause,
            8 => KeyCode.VcCapsLock,
            9 => KeyCode.VcKana,
            10 => KeyCode.VcJunja,
            11 => KeyCode.VcFinal,
            12 => KeyCode.VcHanja,
            13 => KeyCode.VcEscape,
            14 => KeyCode.VcConvert,
            15 => KeyCode.VcNonConvert,
            16 => KeyCode.VcAccept,
            17 => KeyCode.VcModeChange,
            18 => KeyCode.VcSpace,
            19 => KeyCode.VcPageUp,
            20 => KeyCode.VcPageDown,
            21 => KeyCode.VcEnd,
            22 => KeyCode.VcHome,
            23 => KeyCode.VcLeft,
            24 => KeyCode.VcUp,
            25 => KeyCode.VcRight,
            26 => KeyCode.VcDown,
            28 or 30 => KeyCode.VcPrintScreen,
            31 => KeyCode.VcInsert,
            32 => KeyCode.VcDelete,
            33 => KeyCode.VcHelp,
            70 => KeyCode.VcLeftMeta,
            71 => KeyCode.VcRightMeta,
            72 => KeyCode.VcContextMenu,
            73 => KeyCode.VcSleep,
            84 => KeyCode.VcNumPadMultiply,
            85 => KeyCode.VcNumPadAdd,
            86 => KeyCode.VcNumPadSeparator,
            87 => KeyCode.VcNumPadSubtract,
            88 => KeyCode.VcNumPadDecimal,
            89 => KeyCode.VcNumPadDivide,
            114 => KeyCode.VcNumLock,
            115 => KeyCode.VcScrollLock,
            116 => KeyCode.VcLeftShift,
            117 => KeyCode.VcRightShift,
            118 => KeyCode.VcLeftControl,
            119 => KeyCode.VcRightControl,
            120 => KeyCode.VcLeftAlt,
            121 => KeyCode.VcRightAlt,
            122 => KeyCode.VcBrowserBack,
            123 => KeyCode.VcBrowserForward,
            124 => KeyCode.VcBrowserRefresh,
            125 => KeyCode.VcBrowserStop,
            126 => KeyCode.VcBrowserSearch,
            127 => KeyCode.VcBrowserFavorites,
            128 => KeyCode.VcBrowserHome,
            129 => KeyCode.VcVolumeMute,
            130 => KeyCode.VcVolumeDown,
            131 => KeyCode.VcVolumeUp,
            132 => KeyCode.VcMediaNext,
            133 => KeyCode.VcMediaPrevious,
            134 => KeyCode.VcMediaPlay,
            135 => KeyCode.VcMediaStop,
            136 => KeyCode.VcAppMail,
            137 => KeyCode.VcMediaSelect,
            138 => KeyCode.VcApp1,
            139 => KeyCode.VcApp2,
            140 => KeyCode.VcSemicolon,
            141 => KeyCode.VcEquals,
            142 => KeyCode.VcComma,
            143 => KeyCode.VcMinus,
            144 => KeyCode.VcPeriod,
            145 => KeyCode.VcSlash,
            146 => KeyCode.VcBackQuote,
            147 or 154 => KeyCode.VcSection,
            148 => KeyCode.VcYen,
            149 => KeyCode.VcOpenBracket,
            150 => KeyCode.VcBackslash,
            151 => KeyCode.VcCloseBracket,
            152 => KeyCode.VcQuote,
            153 => KeyCode.VcMisc,
            _ => throw new ArgumentOutOfRangeException(
                nameof(key),
                key,
                "The persisted key is not supported by SharpHook on this platform."),
        };
    }

    internal static EventMask ConvertModifiersToEventMask(int modifiers)
    {
        const int known_modifiers = 1 | 2 | 4 | 8;
        if ((modifiers & ~known_modifiers) != 0)
            throw new ArgumentOutOfRangeException(
                nameof(modifiers),
                modifiers,
                "Only Alt, Control, Shift, and Meta modifiers are supported.");

        EventMask result = EventMask.None;
        if ((modifiers & 1) != 0) result |= EventMask.Alt;

        if ((modifiers & 2) != 0) result |= EventMask.Ctrl;

        if ((modifiers & 4) != 0) result |= EventMask.Shift;

        if ((modifiers & 8) != 0) result |= EventMask.Meta;

        return result;
    }

    internal static EventMask NormalizeModifiers(EventMask mask)
    {
        return mask & (EventMask.Alt | EventMask.Ctrl | EventMask.Shift | EventMask.Meta);
    }

    private static bool IsSupportedPlatform()
    {
        return OperatingSystem.IsWindows() ||
               OperatingSystem.IsLinux() ||
               OperatingSystem.IsMacOS();
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs eventArgs)
    {
        KeyCode key = eventArgs.Data.KeyCode;
        EventMask modifiers = NormalizeModifiers(eventArgs.RawEvent.Mask);
        List<Func<CancellationToken, Task>> callbacks;

        lock (gate)
        {
            callbacks = bindings.Values
                .Where(binding => binding.Key == key && binding.Modifiers == modifiers)
                .Select(binding => binding.Callback)
                .ToList();
        }

        foreach (var callback in callbacks) Schedule(callback);
    }

    private void OnHookStopped(IGlobalHook currentHook, Task? task)
    {
        _ = task?.Exception;
        lock (gate)
        {
            if (!ReferenceEquals(hook, currentHook)) return;

            hook = null;
            started = false;
        }

        try
        {
            currentHook.Dispose();
        }
        catch
        {
            // A failed hook is already unusable; there is nothing else to release.
        }
    }

    private void Schedule(Func<CancellationToken, Task> callback)
    {
        CancellationToken cancellationToken;
        lock (gate) cancellationToken = stopping.Token;

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

    private sealed record Binding(
        KeyCode Key,
        EventMask Modifiers,
        Func<CancellationToken, Task> Callback);
}
