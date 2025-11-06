using System.ComponentModel;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;

public class ScaleRotateGeneratorSettings : GeneratorSettings {
    private double angle;
    [DisplayName("Angle")]
    [Description("The angle in degrees to rotate by. Rotation by a positive angle rotates the object counterclockwise, whereas rotation by a negative angle rotates the object clockwise.")]
    public double Angle {
        get => angle;
        set => Set(ref angle, value);
    }

    private double scalar;
    [DisplayName("Scalar")]
    [Description("The scalar for the uniform scaling.")]
    public double Scalar {
        get => scalar;
        set => Set(ref scalar, value);
    }

    private SelectionPredicateCollection originInputPredicate;
    [DisplayName("Origin Input Selection")]
    [Description("Specifies extra rules that virtual lines need to obey to be used as the axis by this generator.")]
    public SelectionPredicateCollection OriginInputPredicate {
        get => originInputPredicate;
        set => Set(ref originInputPredicate, value);
    }
        
    private SelectionPredicateCollection otherInputPredicate;
    [DisplayName("Other Input Selection")]
    [Description("Specifies extra rules that virtual objects need to obey to get mirrored by this generator.")]
    public SelectionPredicateCollection OtherInputPredicate {
        get => otherInputPredicate;
        set => Set(ref otherInputPredicate, value);
    }

    public ScaleRotateGeneratorSettings() {
        Angle = 0;
        Scalar = 1;
        OriginInputPredicate = new SelectionPredicateCollection();
        OtherInputPredicate = new SelectionPredicateCollection();
    }

    public override object Clone() {
        return new ScaleRotateGeneratorSettings {Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep, 
            RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
            InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(),
            Angle = Angle, Scalar = Scalar, 
            OriginInputPredicate = (SelectionPredicateCollection)OriginInputPredicate.Clone(),
            OtherInputPredicate = (SelectionPredicateCollection)OtherInputPredicate.Clone()
        };
    }
}