using System;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels
{
    public class ArCalculatorVm : BindableBase
    {
        private double bpm;
        private double calculatedApproachRate;

        public ArCalculatorVm()
        {
            RunCommand = new CommandImplementation(_ =>
            {
                const double secondsPerMinute = 60.0;
                const double msPerSecond = 1000.0;

                const double tickMultiplier = 1.5;
                const double arSlope = -0.00666;
                const double arIntercept = 12.65;

                var msBetweenTicks = (secondsPerMinute / bpm) * msPerSecond;
                var ar = (arSlope * msBetweenTicks * tickMultiplier) + arIntercept;

                // Cut off the negative values
                ar = Math.Max(ar, 0);

                CalculatedApproachRate = Math.Round(ar, 2, MidpointRounding.AwayFromZero);
            });
        }

        public double Bpm
        {
            get => bpm;
            set => Set(ref bpm, value);
        }

        public double CalculatedApproachRate
        {
            get => calculatedApproachRate;
            set => Set(ref calculatedApproachRate, value);
        }

        [JsonIgnore]
        public CommandImplementation RunCommand { get; }
    }
}
