using Mapping_Tools.Domain.Beatmaps;
using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.Beatmaps.Parsing.V14;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Tests.Beatmaps;

[TestFixture]
public class BeatmapExtensionTests {
    [Test]
    public void QueryTimeCodeTest() {
        var path = Path.Join("Resources", "ComplicatedTestMap.osu");
        var beatmap = new BeatmapDecoder().Decode(File.ReadAllText(path));

        var hos = beatmap.QueryTimeCode("00:56:823 (1,2,1,2) - ").ToArray();
        Assert.That(hos, Is.Empty);

        hos = beatmap.QueryTimeCode("00:00:015 (1,2,3,4,5,1) - ").ToArray();
        Assert.That(hos, Has.Length.EqualTo(6));
    }

    [Test]
    public void FixBreakPeriodsTest() {
        var path = Path.Join("Resources", "ComplicatedTestMap.osu");
        var beatmap = new BeatmapDecoder().Decode(File.ReadAllText(path));

        beatmap.Storyboard.BreakPeriods.Add(new Break(-10000, -5000));
        beatmap.Storyboard.BreakPeriods.Add(new Break(0, 2000));
        beatmap.Storyboard.BreakPeriods.Add(new Break(0, 20000));

        Assert.That(beatmap.Storyboard.BreakPeriods, Has.Count.EqualTo(3));

        beatmap.FixBreakPeriods();

        Assert.That(beatmap.Storyboard.BreakPeriods, Is.Empty);

        beatmap.HitObjects.RemoveAll(ho => ho.StartTime > 15 && ho.StartTime < 2000);
        beatmap.Storyboard.BreakPeriods.Add(new Break(-10000, -5000));
        beatmap.Storyboard.BreakPeriods.Add(new Break(0, 2000));
        beatmap.Storyboard.BreakPeriods.Add(new Break(0, 20000));

        Assert.That(beatmap.HitObjects, Has.Count.EqualTo(2));
        Assert.That(beatmap.Storyboard.BreakPeriods, Has.Count.EqualTo(3));

        beatmap.FixBreakPeriods();
        var approachTime = beatmap.Difficulty.ApproachTime;
        var prevEndTime = beatmap.HitObjects[0].EndTime;

        Assert.That(beatmap.Storyboard.BreakPeriods, Has.Count.EqualTo(1));
        Assert.That(beatmap.Storyboard.BreakPeriods[0].StartTime, Is.EqualTo(prevEndTime + 200).Within(Precision.DoubleEpsilon));
        Assert.That(beatmap.Storyboard.BreakPeriods[0].EndTime, Is.EqualTo(2109 - approachTime).Within(Precision.DoubleEpsilon));
    }
}