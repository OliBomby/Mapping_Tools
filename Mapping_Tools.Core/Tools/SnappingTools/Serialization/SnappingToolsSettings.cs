using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Core.Tools.SnappingTools.Serialization;

/// <summary>Controls which generated objects are shown for a key state.</summary>
[Flags]
public enum ViewMode
{
    /// <summary>Show no generated objects.</summary>
    Nothing = 0,

    /// <summary>Show all descendants.</summary>
    Children = 1,

    /// <summary>Show direct descendants.</summary>
    DirectChildren = 1 << 1,

    /// <summary>Show all ancestors.</summary>
    Parents = 1 << 2,

    /// <summary>Show direct ancestors.</summary>
    DirectParents = 1 << 3,

    /// <summary>Show the complete generated graph.</summary>
    Everything = 1 << 4,
}

/// <summary>Selects which editor hit objects become root objects.</summary>
public enum SelectedHitObjectMode
{
    /// <summary>Use every hit object visible around the editor time.</summary>
    AllwaysAllVisible,

    /// <summary>Use selected objects when any are selected, otherwise visible objects.</summary>
    VisibleOrSelected,

    /// <summary>Use only selected objects.</summary>
    OnlySelected,
}

/// <summary>Selects the event that refreshes the generated-object graph.</summary>
public enum UpdateMode
{
    /// <summary>Refresh after any relevant editor change.</summary>
    AnyChange,

    /// <summary>Refresh when the editor time changes.</summary>
    TimeChange,

    /// <summary>Refresh when the activation key is pressed.</summary>
    HotkeyDown,

    /// <summary>Refresh when osu! becomes the active window.</summary>
    OsuActivated,
}

/// <summary>Stores the persistent appearance and input settings of one dashboard.</summary>
public sealed class SnappingToolsPreferences : ICloneable
{
    private Dictionary<Type, GeneratorSettings> generatorSettings;
    private Hotkey inheritHotkey;
    private Hotkey lockHotkey;
    private Hotkey refreshHotkey;
    private Dictionary<string, RelevantObjectPreferences> relevantObjectPreferences;
    private Hotkey selectHotkey;
    private Hotkey snapHotkey;

    /// <summary>Creates the legacy default Geometry Dashboard settings.</summary>
    public SnappingToolsPreferences()
    {
        relevantObjectPreferences = new Dictionary<string, RelevantObjectPreferences>
        {
            [RelevantPoint.PreferencesNameStatic] = new()
            {
                Name = RelevantPoint.PreferencesNameStatic,
                Color = RgbaColour.FromArgb(255, 0, 255, 255),
                Dashstyle = DashStylesEnum.Solid,
                Opacity = 0.8,
                Size = 5,
                Thickness = 3,
                HasSizeOption = true,
            },
            [RelevantLine.PreferencesNameStatic] = new()
            {
                Name = RelevantLine.PreferencesNameStatic,
                Color = RgbaColour.FromArgb(255, 124, 252, 0),
                Dashstyle = DashStylesEnum.Dash,
                Opacity = 0.8,
                Thickness = 3,
                HasSizeOption = false,
            },
            [RelevantCircle.PreferencesNameStatic] = new()
            {
                Name = RelevantCircle.PreferencesNameStatic,
                Color = RgbaColour.FromArgb(255, 255, 0, 0),
                Dashstyle = DashStylesEnum.Dash,
                Opacity = 0.8,
                Thickness = 3,
                HasSizeOption = false,
            },
        };

        generatorSettings = new Dictionary<Type, GeneratorSettings>();
        snapHotkey = new Hotkey(56);
        selectHotkey = new Hotkey(57);
        lockHotkey = new Hotkey(57, 4);
        inheritHotkey = new Hotkey(57, 1);
        refreshHotkey = new Hotkey(45);
        OffsetTop = 1;
        OffsetBottom = 1;
        AcceptableDifference = 2;
        KeyDownViewMode = ViewMode.Parents;
        KeyUpViewMode = ViewMode.Everything;
        SelectedHitObjectMode = SelectedHitObjectMode.AllwaysAllVisible;
        UpdateMode = UpdateMode.TimeChange;
        InceptionLevel = 5;
    }

    /// <summary>Gets or sets appearance settings by stable preference-group name.</summary>
    public Dictionary<string, RelevantObjectPreferences> RelevantObjectPreferences
    {
        get => relevantObjectPreferences;
        set => relevantObjectPreferences = value ?? [];
    }

    /// <summary>Gets or sets generator settings keyed by concrete generator type.</summary>
    public Dictionary<Type, GeneratorSettings> GeneratorSettings
    {
        get => generatorSettings;
        set => generatorSettings = value ?? [];
    }

