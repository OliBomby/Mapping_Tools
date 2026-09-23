namespace Mapping_Tools.Core.Settings.Models;

/// <summary>
///     Stores Avalonia <c>Key</c> and <c>KeyModifiers</c> values without making
///     the frontend framework a dependency of the shared settings model.
///     The numeric representation remains stable for existing settings and
///     project documents.
/// </summary>
/// <param name="Key">The Avalonia key-enum value; zero disables the binding.</param>
/// <param name="Modifiers">The Avalonia Alt, Control, Shift, and Meta flag bits.</param>
public sealed record HotkeySettings(int Key, int Modifiers);
