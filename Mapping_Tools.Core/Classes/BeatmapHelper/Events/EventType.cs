// ReSharper disable InconsistentNaming

namespace Mapping_Tools.Core.Classes.BeatmapHelper.Events;

/// <summary>
///     Identifies an osu! storyboard command by its serialized command token.
/// </summary>
public enum EventType
{
    /// <summary>
    ///     Changes opacity over time.
    /// </summary>
    F, // Fade

    /// <summary>
    ///     Changes both X and Y position over time.
    /// </summary>
    M, // Move

    /// <summary>
    ///     Changes horizontal position over time.
    /// </summary>
    MX, // Move X

    /// <summary>
    ///     Changes vertical position over time.
    /// </summary>
    MY, // Move Y

    /// <summary>
    ///     Applies uniform scale over time.
    /// </summary>
    S, // Scale

    /// <summary>
    ///     Applies independent horizontal and vertical scale over time.
    /// </summary>
    V, // Vector scale

    /// <summary>
    ///     Rotates the storyboard object over time.
    /// </summary>
    R, // Rotate

    /// <summary>
    ///     Changes red, green, and blue colour channels over time.
    /// </summary>
    C, // Colour

    /// <summary>
    ///     Repeats a nested command group a fixed number of times.
    /// </summary>
    L, // Loop

    /// <summary>
    ///     Executes nested commands while a gameplay trigger is active.
    /// </summary>
    T, // EventType-triggered loop

    /// <summary>
    ///     Toggles additive blending or sprite flipping.
    /// </summary>
    P, // Parameters

    /// <summary>
    ///     A token not recognized by the parser.
    /// </summary>
    Unknown, // Unknown command type
}
