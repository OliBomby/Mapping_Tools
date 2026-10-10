using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;

/// <summary>Serializable Geometry Dashboard preferences and ordered save slots.</summary>
public class GeometryDashboardEngineOptions
{
    private IEnumerable<RelevantObjectsGenerator>? generators;

    /// <summary>Creates a project with legacy default preferences and no save slots.</summary>
    public GeometryDashboardEngineOptions()
    {
        CurrentPreferences = new GeometryDashboardPreferences();
    }

    /// <summary>Gets or sets the active preference state.</summary>
    public GeometryDashboardPreferences CurrentPreferences { get; private set; }

    /// <summary>Gets the saved slots in their persisted order.</summary>
    public List<GeometryDashboardSaveSlot> SaveSlots { get; } = [];

    /// <summary>Associates live generators so preferences can be applied or captured.</summary>
    /// <param name="newGenerators">The generator instances owned by the calculation engine.</param>
    public void SetGenerators(IEnumerable<RelevantObjectsGenerator>? newGenerators)
    {
        generators = newGenerators;
        if (generators is not null) CurrentPreferences.ApplyGeneratorSettings(generators);
    }

    /// <summary>Replaces active preferences with an independent cloned snapshot.</summary>
    /// <param name="preferences">The preferences to clone.</param>
    /// <exception cref="ArgumentNullException"><paramref name="preferences" /> is <see langword="null" />.</exception>
    public void SetCurrentPreferences(GeometryDashboardPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        CurrentPreferences = (GeometryDashboardPreferences)preferences.Clone();
        if (generators is not null) CurrentPreferences.ApplyGeneratorSettings(generators);
    }

    /// <summary>Captures live generator settings and returns the active preferences object.</summary>
    /// <returns>The current preferences after generator settings are synchronized.</returns>
    public GeometryDashboardPreferences GetCurrentPreferences()
    {
        if (generators is not null) CurrentPreferences.SaveGeneratorSettings(generators);

        return CurrentPreferences;
    }

    /// <summary>Synchronizes live settings and returns this project for persistence.</summary>
    /// <returns>This mutable project instance.</returns>
    public GeometryDashboardEngineOptions GetThis()
    {
        if (generators is not null) CurrentPreferences.SaveGeneratorSettings(generators);

        return this;
    }

    /// <summary>Copies active preferences into a named slot.</summary>
    /// <param name="saveSlot">The destination slot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="saveSlot" /> is <see langword="null" />.</exception>
    public void SaveToSlot(GeometryDashboardSaveSlot saveSlot)
    {
        ArgumentNullException.ThrowIfNull(saveSlot);
        saveSlot.Preferences = (GeometryDashboardPreferences)GetCurrentPreferences().Clone();
    }

    /// <summary>Loads a slot's independent preferences into the active project.</summary>
    /// <param name="saveSlot">The source slot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="saveSlot" /> is <see langword="null" />.</exception>
    public void LoadFromSlot(GeometryDashboardSaveSlot saveSlot)
    {
        ArgumentNullException.ThrowIfNull(saveSlot);
        SetCurrentPreferences(saveSlot.Preferences);
    }
}
