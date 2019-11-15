using System.ComponentModel;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses {
    public class SliderPathGeneratorSettings : GeneratorSettings {
        private double _pointDensity;
        [DisplayName("Point Density")]
        [Description("Specifies how many points will be generated per osu! pixel of sliderpath.")]
        public double PointDensity {
            get => _pointDensity;
            set => Set(ref _pointDensity, value);
        }
    }
}