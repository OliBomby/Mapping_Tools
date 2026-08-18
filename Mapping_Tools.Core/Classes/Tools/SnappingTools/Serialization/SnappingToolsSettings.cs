using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.SystemTools;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization;

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
    Everything = 1 << 4
}

/// <summary>Selects which editor hit objects become root objects.</summary>
public enum SelectedHitObjectMode
{
    /// <summary>Use every hit object visible around the editor time.</summary>
    AllwaysAllVisible,
    /// <summary>Use selected objects when any are selected, otherwise visible objects.</summary>
    VisibleOrSelected,
    /// <summary>Use only selected objects.</summary>
    OnlySelected
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
    OsuActivated
}

/// <summary>Stores the persistent appearance and input settings of one dashboard.</summary>
public sealed class SnappingToolsPreferences : BindableBase, ICloneable
{
    private Dictionary<string, RelevantObjectPreferences> _relevantObjectPreferences;
    private Dictionary<Type, GeneratorSettings> _generatorSettings;
    private Hotkey _snapHotkey;
    private Hotkey _selectHotkey;
    private Hotkey _lockHotkey;
    private Hotkey _inheritHotkey;
    private Hotkey _refreshHotkey;
    private double _offsetLeft;
    private double _offsetTop;
    private double _offsetRight;
    private double _offsetBottom;
    private double _acceptableDifference;
    private bool _keepRunning;
    private bool _visiblePlayfieldBoundary;
    private bool _debugEnabled;
    private ViewMode _keyDownViewMode;
    private ViewMode _keyUpViewMode;
    private SelectedHitObjectMode _selectedHitObjectMode;
    private UpdateMode _updateMode;
    private int _inceptionLevel;

    /// <summary>Creates the legacy default Geometry Dashboard settings.</summary>
    public SnappingToolsPreferences()
    {
        _relevantObjectPreferences = new Dictionary<string, RelevantObjectPreferences>
        {
            [RelevantPoint.PreferencesNameStatic] = new RelevantObjectPreferences
            {
                Name = RelevantPoint.PreferencesNameStatic,
                Color = RgbaColour.FromArgb(255, 0, 255, 255),
                Dashstyle = DashStylesEnum.Solid,
                Opacity = 0.8,
                Size = 5,
                Thickness = 3,
                HasSizeOption = true
            },
            [RelevantLine.PreferencesNameStatic] = new RelevantObjectPreferences
            {
                Name = RelevantLine.PreferencesNameStatic,
                Color = RgbaColour.FromArgb(255, 124, 252, 0),
                Dashstyle = DashStylesEnum.Dash,
                Opacity = 0.8,
                Thickness = 3,
                HasSizeOption = false
            },
            [RelevantCircle.PreferencesNameStatic] = new RelevantObjectPreferences
            {
                Name = RelevantCircle.PreferencesNameStatic,
                Color = RgbaColour.FromArgb(255, 255, 0, 0),
                Dashstyle = DashStylesEnum.Dash,
                Opacity = 0.8,
                Thickness = 3,
                HasSizeOption = false
            }
        };

        _generatorSettings = new Dictionary<Type, GeneratorSettings>();
        _snapHotkey = new Hotkey(56, 0);
        _selectHotkey = new Hotkey(57, 0);
        _lockHotkey = new Hotkey(57, 4);
        _inheritHotkey = new Hotkey(57, 1);
        _refreshHotkey = new Hotkey(45, 0);
        _offsetTop = 1;
        _offsetBottom = 1;
        _acceptableDifference = 2;
        _keyDownViewMode = ViewMode.Parents;
        _keyUpViewMode = ViewMode.Everything;
        _selectedHitObjectMode = SelectedHitObjectMode.AllwaysAllVisible;
        _updateMode = UpdateMode.TimeChange;
        _inceptionLevel = 5;
    }

