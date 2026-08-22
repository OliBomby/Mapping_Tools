using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Tools.TumourGenerating;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts one plain tumour layer for Desktop editing and preview refresh.</summary>
public sealed class ObservableTumourLayer : ObservableObject
{
    private readonly TumourLayer model;

    /// <summary>Creates an adapter around the supplied plain layer.</summary>
    /// <param name="model">The domain layer edited by this adapter.</param>
    public ObservableTumourLayer(TumourLayer model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>Gets the plain layer represented by this adapter.</summary>
    public TumourLayer Model => model;

    /// <summary>Gets or sets the selected geometric template.</summary>
    public TumourTemplate TumourTemplateEnum
    {
        get => model.TumourTemplateEnum;
        set
        {
            if (model.TumourTemplateEnum == value) return;
            model.TumourTemplateEnum = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TumourTemplate));
        }
    }

    /// <summary>Gets the configured template instance for the selected enum.</summary>
    public ITumourTemplate TumourTemplate => model.TumourTemplate;

    /// <summary>Gets or sets how the tumour follows the slider path.</summary>
    public WrappingMode WrappingMode { get => model.WrappingMode; set => Set(model.WrappingMode, value, assigned => model.WrappingMode = assigned); }

    /// <summary>Gets or sets the side-selection policy.</summary>
    public TumourSidedness TumourSidedness { get => model.TumourSidedness; set => Set(model.TumourSidedness, value, assigned => model.TumourSidedness = assigned); }

    /// <summary>Gets or sets the graph controlling tumour length.</summary>
    public GraphState TumourLength { get => model.TumourLength; set => Set(model.TumourLength, value, assigned => model.TumourLength = assigned); }

    /// <summary>Gets or sets the graph controlling tumour scale.</summary>
    public GraphState TumourScale { get => model.TumourScale; set => Set(model.TumourScale, value, assigned => model.TumourScale = assigned); }

    /// <summary>Gets or sets the graph controlling tumour rotation.</summary>
    public GraphState TumourRotation { get => model.TumourRotation; set => Set(model.TumourRotation, value, assigned => model.TumourRotation = assigned); }

    /// <summary>Gets or sets the graph controlling the template parameter.</summary>
    public GraphState TumourParameter { get => model.TumourParameter; set => Set(model.TumourParameter, value, assigned => model.TumourParameter = assigned); }

    /// <summary>Gets or sets the graph controlling tumour spacing.</summary>
    public GraphState TumourDistance { get => model.TumourDistance; set => Set(model.TumourDistance, value, assigned => model.TumourDistance = assigned); }

    /// <summary>Gets or sets the explicit tumour count.</summary>
    public int TumourCount { get => model.TumourCount; set => Set(model.TumourCount, value, assigned => model.TumourCount = assigned); }

    /// <summary>Gets or sets the sequence start.</summary>
    public double TumourStart { get => model.TumourStart; set => Set(model.TumourStart, value, assigned => model.TumourStart = assigned); }

    /// <summary>Gets or sets the sequence end.</summary>
    public double TumourEnd { get => model.TumourEnd; set => Set(model.TumourEnd, value, assigned => model.TumourEnd = assigned); }

    /// <summary>Gets or sets the deterministic random-side seed.</summary>
    public int RandomSeed { get => model.RandomSeed; set => Set(model.RandomSeed, value, assigned => model.RandomSeed = assigned); }

    /// <summary>Gets or sets whether the path is recalculated before placement.</summary>
    public bool Recalculate { get => model.Recalculate; set => Set(model.Recalculate, value, assigned => model.Recalculate = assigned); }

    /// <summary>Gets or sets whether range and shape values are absolute pixels.</summary>
    public bool UseAbsoluteRange { get => model.UseAbsoluteRange; set => Set(model.UseAbsoluteRange, value, assigned => model.UseAbsoluteRange = assigned); }

    /// <summary>Gets or sets whether this layer participates in generation.</summary>
    public bool IsActive { get => model.IsActive; set => Set(model.IsActive, value, assigned => model.IsActive = assigned); }

    /// <summary>Gets or sets the user-facing layer name.</summary>
    public string Name { get => model.Name; set => Set(model.Name, value ?? string.Empty, assigned => model.Name = assigned); }

    /// <summary>Creates a plain snapshot for an Application service.</summary>
    /// <returns>An independently mutable copy of this layer.</returns>
    public TumourLayer Snapshot() => model.Copy();

    private void Set<T>(
        T current,
        T value,
        Action<T> assign,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value)) return;
        assign(value);
        OnPropertyChanged(propertyName);
    }
}
