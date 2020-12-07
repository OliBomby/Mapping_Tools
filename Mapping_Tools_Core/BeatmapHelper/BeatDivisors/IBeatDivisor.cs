using System;

namespace Mapping_Tools_Core.BeatmapHelper.BeatDivisors {
    public interface IBeatDivisor : IEquatable<IBeatDivisor> {
        double GetValue();
    }
}