    /// <summary>Gets or sets appearance settings by stable preference-group name.</summary>
    public Dictionary<string, RelevantObjectPreferences> RelevantObjectPreferences
    {
        get => _relevantObjectPreferences;
        set => Set(ref _relevantObjectPreferences, value ?? []);
    }

    /// <summary>Gets or sets generator settings keyed by concrete generator type.</summary>
    public Dictionary<Type, GeneratorSettings> GeneratorSettings
    {
        get => _generatorSettings;
        set => Set(ref _generatorSettings, value ?? []);
    }

    /// <summary>Gets or sets the activation/snap key.</summary>
    public Hotkey SnapHotkey { get => _snapHotkey; set => Set(ref _snapHotkey, value ?? new Hotkey()); }
    /// <summary>Gets or sets the selection key.</summary>
    public Hotkey SelectHotkey { get => _selectHotkey; set => Set(ref _selectHotkey, value ?? new Hotkey()); }
    /// <summary>Gets or sets the lock/unlock key.</summary>
    public Hotkey LockHotkey { get => _lockHotkey; set => Set(ref _lockHotkey, value ?? new Hotkey()); }
    /// <summary>Gets or sets the inheritability key.</summary>
    public Hotkey InheritHotkey { get => _inheritHotkey; set => Set(ref _inheritHotkey, value ?? new Hotkey()); }
    /// <summary>Gets or sets the refresh key.</summary>
    public Hotkey RefreshHotkey { get => _refreshHotkey; set => Set(ref _refreshHotkey, value ?? new Hotkey()); }

    /// <summary>Gets or sets the left editor-box correction in pixels.</summary>
    public double OffsetLeft { get => _offsetLeft; set => Set(ref _offsetLeft, value); }
    /// <summary>Gets or sets the top editor-box correction in pixels.</summary>
    public double OffsetTop { get => _offsetTop; set => Set(ref _offsetTop, value); }
    /// <summary>Gets or sets the right editor-box correction in pixels.</summary>
    public double OffsetRight { get => _offsetRight; set => Set(ref _offsetRight, value); }
    /// <summary>Gets or sets the bottom editor-box correction in pixels.</summary>
    public double OffsetBottom { get => _offsetBottom; set => Set(ref _offsetBottom, value); }

    /// <summary>Gets the four stored editor-box corrections as a box.</summary>
    public Box2 OverlayOffset => new(OffsetLeft, OffsetTop, OffsetRight, OffsetBottom);

    /// <summary>Gets or sets the duplicate-distance tolerance in editor pixels.</summary>
    public double AcceptableDifference { get => _acceptableDifference; set => Set(ref _acceptableDifference, value); }
    /// <summary>Gets or sets whether the engine continues when the dashboard is hidden.</summary>
    public bool KeepRunning { get => _keepRunning; set => Set(ref _keepRunning, value); }
    /// <summary>Gets or sets whether the osu! playfield boundary is displayed.</summary>
    public bool VisiblePlayfieldBoundary { get => _visiblePlayfieldBoundary; set => Set(ref _visiblePlayfieldBoundary, value); }
    /// <summary>Gets or sets whether platform debugging visuals are enabled.</summary>
    public bool DebugEnabled { get => _debugEnabled; set => Set(ref _debugEnabled, value); }
    /// <summary>Gets or sets the graph shown while the snap key is down.</summary>
    public ViewMode KeyDownViewMode { get => _keyDownViewMode; set => Set(ref _keyDownViewMode, value); }
    /// <summary>Gets or sets the graph shown while the snap key is up.</summary>
    public ViewMode KeyUpViewMode { get => _keyUpViewMode; set => Set(ref _keyUpViewMode, value); }
    /// <summary>Gets or sets the rule for selecting root hit objects.</summary>
    public SelectedHitObjectMode SelectedHitObjectMode { get => _selectedHitObjectMode; set => Set(ref _selectedHitObjectMode, value); }
    /// <summary>Gets or sets the refresh trigger.</summary>
    public UpdateMode UpdateMode { get => _updateMode; set => Set(ref _updateMode, value); }
    /// <summary>Gets or sets the number of generated layers, including the root layer.</summary>
    public int InceptionLevel { get => _inceptionLevel; set => Set(ref _inceptionLevel, value); }

