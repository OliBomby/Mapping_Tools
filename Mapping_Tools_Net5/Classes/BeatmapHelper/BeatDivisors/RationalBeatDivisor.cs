
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.BeatmapHelper.BeatDivisors {
    public class RationalBeatDivisor : IBeatDivisor {
        /// <summary>
        /// The number above the line in a vulgar fraction showing how many of the parts indicated by the denominator are taken, for example, 2 in 2/3.
        /// </summary>
        public readonly int Numerator;

        /// <summary>
        /// The number below the line in a vulgar fraction; a divisor.
        /// </summary>
        public readonly int Denominator;

        public RationalBeatDivisor(int denominator) {
            Numerator = 1;
            Denominator = denominator;
        }

        [JsonConstructor]
        public RationalBeatDivisor(int numerator, int denominator) {
            Numerator = numerator;
            Denominator = denominator;
        }

        public static implicit operator RationalBeatDivisor(int denominator) {
            return new RationalBeatDivisor(denominator);
        }

        public double GetValue() {
            return (double) Numerator / Denominator;
        }

        protected bool Equals(RationalBeatDivisor other) {
            return Numerator == other.Numerator && Denominator == other.Denominator;
        }

        public bool Equals(IBeatDivisor other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other is RationalBeatDivisor otherRational) return Equals(otherRational);
            return false;
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((RationalBeatDivisor) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (Numerator * 397) ^ Denominator;
            }
        }

        public static IBeatDivisor[] GetDefaultBeatDivisors() {
            return new IBeatDivisor[] {new RationalBeatDivisor(16), new RationalBeatDivisor(12)};
        }
    }
}