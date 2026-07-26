using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Shell;

/// <summary>
/// Describes one feature that can be discovered and activated by the desktop shell.
/// </summary>
public sealed class ShellFeatureRegistration
{
    private readonly Func<ObservableObject> _createViewModel;

    /// <summary>
    /// Creates an explicit shell feature registration.
    /// </summary>
    /// <param name="id">Stable persistence identifier.</param>
    /// <param name="displayName">User-facing navigation label.</param>
    /// <param name="category">Navigation and search category.</param>
    /// <param name="description">Short accessible feature summary.</param>
    /// <param name="searchTerms">Additional case-insensitive search terms.</param>
    /// <param name="createViewModel">Factory invoked when the feature is first activated.</param>
    /// <param name="startsSection">Whether navigation draws a divider before this item.</param>
    public ShellFeatureRegistration(
        string id,
        string displayName,
        string category,
        string description,
        IEnumerable<string> searchTerms,
        Func<ObservableObject> createViewModel,
        bool startsSection = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(searchTerms);
        ArgumentNullException.ThrowIfNull(createViewModel);

        Id = id;
        DisplayName = displayName;
        Category = category;
        Description = description;
        SearchTerms = searchTerms.Where(term => !string.IsNullOrWhiteSpace(term)).ToArray();
        StartsSection = startsSection;
        _createViewModel = createViewModel;
    }

    /// <summary>Gets the stable feature identifier stored by the shell.</summary>
    public string Id { get; }

    /// <summary>Gets the navigation label.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the feature category.</summary>
    public string Category { get; }

    /// <summary>Gets the short feature description.</summary>
    public string Description { get; }

    /// <summary>Gets additional terms considered by shell search.</summary>
    public IReadOnlyList<string> SearchTerms { get; }

    /// <summary>Gets whether this item begins a visually separated navigation group.</summary>
    public bool StartsSection { get; }

    /// <summary>Creates the presentation model when the feature is first opened.</summary>
    /// <returns>A new feature presentation model.</returns>
    public ObservableObject CreateViewModel() => _createViewModel();
}
