using Mapping_Tools.Desktop.Shell;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Presents one explicit feature registration in shell navigation.
/// </summary>
public sealed class ShellFeatureItemViewModel : ViewModelBase
{
    private bool _isFavorite;
    private bool _isActive;

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
        SearchableText = string.Join(
            ' ',
            new[] { registration.DisplayName, registration.Category, registration.Description }
                .Concat(registration.SearchTerms));
        _isFavorite = isFavorite;
        ActivateCommand = ReactiveCommand.Create(() => activate(this));
        ToggleFavoriteCommand = ReactiveCommand.Create(() => toggleFavorite(this));
    }

    /// <summary>Gets the stable persistence identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the navigation label.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the navigation category.</summary>
    public string Category { get; }

    /// <summary>Gets the feature summary.</summary>
    public string Description { get; }

    /// <summary>Gets whether a divider precedes this navigation item.</summary>
    public bool StartsSection { get; }

    /// <summary>Gets whether this feature is pinned ahead of ordinary items.</summary>
    public bool IsFavorite
    {
        get => _isFavorite;
        internal set
        {
            this.RaiseAndSetIfChanged(ref _isFavorite, value);
            this.RaisePropertyChanged(nameof(FavoriteGlyph));
            this.RaisePropertyChanged(nameof(FavoriteActionLabel));
        }
    }

    /// <summary>Gets the star glyph corresponding to favorite state.</summary>
    public string FavoriteGlyph => IsFavorite ? "★" : "☆";

    /// <summary>Gets an accessible description of the favorite action.</summary>
    public string FavoriteActionLabel => IsFavorite
        ? $"Remove {DisplayName} from favorites"
        : $"Add {DisplayName} to favorites";

    /// <summary>Gets whether this feature currently occupies the shell content area.</summary>
    public bool IsActive
    {
        get => _isActive;
        internal set
        {
            this.RaiseAndSetIfChanged(ref _isActive, value);
        }
    }

    /// <summary>Gets the legacy row height for default pages and tool pages.</summary>
    public double NavigationHeight => Category.Equals("Tools", StringComparison.Ordinal)
        ? 37
        : 41;

    /// <summary>Gets the complete row height, including a legacy section divider when present.</summary>
    public double NavigationRowHeight => NavigationHeight + (StartsSection ? 21 : 0);

    /// <summary>Gets the command that activates this feature.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ActivateCommand { get; }

    /// <summary>Gets the command that toggles persisted favorite state.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ToggleFavoriteCommand { get; }

    internal string SearchableText { get; }

    internal int Order { get; }
}
