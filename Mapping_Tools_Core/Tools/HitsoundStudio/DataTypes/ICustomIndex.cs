using System;
using System.Collections.Generic;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.DataTypes {
    public interface ICustomIndex : ICloneable {
        int Index { get; set; }
        Dictionary<string, HashSet<ISampleGeneratingArgs>> Samples { get; }

        bool Fits(ICustomIndex other);
        bool CanMerge(ICustomIndex other);
        void MergeWith(ICustomIndex other);
        string GetNumberExtension();
    }
}