    /// <summary>Gets or sets the activation/snap key.</summary>
    public Hotkey SnapHotkey { get => snapHotkey; set => snapHotkey = value ?? new Hotkey(); }

    /// <summary>Gets or sets the selection key.</summary>
    public Hotkey SelectHotkey { get => selectHotkey; set => selectHotkey = value ?? new Hotkey(); }

    /// <summary>Gets or sets the lock/unlock key.</summary>
    public Hotkey LockHotkey { get => lockHotkey; set => lockHotkey = value ?? new Hotkey(); }

    /// <summary>Gets or sets the inheritability key.</summary>
    public Hotkey InheritHotkey { get => inheritHotkey; set => inheritHotkey = value ?? new Hotkey(); }

    /// <summary>Gets or sets the refresh key.</summary>
    public Hotkey RefreshHotkey { get => refreshHotkey; set => refreshHotkey = value ?? new Hotkey(); }

    /// <summary>Gets or sets the left editor-box correction in pixels.</summary>
    public double OffsetLeft { get; set; }

    /// <summary>Gets or sets the top editor-box correction in pixels.</summary>
    public double OffsetTop { get; set; }

    /// <summary>Gets or sets the right editor-box correction in pixels.</summary>
    public double OffsetRight { get; set; }

    /// <summary>Gets or sets the bottom editor-box correction in pixels.</summary>
    public double OffsetBottom { get; set; }

    /// <summary>Gets the four stored editor-box corrections as a box.</summary>
    public Box2 OverlayOffset => new(OffsetLeft, OffsetTop, OffsetRight, OffsetBottom);

    /// <summary>Gets or sets the duplicate-distance tolerance in editor pixels.</summary>
    public double AcceptableDifference { get; set; }

    /// <summary>Gets or sets whether the engine continues when the dashboard is hidden.</summary>
    public bool KeepRunning { get; set; }

    /// <summary>Gets or sets whether the osu! playfield boundary is displayed.</summary>
    public bool VisiblePlayfieldBoundary { get; set; }

    /// <summary>Gets or sets whether platform debugging visuals are enabled.</summary>
    public bool DebugEnabled { get; set; }

    /// <summary>Gets or sets the graph shown while the snap key is down.</summary>
    public ViewMode KeyDownViewMode { get; set; }

    /// <summary>Gets or sets the graph shown while the snap key is up.</summary>
    public ViewMode KeyUpViewMode { get; set; }

    /// <summary>Gets or sets the rule for selecting root hit objects.</summary>
    public SelectedHitObjectMode SelectedHitObjectMode { get; set; }

    /// <summary>Gets or sets the refresh trigger.</summary>
    public UpdateMode UpdateMode { get; set; }

    /// <summary>Gets or sets the number of generated layers, including the root layer.</summary>
    public int InceptionLevel { get; set; }

    /// <inheritdoc />
    public object Clone()
    {
        var clone = (SnappingToolsPreferences)MemberwiseClone();
        clone.GeneratorSettings = new Dictionary<Type, GeneratorSettings>();
        foreach (var (key, value) in GeneratorSettings) clone.GeneratorSettings.Add(key, (GeneratorSettings)value.Clone());

        clone.RelevantObjectPreferences = new Dictionary<string, RelevantObjectPreferences>();
        foreach ((string key, var value) in RelevantObjectPreferences) clone.RelevantObjectPreferences.Add(key, (RelevantObjectPreferences)value.Clone());

        clone.snapHotkey = (Hotkey)SnapHotkey.Clone();
        clone.selectHotkey = (Hotkey)SelectHotkey.Clone();
        clone.lockHotkey = (Hotkey)LockHotkey.Clone();
        clone.inheritHotkey = (Hotkey)InheritHotkey.Clone();
        clone.refreshHotkey = (Hotkey)RefreshHotkey.Clone();

        return clone;
    }

    /// <summary>Gets a configured preference group or a new empty fallback.</summary>
    /// <param name="input">The preference-group name.</param>
    /// <returns>The stored preferences or a default empty instance.</returns>
    public RelevantObjectPreferences GetReleventObjectPreferences(string input)
    {
        return RelevantObjectPreferences.TryGetValue(input, out var output)
            ? output
            : new RelevantObjectPreferences();
    }

    /// <summary>Copies saved settings into the supplied generator instances.</summary>
    /// <param name="generators">The live generators to configure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generators" /> is <see langword="null" />.</exception>
    public void ApplyGeneratorSettings(IEnumerable<RelevantObjectsGenerator> generators)
    {
        ArgumentNullException.ThrowIfNull(generators);

        foreach (var generator in generators)
            if (GeneratorSettings.TryGetValue(generator.GetType(), out var settings))
                settings.CopyTo(generator.Settings);
    }