    /// <summary>Gets a configured preference group or a new empty fallback.</summary>
    /// <param name="input">The preference-group name.</param>
    /// <returns>The stored preferences or a default empty instance.</returns>
    public RelevantObjectPreferences GetReleventObjectPreferences(string input) =>
        RelevantObjectPreferences.TryGetValue(input, out RelevantObjectPreferences? output)
            ? output
            : new RelevantObjectPreferences();

    /// <summary>Copies saved settings into the supplied generator instances.</summary>
    /// <param name="generators">The live generators to configure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generators"/> is <see langword="null"/>.</exception>
    public void ApplyGeneratorSettings(IEnumerable<RelevantObjectsGenerator> generators)
    {
        ArgumentNullException.ThrowIfNull(generators);

        foreach (RelevantObjectsGenerator generator in generators)
        {
            if (GeneratorSettings.TryGetValue(generator.GetType(), out GeneratorSettings? settings))
            {
                settings.CopyTo(generator.Settings);
            }
        }
    }

    /// <summary>Stores each live generator's settings under its concrete type.</summary>
    /// <param name="generators">The live generators to snapshot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="generators"/> is <see langword="null"/>.</exception>
    public void SaveGeneratorSettings(IEnumerable<RelevantObjectsGenerator> generators)
    {
        ArgumentNullException.ThrowIfNull(generators);

        foreach (RelevantObjectsGenerator generator in generators)
        {
            GeneratorSettings[generator.GetType()] = generator.Settings;
        }
    }

    /// <inheritdoc/>
    public object Clone()
    {
        SnappingToolsPreferences clone = (SnappingToolsPreferences)MemberwiseClone();
        clone.GeneratorSettings = new Dictionary<Type, GeneratorSettings>();
        foreach ((Type key, GeneratorSettings value) in GeneratorSettings)
        {
            clone.GeneratorSettings.Add(key, (GeneratorSettings)value.Clone());
        }

        clone.RelevantObjectPreferences = new Dictionary<string, RelevantObjectPreferences>();
        foreach ((string key, RelevantObjectPreferences value) in RelevantObjectPreferences)
        {
            clone.RelevantObjectPreferences.Add(key, (RelevantObjectPreferences)value.Clone());
        }

        clone._snapHotkey = (Hotkey)SnapHotkey.Clone();
        clone._selectHotkey = (Hotkey)SelectHotkey.Clone();
        clone._lockHotkey = (Hotkey)LockHotkey.Clone();
        clone._inheritHotkey = (Hotkey)InheritHotkey.Clone();
        clone._refreshHotkey = (Hotkey)RefreshHotkey.Clone();

        return clone;
    }
}

/// <summary>A named saved snapshot of Geometry Dashboard preferences.</summary>
public sealed class SnappingToolsSaveSlot : BindableBase, ICloneable
{
    private string _name = string.Empty;
    private Hotkey _projectHotkey = new();
    private SnappingToolsPreferences _preferences = new();

    /// <summary>Gets or sets the user-visible slot name.</summary>
    public string Name { get => _name; set => Set(ref _name, value ?? string.Empty); }

    /// <summary>Gets or sets the slot activation hotkey.</summary>
    public Hotkey ProjectHotkey { get => _projectHotkey; set => Set(ref _projectHotkey, value ?? new Hotkey()); }

    /// <summary>Gets the owning project while the slot is attached to a project collection.</summary>
    [Newtonsoft.Json.JsonIgnore]
    public SnappingToolsProject? ParentProject { get; set; }

    /// <summary>Gets or sets the preference snapshot stored by this slot.</summary>
    public SnappingToolsPreferences Preferences
    {
        get => _preferences;
        set => _preferences = value ?? new SnappingToolsPreferences();
    }

