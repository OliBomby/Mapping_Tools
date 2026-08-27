namespace Mapping_Tools.Core.Tools.TumourGenerator.Templates;

/// <summary>Marks templates that require initialization after their dimensions are set.</summary>
public interface IRequireInit
{
    /// <summary>Recomputes any cached shape values from the current template properties.</summary>
    void Init();
}

