using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard;

[TestClass]
public sealed class GeometryDashboardDomainTests
{
    [TestMethod]
    public void GeometryDashboardPreferences_Defaults_PreserveLegacyDashboardValues()
    {
        // Arrange
        GeometryDashboardPreferences preferences = new();

        // Act
        var pointPreferences = preferences.GetReleventObjectPreferences(RelevantPoint.PreferencesNameStatic);

        // Assert
        preferences.AcceptableDifference.Should().Be(2);
        preferences.InceptionLevel.Should().Be(5);
        preferences.SelectedHitObjectMode.Should().Be(SelectedHitObjectMode.AllwaysAllVisible);
        preferences.UpdateMode.Should().Be(UpdateMode.TimeChange);
        preferences.SnapHotkey!.Key.Should().Be(56);
        preferences.LockHotkey!.Modifiers.Should().Be(4);
        pointPreferences.Color.ToString().Should().Be("#FF00FFFF");
        pointPreferences.Dashstyle.Should().Be(DashStylesEnum.Solid);
    }

    [TestMethod]
    public void GeometryDashboardPreferences_Clone_WithClearedHotkey_PreservesClearedValue()
    {
        // Arrange
        GeometryDashboardPreferences preferences = new() { SnapHotkey = null };

        // Act
        var clone = (GeometryDashboardPreferences)preferences.Clone();

        // Assert
        clone.SnapHotkey.Should().BeNull();
    }

    [TestMethod]
    public void LayerCollection_AddSelectedPoints_GeneratesLineInNextLayer()
    {
        // Arrange
        RelevantObjectsGeneratorCollection generators = new([new LineGenerator()]);
        LayerCollection layers = new(generators, 0.01);
        layers.SetInceptionLevel(2);
        RelevantPoint first = new(new Vector2(100, 100)) { IsSelected = true, Time = 100 };
        RelevantPoint second = new(new Vector2(200, 100)) { IsSelected = true, Time = 200 };

        // Act
        layers.GetRootLayer().Add([first, second]);

        // Assert
        var line = layers.ObjectLayers[1].Objects.Values
            .SelectMany(objects => objects)
            .OfType<RelevantLine>()
            .Should().ContainSingle().Which;
        line.Child.PositionVector.Should().Be(new Vector2(100, 100));
        line.Child.DirectionVector.Should().Be(new Vector2(100, 0));
        line.ParentObjects.Should().BeEquivalentTo([first, second]);
    }

    [TestMethod]
    public void RelevantObject_ParentAssignment_RecomputesTimeAndRelevancy()
    {
        // Arrange
        RelevantPoint first = new(new Vector2(0, 0)) { Time = 100, Relevancy = 0.25 };
        RelevantPoint second = new(new Vector2(100, 0)) { Time = 200, Relevancy = 0.75 };
        RelevantPoint generated = new(new Vector2(50, 0)) { Generator = new LineGenerator() };

        // Act
        generated.ParentObjects = [first, second];

        // Assert
        generated.Time.Should().Be(150);
        generated.Relevancy.Should().Be(0.2);
    }

    [TestMethod]
    public void RelevantObject_GetLockedRelevantObject_DoesNotDetachTheSourceGraph()
    {
        // Arrange
        RelevantPoint parent = new(new Vector2(0, 0));
        RelevantPoint child = new(new Vector2(10, 10)) { ParentObjects = [parent] };
        parent.ChildObjects.Add(child);

        // Act
        var locked = (RelevantPoint)child.GetLockedRelevantObject();

        // Assert
        parent.ChildObjects.Should().Contain(child);
        child.ParentObjects.Should().Contain(parent);
        locked.ParentObjects.Should().BeEmpty();
        locked.ChildObjects.Should().BeEmpty();
        locked.IsLocked.Should().BeTrue();
    }

    [TestMethod]
    public void RelevantHitObject_GetLockedRelevantObject_ClonesTheMutableHitObject()
    {
        // Arrange
        HitObject hitObject = new()
        {
            Pos = new Vector2(100, 100),
            Time = 500,
            IsCircle = true,
            CurvePoints = [],
        };
        RelevantHitObject source = new(hitObject);

        // Act
        var locked = (RelevantHitObject)source.GetLockedRelevantObject();
        locked.HitObject.Pos = new Vector2(200, 200);
        locked.HitObject.Time = 750;

        // Assert
        source.HitObject.Pos.Should().Be(new Vector2(100, 100));
        source.HitObject.Time.Should().Be(500);
    }

    [TestMethod]
    public void RelevantHitObject_Difference_WithUninitializedCurvePoints_TreatsThemAsEmpty()
    {
        // Arrange
        RelevantHitObject first = new(new HitObject
        {
            IsCircle = true,
            Pos = new Vector2(100, 100),
        });
        RelevantHitObject second = new(new HitObject
        {
            IsCircle = true,
            Pos = new Vector2(100, 100),
        });

        // Act
        double difference = first.Difference(second);

        // Assert
        difference.Should().Be(0);
    }

    [TestMethod]
    public void RelevantObjectLayer_AddMoreThanMaximum_RejectsOverflowWithoutExceedingTheCap()
    {
        // Arrange
        LayerCollection layers = new(new RelevantObjectsGeneratorCollection([]), 0);
        var points = Enumerable.Range(0, layers.MaxObjects + 1)
            .Select(index => new RelevantPoint(new Vector2(index, 0)))
            .ToArray();

        // Act
        layers.GetRootLayer().Add(points);

        // Assert
        layers.GetRootLayer().Objects.GetCount().Should().Be(layers.MaxObjects);
        points[^1].Disposed.Should().BeTrue();
    }

    [TestMethod]
    public void GeometryGenerators_DegenerateInputs_ReturnFiniteOrEmptyResults()
    {
        // Arrange
        RelevantCircle circle = new(new Circle(new Vector2(100, 100), 25));
        RelevantHitObject hitObject = new(new HitObject
        {
            IsSlider = true,
            SliderType = PathType.Linear,
            Pos = new Vector2(0, 0),
            CurvePoints = [new Vector2(100, 0)],
            PixelLength = 1,
            Time = 100,
            EndTime = 100,
        });
        SliderPathGenerator pathGenerator = new();
        ((SliderPathGeneratorSettings)pathGenerator.Settings).PointDensity = 1;

        // Act
        var nearest = circle.NearestPoint(circle.Child.Centre);
        var tangentLines = new CircleTangentGenerator().GetRelevantObjects(
            new RelevantPoint(circle.Child.Centre), circle);
        var pathPoints = pathGenerator.GetRelevantObjects(hitObject)!;

        // Assert
        nearest.X.Should().NotBe(double.NaN);
        nearest.Y.Should().NotBe(double.NaN);
        tangentLines.Should().BeEmpty();
        pathPoints.Should().ContainSingle();
        pathPoints[0].Child.X.Should().NotBe(double.NaN);
        pathPoints[0].Child.Y.Should().NotBe(double.NaN);
    }
}