    /// <inheritdoc/>
    public object Clone() => new SnappingToolsSaveSlot
    {
        Name = Name,
        ProjectHotkey = (Hotkey)ProjectHotkey.Clone(),
        Preferences = (SnappingToolsPreferences)Preferences.Clone()
    };
}

/// <summary>Serializable Geometry Dashboard preferences and ordered save slots.</summary>
public sealed class SnappingToolsProject : BindableBase
{
    private SnappingToolsPreferences _currentPreferences;
    private IEnumerable<RelevantObjectsGenerator>? _generators;

    /// <summary>Creates a project with legacy default preferences and no save slots.</summary>
    public SnappingToolsProject()
    {
        _currentPreferences = new SnappingToolsPreferences();
        SaveSlots = new ObservableCollection<SnappingToolsSaveSlot>();
        SaveSlots.CollectionChanged += SaveSlotsOnCollectionChanged;
    }

    /// <summary>Gets or sets the active preference state.</summary>
    public SnappingToolsPreferences CurrentPreferences
    {
        get => _currentPreferences;
        set => Set(ref _currentPreferences, value ?? new SnappingToolsPreferences());
    }

    /// <summary>Gets the saved slots in their persisted order.</summary>
    public ObservableCollection<SnappingToolsSaveSlot> SaveSlots { get; }

    private void SaveSlotsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (SnappingToolsSaveSlot oldItem in e.OldItems)
            {
                oldItem.ParentProject = null;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (SnappingToolsSaveSlot newItem in e.NewItems)
            {
                newItem.ParentProject = this;
            }
        }
    }

    /// <summary>Associates live generators so preferences can be applied or captured.</summary>
    /// <param name="generators">The generator instances owned by the calculation engine.</param>
    public void SetGenerators(IEnumerable<RelevantObjectsGenerator>? generators)
    {
        _generators = generators;
        if (_generators is not null)
        {
            CurrentPreferences.ApplyGeneratorSettings(_generators);
        }
    }

    /// <summary>Replaces active preferences with an independent cloned snapshot.</summary>
    /// <param name="preferences">The preferences to clone.</param>
    /// <exception cref="ArgumentNullException"><paramref name="preferences"/> is <see langword="null"/>.</exception>
    public void SetCurrentPreferences(SnappingToolsPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        CurrentPreferences = (SnappingToolsPreferences)preferences.Clone();
        if (_generators is not null)
        {
            CurrentPreferences.ApplyGeneratorSettings(_generators);
        }
    }

    /// <summary>Captures live generator settings and returns the active preferences object.</summary>
    /// <returns>The current preferences after generator settings are synchronized.</returns>
    public SnappingToolsPreferences GetCurrentPreferences()
    {
        if (_generators is not null)
        {
            CurrentPreferences.SaveGeneratorSettings(_generators);
        }

        return CurrentPreferences;
    }

    /// <summary>Synchronizes live settings and returns this project for persistence.</summary>
    /// <returns>This mutable project instance.</returns>
    public SnappingToolsProject GetThis()
    {
        if (_generators is not null)
        {
            CurrentPreferences.SaveGeneratorSettings(_generators);
        }

        return this;
    }

    /// <summary>Copies active preferences into a named slot.</summary>
    /// <param name="saveSlot">The destination slot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="saveSlot"/> is <see langword="null"/>.</exception>
    public void SaveToSlot(SnappingToolsSaveSlot saveSlot)
    {
        ArgumentNullException.ThrowIfNull(saveSlot);
        saveSlot.Preferences = (SnappingToolsPreferences)GetCurrentPreferences().Clone();
    }

    /// <summary>Loads a slot's independent preferences into the active project.</summary>
    /// <param name="saveSlot">The source slot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="saveSlot"/> is <see langword="null"/>.</exception>
    public void LoadFromSlot(SnappingToolsSaveSlot saveSlot)
    {
        ArgumentNullException.ThrowIfNull(saveSlot);
        SetCurrentPreferences(saveSlot.Preferences);
    }
}
