namespace Mapping_Tools.Application.Settings.Models;

/// <summary>
///     Preserves the numeric WPF <c>Key</c> and <c>ModifierKeys</c> values written
///     by legacy settings while platform adapters translate them to native input.
/// </summary>
/// <param name="Key">The persisted WPF key-enum value; zero disables the binding.</param>
/// <param name="Modifiers">Persisted Alt, Control, Shift, and Windows flag bits.</param>
public sealed record HotkeySettings(int Key, int Modifiers);

