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

