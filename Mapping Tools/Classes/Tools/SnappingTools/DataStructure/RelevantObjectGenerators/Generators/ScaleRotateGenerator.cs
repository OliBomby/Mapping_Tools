using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class ScaleRotateGenerator : RelevantObjectsGenerator {
        public override string Name => "Scale & Rotate around a Fixed Point";
        public override string Tooltip => "Spins and scales any virtual object around a fixed point by a specified angle and scalar. In the settings you can set the angle, scalar and extra rules for selecting the fixed point.";
        public override GeneratorType GeneratorType => GeneratorType.Advanced;

        private ScaleRotateGeneratorSettings MySettings => (ScaleRotateGeneratorSettings) Settings;

        /// <summary>
        /// Initializes ScaleRotateGenerator with a custom settings object
        /// </summary>
        public ScaleRotateGenerator() : base(new ScaleRotateGeneratorSettings()) {
            Settings.Generator = this;

            Settings.IsActive = true;
            Settings.IsDeep = true;
            MySettings.Angle = 180;
            MySettings.Scalar = 1;
            MySettings.OriginInputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, NeedLocked = true, NeedGeneratedNotByThis = true});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            // Any point can be the origin
            if (MySettings.OriginInputPredicate.Check(point1, this) &&
                MySettings.OtherInputPredicate.Check(point2, this)) {
                return new RelevantPoint( Matrix2.Mult(Matrix2.CreateRotation(MathHelper.DegreesToRadians(MySettings.Angle)), point2.Child - point1.Child) * MySettings.Scalar + point1.Child);
            }

            if (MySettings.OriginInputPredicate.Check(point2, this) &&
                MySettings.OtherInputPredicate.Check(point1, this)) {
                return new RelevantPoint( Matrix2.Mult(Matrix2.CreateRotation(MathHelper.DegreesToRadians(MySettings.Angle)), point1.Child - point2.Child) * MySettings.Scalar + point2.Child);
            }

            return null;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantPoint origin, RelevantLine line) {
            return !MySettings.OriginInputPredicate.Check(origin, this) || !MySettings.OtherInputPredicate.Check(line, this) ? null : 
                new RelevantLine(Line2.FromPoints(Matrix2.Mult(Matrix2.CreateRotation(MathHelper.DegreesToRadians(MySettings.Angle)), line.Child.PositionVector - origin.Child) * MySettings.Scalar + origin.Child, 
                    Matrix2.Mult(Matrix2.CreateRotation(MathHelper.DegreesToRadians(MySettings.Angle)), line.Child.PositionVector + line.Child.DirectionVector - origin.Child) * MySettings.Scalar + origin.Child));
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantCircle GetRelevantObjects(RelevantPoint origin, RelevantCircle circle) {
            return !MySettings.OriginInputPredicate.Check(origin, this) || !MySettings.OtherInputPredicate.Check(circle, this) ? null : 
                new RelevantCircle(new Circle(Matrix2.Mult(Matrix2.CreateRotation(MathHelper.DegreesToRadians(MySettings.Angle)), circle.Child.Centre - origin.Child) * MySettings.Scalar + origin.Child, circle.Child.Radius * MySettings.Scalar));
        }
    }
}
