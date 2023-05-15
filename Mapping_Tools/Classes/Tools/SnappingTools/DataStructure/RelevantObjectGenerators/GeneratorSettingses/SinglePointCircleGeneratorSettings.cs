﻿using System.ComponentModel;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses
{
    public class SinglePointCircleGeneratorSettings : GeneratorSettings
    {
        private double radius;
        [DisplayName("Radius")]
        [Description("The radius of the circle in osu! pixels.")]
        public double Radius
        {
            get => radius;
            set => Set(ref radius, value);
        }

        public SinglePointCircleGeneratorSettings()
        {
            Radius = 0;
        }

        public override object Clone()
        {
            return new SinglePointCircleGeneratorSettings
            {
                Generator = Generator,
                IsActive = IsActive,
                IsSequential = IsSequential,
                IsDeep = IsDeep,
                RelevancyRatio = RelevancyRatio,
                GeneratesInheritable = GeneratesInheritable,
                InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(),
                Radius = Radius
            };
        }
    }
}