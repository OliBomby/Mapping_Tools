using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Presents one explicit feature registration in shell navigation.
/// </summary>
public sealed partial class ShellFeatureItemViewModel : ObservableObject
{
    private readonly Action<ShellFeatureItemViewModel> _activate;
    private readonly Action<ShellFeatureItemViewModel> _toggleFavorite;

    internal ShellFeatureItemViewModel(
        ShellFeatureRegistration registration,
        int order,
        bool isFavorite,
        Action<ShellFeatureItemViewModel> activate,
        Action<ShellFeatureItemViewModel> toggleFavorite)
    {
        Id = registration.Id;
        DisplayName = registration.DisplayName;
        Category = registration.Category;
        Description = registration.Description;
        Order = order;
        StartsSection = registration.StartsSection;
        _activate = activate;
        _toggleFavorite = toggleFavorite;
        SearchableText = string.Join(
            ' ',
            new[] { registration.DisplayName, registration.Category, registration.Description }
                .Concat(registration.SearchTerms));
        IsFavorite = isFavorite;
    }

    /// <summary>Gets the stable persistence identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the navigation label.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the navigation category.</summary>
    public string Category { get; }

    /// <summary>Gets the feature summary.</summary>
    public string Description { get; }

    /// <summary>Gets whether a non-selectable divider precedes this navigation item.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigationRowHeight))]
    public partial bool StartsSection { get; internal set; }

    /// <summary>Gets whether this feature is pinned ahead of ordinary items.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteGlyph))]
    [NotifyPropertyChangedFor(nameof(FavoriteActionLabel))]
    public partial bool IsFavorite { get; internal set; }

    /// <summary>Gets the star glyph corresponding to favorite state.</summary>
    public string FavoriteGlyph => IsFavorite ? "★" : "☆";

    /// <summary>Gets the context-menu action for the current favorite state.</summary>
    public string FavoriteActionLabel => IsFavorite ? "Unfavorite" : "Favorite";

    /// <summary>Gets whether this feature currently occupies the shell content area.</summary>
    [ObservableProperty]
    public partial bool IsActive { get; internal set; }

    /// <summary>Gets the legacy row height for default pages and tool pages.</summary>
    public double NavigationHeight => Category.Equals("Tools", StringComparison.Ordinal)
        ? 37
        : 41;

    /// <summary>Gets the complete row height, including a legacy section divider when present.</summary>
    public double NavigationRowHeight => NavigationHeight + (StartsSection ? 21 : 0);

    internal string SearchableText { get; }

    internal int Order { get; }

    [RelayCommand]
    private void Activate() =>
        _activate(this);

    [RelayCommand]
    private void ToggleFavorite() =>
        _toggleFavorite(this);
}
