using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Tools.TumourGenerating;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts one plain tumour layer for Desktop editing and preview refresh.</summary>
public sealed partial class ObservableTumourLayer : ObservableObject
{
    private readonly TumourLayer model;

    /// <summary>Creates an adapter around the supplied plain layer.</summary>
    /// <param name="model">The domain layer edited by this adapter.</param>
    public ObservableTumourLayer(TumourLayer model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        TumourTemplateEnum = model.TumourTemplateEnum;
        WrappingMode = model.WrappingMode;
        TumourSidedness = model.TumourSidedness;
        TumourLength = model.TumourLength;
        TumourScale = model.TumourScale;
        TumourRotation = model.TumourRotation;
        TumourParameter = model.TumourParameter;
        TumourDistance = model.TumourDistance;
        TumourCount = model.TumourCount;
        TumourStart = model.TumourStart;
        TumourEnd = model.TumourEnd;
        RandomSeed = model.RandomSeed;
        Recalculate = model.Recalculate;
        UseAbsoluteRange = model.UseAbsoluteRange;
        IsActive = model.IsActive;
        Name = model.Name;
    }

    /// <summary>Gets the plain layer represented by this adapter.</summary>
    public TumourLayer Model => model;

    /// <summary>Gets or sets the selected geometric template.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TumourTemplate))]
    public partial TumourTemplate TumourTemplateEnum { get; set; }

    /// <summary>Gets the configured template instance for the selected enum.</summary>
    public ITumourTemplate TumourTemplate => model.TumourTemplate;

    /// <summary>Gets or sets how the tumour follows the slider path.</summary>
    [ObservableProperty]
    public partial WrappingMode WrappingMode { get; set; }

    /// <summary>Gets or sets the side-selection policy.</summary>
    [ObservableProperty]
    public partial TumourSidedness TumourSidedness { get; set; }

    /// <summary>Gets or sets the graph controlling tumour length.</summary>
    [ObservableProperty]
    public partial GraphState TumourLength { get; set; }

    /// <summary>Gets or sets the graph controlling tumour scale.</summary>
    [ObservableProperty]
    public partial GraphState TumourScale { get; set; }

    /// <summary>Gets or sets the graph controlling tumour rotation.</summary>
    [ObservableProperty]
    public partial GraphState TumourRotation { get; set; }

    /// <summary>Gets or sets the graph controlling the template parameter.</summary>
    [ObservableProperty]
    public partial GraphState TumourParameter { get; set; }

    /// <summary>Gets or sets the graph controlling tumour spacing.</summary>
    [ObservableProperty]
    public partial GraphState TumourDistance { get; set; }

    /// <summary>Gets or sets the explicit tumour count.</summary>
    [ObservableProperty]
    public partial int TumourCount { get; set; }

    /// <summary>Gets or sets the sequence start.</summary>
    [ObservableProperty]
    public partial double TumourStart { get; set; }

    /// <summary>Gets or sets the sequence end.</summary>
    [ObservableProperty]
    public partial double TumourEnd { get; set; }

    /// <summary>Gets or sets the deterministic random-side seed.</summary>
    [ObservableProperty]
    public partial int RandomSeed { get; set; }

    /// <summary>Gets or sets whether the path is recalculated before placement.</summary>
    [ObservableProperty]
    public partial bool Recalculate { get; set; }

    /// <summary>Gets or sets whether range and shape values are absolute pixels.</summary>
    [ObservableProperty]
    public partial bool UseAbsoluteRange { get; set; }

    /// <summary>Gets or sets whether this layer participates in generation.</summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>Gets or sets the user-facing layer name.</summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>Creates a plain snapshot for an Application service.</summary>
    /// <returns>An independently mutable copy of this layer.</returns>
    public TumourLayer Snapshot() => model.Copy();

    partial void OnTumourTemplateEnumChanged(TumourTemplate value) => model.TumourTemplateEnum = value;

    partial void OnWrappingModeChanged(WrappingMode value) => model.WrappingMode = value;
    partial void OnTumourSidednessChanged(TumourSidedness value) => model.TumourSidedness = value;
    partial void OnTumourLengthChanged(GraphState value) => model.TumourLength = value;
    partial void OnTumourScaleChanged(GraphState value) => model.TumourScale = value;
    partial void OnTumourRotationChanged(GraphState value) => model.TumourRotation = value;
    partial void OnTumourParameterChanged(GraphState value) => model.TumourParameter = value;
    partial void OnTumourDistanceChanged(GraphState value) => model.TumourDistance = value;
    partial void OnTumourCountChanged(int value) => model.TumourCount = value;
    partial void OnTumourStartChanged(double value) => model.TumourStart = value;
    partial void OnTumourEndChanged(double value) => model.TumourEnd = value;
    partial void OnRandomSeedChanged(int value) => model.RandomSeed = value;
    partial void OnRecalculateChanged(bool value) => model.Recalculate = value;
    partial void OnUseAbsoluteRangeChanged(bool value) => model.UseAbsoluteRange = value;
    partial void OnIsActiveChanged(bool value) => model.IsActive = value;
    partial void OnNameChanged(string value) => model.Name = value;
}
