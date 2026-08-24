using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mapping_Tools.Application.Interactions.Converters;

namespace Mapping_Tools.Application.Interactions;

/// <summary>
///     Describes one strongly typed action in a message or confirmation dialog.
/// </summary>
/// <typeparam name="TResult">The result returned when the action is chosen.</typeparam>
/// <param name="Label">The concise text shown on the action button.</param>
/// <param name="Result">The value returned to the caller.</param>
/// <param name="IsDefault">Whether Enter activates this action.</param>
/// <param name="IsCancel">Whether Escape activates this action.</param>
public sealed record DialogChoice<TResult>(
    string Label,
    TResult Result,
    bool IsDefault = false,
    bool IsCancel = false);

