using System;

namespace Mapping_Tools.Classes.HitsoundStuff.Effects {
    public class EffectParameter {
        private float currentValue;

        public EffectParameter(float defaultValue, float minimum, float maximum, string description) {
            Min = minimum;
            Max = maximum;
            Description = description;
            CurrentValue = defaultValue;
        }

        public float Min { get; }
        public float Max { get; }
        public string Description { get; }

        public float CurrentValue {
            get => currentValue;
            set {
                if (value < Min || value > Max)
                    throw new ArgumentOutOfRangeException(nameof(CurrentValue));
                if (currentValue != value)
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                currentValue = value;
            }
        }

        public event EventHandler ValueChanged;
    }
}