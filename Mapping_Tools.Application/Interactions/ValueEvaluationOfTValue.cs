using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mapping_Tools.Application.Interactions.Converters;

namespace Mapping_Tools.Application.Interactions;

/// <summary>
///     Carries the typed result of parsing and validating current field text.
/// </summary>
/// <typeparam name="TValue">The form value type.</typeparam>
/// <param name="IsValid">Whether parsing and every ordered rule succeeded.</param>
/// <param name="Value">The parsed value when valid, or the type default after failure.</param>
/// <param name="ErrorMessage">The first format or validation correction.</param>
public readonly record struct ValueEvaluation<TValue>(
    bool IsValid,
    TValue? Value,
    string? ErrorMessage);

