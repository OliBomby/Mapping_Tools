using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.ObjectVisualiser;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Adapts one persisted pattern to the thumbnail gallery.</summary>
public sealed partial class PatternGalleryItemViewModel : ObservableObject
{
    private bool isSelected;

    /// <summary>Creates a gallery item for the supplied persisted pattern.</summary>
    /// <param name="pattern">The pattern metadata owned by the project.</param>
    public PatternGalleryItemViewModel(PatternGalleryPattern pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    }

    /// <summary>Gets the persisted pattern metadata.</summary>
    public PatternGalleryPattern Pattern { get; }

    /// <summary>Gets the current display name.</summary>
    public string Name => Pattern.Name;

    /// <summary>Gets or sets whether this item participates in the next export.</summary>
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            SetProperty(ref isSelected, value);
        }
    }

    /// <summary>Gets the framework-neutral thumbnail scene, when it has loaded.</summary>
    [ObservableProperty]
    public partial ObjectVisualiserScene? Scene { get; private set; }

    /// <summary>Publishes a newly loaded thumbnail scene.</summary>
    /// <param name="scene">The scene built from the stored pattern map.</param>
    internal void SetScene(ObjectVisualiserScene? scene) => Scene = scene;

    /// <summary>Refreshes bindings after persisted metadata changes.</summary>
    internal void RefreshMetadata() => OnPropertyChanged(nameof(Name));

    /// <summary>Refreshes the selection binding after a bulk selection operation.</summary>
    internal void RefreshSelection() => OnPropertyChanged(nameof(IsSelected));
}

/// <summary>Groups visible Pattern Gallery items by their persisted group name.</summary>
public sealed class PatternGalleryGroupViewModel
{
    /// <summary>Creates a group with its sorted visible items.</summary>
    /// <param name="name">The display label, with empty groups shown as None.</param>
    /// <param name="patterns">The items assigned to the group.</param>
    public PatternGalleryGroupViewModel(
        string name,
        IEnumerable<PatternGalleryItemViewModel> patterns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(patterns);
        Name = name;
        Patterns = patterns.ToArray();
    }

    /// <summary>Gets the display label for this group.</summary>
    public string Name { get; }

    /// <summary>Gets the visible items in this group.</summary>
    public IReadOnlyList<PatternGalleryItemViewModel> Patterns { get; }

    /// <summary>Gets the number of visible patterns in this group.</summary>
    public int ItemCount => Patterns.Count;
}
