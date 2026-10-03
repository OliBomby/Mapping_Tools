using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools;

/// <summary>
///     Describes one Mapping Tools capability as seen by application-level
///     catalogs and command routing.
/// </summary>
public sealed class ToolDefinition
{
    private readonly string description;
    private readonly Func<string>? translatedDescriptionGetter;
    private readonly Func<string>? translatedSearchTermsGetter;
    private readonly string[] searchTerms;

    /// <summary>
    ///     Creates an immutable tool definition.
    /// </summary>
    /// <param name="id">The stable identifier used for navigation, execution, and command lookup.</param>
    /// <param name="displayName">The user-facing tool name used by shell and command messages.</param>
    /// <param name="description">The concise user-facing summary shown in tool discovery.</param>
    /// <param name="searchTerms">Additional terms used when finding the tool in shell search.</param>
    /// <param name="quickRunTargets">
    ///     The selection sizes for which QuickRun may offer this tool, or
    ///     <see langword="null" /> when the tool has no QuickRun command.
    /// </param>
    /// <param name="translatedDescriptionGetter">Optional getter for the description in the currently selected text language.</param>
    /// <param name="translatedSearchTermsGetter">Optional getter for additional translated search terms, separated by vertical bars.</param>
    /// <exception cref="ArgumentException">
    ///     A required text value is blank, or <paramref name="quickRunTargets" />
    ///     contains no known target.
    /// </exception>
    public ToolDefinition(
        string id,
        string displayName,
        string description,
        IEnumerable<string> searchTerms,
        QuickRunTargets? quickRunTargets = null,
        Func<string>? translatedDescriptionGetter = null,
        Func<string>? translatedSearchTermsGetter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(searchTerms);

        if (quickRunTargets is 0
            || quickRunTargets is not null
            && (quickRunTargets.Value & ~QuickRun.Models.QuickRunTargets.Always) != 0)
            throw new ArgumentException(
                // Developer diagnostic for invalid tool registration, not displayed in the UI.
                // ReSharper disable once LocalizableElement
                "QuickRun targets must contain at least one known selection size.",
                nameof(quickRunTargets));

        Id = id;
        DisplayName = displayName;
        this.description = description;
        this.translatedDescriptionGetter = translatedDescriptionGetter;
        this.translatedSearchTermsGetter = translatedSearchTermsGetter;
        this.searchTerms = searchTerms
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Select(term => term.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        QuickRunTargets = quickRunTargets;
    }

    /// <summary>Gets the stable identifier shared by the tool and its primary operation.</summary>
    public string Id { get; }

    /// <summary>Gets the user-facing name used by navigation and execution messages.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the concise description used by tool discovery surfaces.</summary>
    public string Description => translatedDescriptionGetter is null
        ? description
        : translatedDescriptionGetter();

    /// <summary>Gets the normalized search terms used by tool discovery.</summary>
    public IReadOnlyList<string> SearchTerms
    {
        get
        {
            IEnumerable<string> localizedTerms = translatedSearchTermsGetter is null
                ? []
                : translatedSearchTermsGetter()
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return searchTerms
                .Concat(localizedTerms)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    /// <summary>
    ///     Gets the live selection sizes for which the primary operation may be
    ///     offered as a QuickRun target, or <see langword="null" /> when unsupported.
    /// </summary>
    public QuickRunTargets? QuickRunTargets { get; }
}
