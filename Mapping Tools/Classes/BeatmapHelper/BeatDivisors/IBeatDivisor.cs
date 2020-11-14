using System;

namespace Mapping_Tools.Classes.BeatmapHelper.BeatDivisors {
    public interface IBeatDivisor : IEquatable<IBeatDivisor> {
        double GetValue();
    }
}