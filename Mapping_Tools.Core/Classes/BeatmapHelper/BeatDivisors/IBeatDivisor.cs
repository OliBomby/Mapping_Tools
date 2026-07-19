using System;

namespace Mapping_Tools.Classes.BeatmapHelper.BeatDivisors {
#nullable disable

    public interface IBeatDivisor : IEquatable<IBeatDivisor> {
        double GetValue();
    }
}
