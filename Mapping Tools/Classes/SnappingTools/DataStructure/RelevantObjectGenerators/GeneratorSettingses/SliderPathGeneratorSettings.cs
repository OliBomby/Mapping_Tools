namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses {
    public class SliderPathGeneratorSettings : GeneratorSettings {
        private double _pointDensity;
        public double PointDensity {
            get => _pointDensity;
            set => Set(ref _pointDensity, value);
        }
    }
}