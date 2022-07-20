using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates {
    public abstract class TumourTemplateBase : ITumourTemplate {
        public double Length { get; set; }

        public double Width { get; set; }

        public abstract Vector2 GetOffset(double t);

        public abstract double GetLength();

        public abstract double GetDefaultSpan();

        public abstract int GetDetailLevel();

        public abstract IEnumerable<double> GetCriticalPoints();

        public abstract List<Vector2> GetReconstructionHint();

        public abstract PathType GetReconstructionHintPathType();

        public abstract Func<double, double> GetDistanceRelation();
    }
}