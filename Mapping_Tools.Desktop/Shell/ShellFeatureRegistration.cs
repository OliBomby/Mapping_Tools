using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Tools;

namespace Mapping_Tools.Desktop.Shell;

/// <summary>
///     Describes one feature that can be discovered and activated by the desktop shell.
/// </summary>
public sealed class ShellFeatureRegistration
{
    private readonly Func<ObservableObject> createViewModel;
    private readonly string displayName;
    private readonly string description;
    private readonly IReadOnlyList<string> searchTerms;
    private readonly ToolDefinition? toolDefinition;
    private readonly Func<string>? translatedDisplayNameGetter;
    private readonly Func<string>? translatedDescriptionGetter;
    private readonly Func<string>? translatedSearchTermsGetter;

    /// <summary>
    ///     Creates a shell feature registration supplied by composition.
    /// </summary>
    /// <param name="id">Stable persistence identifier.</param>
    /// <param name="displayName">User-facing navigation label.</param>
    /// <param name="category">Navigation and search category.</param>
    /// <param name="description">Short accessible feature summary.</param>
    /// <param name="searchTerms">Additional case-insensitive search terms.</param>
    /// <param name="createViewModel">Factory invoked when the feature is first activated.</param>
    /// <param name="horizontalScrollBarVisibility">How the shell scrolls this feature horizontally.</param>
    /// <param name="verticalScrollBarVisibility">How the shell scrolls this feature vertically.</param>
    /// <param name="toolDefinition">Optional live tool metadata used for translated descriptions and search terms.</param>
    /// <param name="translatedDisplayNameGetter">Optional getter for a built-in page name in the current text language.</param>
    /// <param name="translatedDescriptionGetter">Optional getter for a built-in page description in the current text language.</param>
    /// <param name="translatedSearchTermsGetter">Optional getter for semicolon-separated translated search synonyms.</param>
    public ShellFeatureRegistration(
        string id,
        string displayName,
        string category,
        string description,
        IEnumerable<string> searchTerms,
        Func<ObservableObject> createViewModel,
        ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        ToolDefinition? toolDefinition = null,
        Func<string>? translatedDisplayNameGetter = null,
        Func<string>? translatedDescriptionGetter = null,
        Func<string>? translatedSearchTermsGetter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(searchTerms);
        ArgumentNullException.ThrowIfNull(createViewModel);

        Id = id;
        this.displayName = displayName;
        Category = category;
        this.description = description;
        this.searchTerms = searchTerms.Where(term => !string.IsNullOrWhiteSpace(term)).ToArray();
        this.toolDefinition = toolDefinition;
        this.translatedDisplayNameGetter = translatedDisplayNameGetter;
        this.translatedDescriptionGetter = translatedDescriptionGetter;
        this.translatedSearchTermsGetter = translatedSearchTermsGetter;
        HorizontalScrollBarVisibility = horizontalScrollBarVisibility;
        VerticalScrollBarVisibility = verticalScrollBarVisibility;
        this.createViewModel = createViewModel;
    }

    /// <summary>Gets the stable feature identifier stored by the shell.</summary>
    public string Id { get; }

    /// <summary>Gets the navigation label.</summary>
    public string DisplayName => translatedDisplayNameGetter is null ? displayName : translatedDisplayNameGetter();

    /// <summary>Gets the feature category.</summary>
    public string Category { get; }

    /// <summary>Gets the short feature description.</summary>
    public string Description => toolDefinition?.Description
                                 ?? (translatedDescriptionGetter is null ? description : translatedDescriptionGetter());

    /// <summary>Gets additional terms considered by shell search.</summary>
    public IReadOnlyList<string> SearchTerms => toolDefinition?.SearchTerms ?? (translatedSearchTermsGetter is null
        ? searchTerms
        : searchTerms.Concat(translatedSearchTermsGetter().Split(';', StringSplitOptions.RemoveEmptyEntries)).ToArray());

    /// <summary>Gets the horizontal scrolling behavior owned by the feature shell.</summary>
    public ScrollBarVisibility HorizontalScrollBarVisibility { get; }

    /// <summary>Gets the vertical scrolling behavior owned by the feature shell.</summary>
    public ScrollBarVisibility VerticalScrollBarVisibility { get; }

    /// <summary>Creates the presentation model when the feature is first opened.</summary>
    /// <returns>A new feature presentation model.</returns>
    public ObservableObject CreateViewModel()
    {
        return createViewModel();
    }
}
