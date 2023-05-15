﻿using System.ComponentModel;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses {
    public class SliderPathGeneratorSettings : GeneratorSettings {
        private double pointDensity;
        [DisplayName("Point Density")]
        [Description("Specifies how many points will be generated per osu! pixel of sliderpath.")]
        public double PointDensity {
            get => pointDensity;
            set => Set(ref pointDensity, value);
        }

        public override object Clone() {
            return new SliderPathGeneratorSettings {Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep, 
                RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
                InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(),
                PointDensity = PointDensity
            };
        }
    }
}