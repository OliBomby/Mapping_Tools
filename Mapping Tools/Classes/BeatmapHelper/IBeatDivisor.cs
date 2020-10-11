using System;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public interface IBeatDivisor : IEquatable<IBeatDivisor> {
        double GetValue();
    }
}