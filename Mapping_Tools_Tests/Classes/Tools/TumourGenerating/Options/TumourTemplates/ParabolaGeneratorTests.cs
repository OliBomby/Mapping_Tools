using Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates;
using NUnit.Framework;

namespace Mapping_Tools_Tests.Classes.Tools.TumourGenerating.Options.TumourTemplates;

[TestFixture]
public class ParabolaGeneratorTests {
    [Test]
    public void TestDistanceFunction() {
        var template = new ParabolaTemplate { Length = 1, Width = 1 };
        var distanceFunc = template.GetDistanceRelation();

        Assert.That(distanceFunc(0), Is.EqualTo(0));
        Assert.That(distanceFunc(0.5), Is.EqualTo(0.5));
        Assert.That(distanceFunc(1), Is.EqualTo(1));
    }
}