    /// <summary>Stores each live generator's settings under its concrete type.</summary>
    /// <param name="generators">The live generators to snapshot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generators" /> is <see langword="null" />.</exception>
    public void SaveGeneratorSettings(IEnumerable<RelevantObjectsGenerator> generators)
    {
        ArgumentNullException.ThrowIfNull(generators);

        foreach (var generator in generators) GeneratorSettings[generator.GetType()] = generator.Settings;
    }
}

/// <summary>A named saved snapshot of Geometry Dashboard preferences.</summary>
public sealed class SnappingToolsSaveSlot : ICloneable
{
    private string name = string.Empty;
    private SnappingToolsPreferences preferences = new();
    private Hotkey projectHotkey = new();

    /// <summary>Gets or sets the user-visible slot name.</summary>
    public string Name { get => name; set => name = value ?? string.Empty; }

    /// <summary>Gets or sets the slot activation hotkey.</summary>
    public Hotkey ProjectHotkey { get => projectHotkey; set => projectHotkey = value ?? new Hotkey(); }

    /// <summary>Gets or sets the preference snapshot stored by this slot.</summary>
    public SnappingToolsPreferences Preferences
    {
        get => preferences;
        set => preferences = value ?? new SnappingToolsPreferences();
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new SnappingToolsSaveSlot
        {
            Name = Name,
            ProjectHotkey = (Hotkey)ProjectHotkey.Clone(),
            Preferences = (SnappingToolsPreferences)Preferences.Clone(),
        };
    }
}

/// <summary>Serializable Geometry Dashboard preferences and ordered save slots.</summary>
public sealed class SnappingToolsProject
{
    private SnappingToolsPreferences currentPreferences;
    private IEnumerable<RelevantObjectsGenerator>? generators;

    /// <summary>Creates a project with legacy default preferences and no save slots.</summary>
    public SnappingToolsProject()
    {
        currentPreferences = new SnappingToolsPreferences();
    }

    /// <summary>Gets or sets the active preference state.</summary>
    public SnappingToolsPreferences CurrentPreferences
    {
        get => currentPreferences;
        set => currentPreferences = value ?? new SnappingToolsPreferences();
    }

    /// <summary>Gets the saved slots in their persisted order.</summary>
    public List<SnappingToolsSaveSlot> SaveSlots { get; set; } = [];

    /// <summary>Associates live generators so preferences can be applied or captured.</summary>
    /// <param name="generators">The generator instances owned by the calculation engine.</param>
    public void SetGenerators(IEnumerable<RelevantObjectsGenerator>? generators)
    {
        this.generators = generators;
        if (this.generators is not null) CurrentPreferences.ApplyGeneratorSettings(this.generators);
    }

    /// <summary>Replaces active preferences with an independent cloned snapshot.</summary>
    /// <param name="preferences">The preferences to clone.</param>
    /// <exception cref="ArgumentNullException"><paramref name="preferences" /> is <see langword="null" />.</exception>
    public void SetCurrentPreferences(SnappingToolsPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        CurrentPreferences = (SnappingToolsPreferences)preferences.Clone();
        if (generators is not null) CurrentPreferences.ApplyGeneratorSettings(generators);
    }

    /// <summary>Captures live generator settings and returns the active preferences object.</summary>
    /// <returns>The current preferences after generator settings are synchronized.</returns>
    public SnappingToolsPreferences GetCurrentPreferences()
    {
        if (generators is not null) CurrentPreferences.SaveGeneratorSettings(generators);

        return CurrentPreferences;
    }

    /// <summary>Synchronizes live settings and returns this project for persistence.</summary>
    /// <returns>This mutable project instance.</returns>
    public SnappingToolsProject GetThis()
    {
        if (generators is not null) CurrentPreferences.SaveGeneratorSettings(generators);

        return this;
    }

    /// <summary>Copies active preferences into a named slot.</summary>
    /// <param name="saveSlot">The destination slot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="saveSlot" /> is <see langword="null" />.</exception>
    public void SaveToSlot(SnappingToolsSaveSlot saveSlot)
    {
        ArgumentNullException.ThrowIfNull(saveSlot);
        saveSlot.Preferences = (SnappingToolsPreferences)GetCurrentPreferences().Clone();
    }

    /// <summary>Loads a slot's independent preferences into the active project.</summary>
    /// <param name="saveSlot">The source slot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="saveSlot" /> is <see langword="null" />.</exception>
    public void LoadFromSlot(SnappingToolsSaveSlot saveSlot)
    {
        ArgumentNullException.ThrowIfNull(saveSlot);
        SetCurrentPreferences(saveSlot.Preferences);
    }
}
