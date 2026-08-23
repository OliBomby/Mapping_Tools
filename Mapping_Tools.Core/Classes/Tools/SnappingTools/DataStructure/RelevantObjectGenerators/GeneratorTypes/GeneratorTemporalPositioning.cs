namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

/// <summary>Describes how a generated object's time is derived from its parents.</summary>
public enum GeneratorTemporalPositioning
{
    /// <summary>Use the earliest parent time.</summary>
    Before,

    /// <summary>Use the average parent time.</summary>
    Average,

    /// <summary>Use the latest parent time.</summary>
    After,

    /// <summary>Let the generated object supply a custom time.</summary>
    Custom,
}
