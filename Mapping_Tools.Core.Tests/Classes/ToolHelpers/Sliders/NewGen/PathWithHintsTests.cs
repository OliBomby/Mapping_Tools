using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.ToolHelpers.Sliders.NewGen;

[TestClass]
public class PathWithHintsTests
{
    private const int num_points = 11;
    private PathWithHints path;
    private List<LinkedListNode<PathPoint>> points;

    [TestInitialize]
    public void Initialize()
    {
        points = new List<LinkedListNode<PathPoint>>();
        path = new PathWithHints();
        for (int i = 0; i < num_points; i++)
        {
            path.Path.AddLast(new PathPoint(new Vector2(i, 0), 0, 0, i));
            points.Add(path.Path.Last);
        }

        path.AddReconstructionHint(new ReconstructionHint(path.Path.First, path.Path.Last, -1, new List<Vector2>
        {
            new(0, 0),
            new(num_points - 1, 0),
        }, PathType.Linear));
    }

    [TestMethod]
    public void AddReconstructionHint_ZeroLengthHint_ThrowsArgumentException()
    {
        // Arrange
        // Act
        var act1 = () => path.AddReconstructionHint(new ReconstructionHint(points[2], points[2], 0, new List<Vector2>
        {
            new(2, 0),
            new(2, 1),
            new(2, 0),
        }));

        // Assert
        act1.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void AddReconstructionHint_OverlappingLayers_SplitsHintsCorrectly()
    {
        // Arrange
        // Act
        path.AddReconstructionHint(new ReconstructionHint(points[2], points[8], 0, new List<Vector2>
        {
            new(2, 0),
            new(5, 1),
            new(8, 0),
        }));

        // Assert
        path.ReconstructionHints.Count.Should().Be(3);
        path.ReconstructionHints[0].Start.Should().Be(points[0]);
        path.ReconstructionHints[0].End.Should().Be(points[2]);
        path.ReconstructionHints[0].StartP.Should().Be(0);
        path.ReconstructionHints[0].EndP.Should().Be(0.2);
        path.ReconstructionHints[0].Anchors.Should().NotBeNull();
        path.ReconstructionHints[1].Start.Should().Be(points[2]);
        path.ReconstructionHints[1].End.Should().Be(points[8]);
        path.ReconstructionHints[1].StartP.Should().Be(0);
        path.ReconstructionHints[1].EndP.Should().Be(1);
        path.ReconstructionHints[1].Anchors.Should().NotBeNull();
        path.ReconstructionHints[2].Start.Should().Be(points[8]);
        path.ReconstructionHints[2].End.Should().Be(points[10]);
        path.ReconstructionHints[2].StartP.Should().Be(0.8);
        path.ReconstructionHints[2].EndP.Should().Be(1);
        path.ReconstructionHints[2].Anchors.Should().NotBeNull();

        path.AddReconstructionHint(new ReconstructionHint(points[0], points[1], 0, new List<Vector2>
        {
            new(0, 0),
            new(0.5, 1),
            new(1, 0),
        }));

        path.ReconstructionHints.Count.Should().Be(4);
        path.ReconstructionHints[0].Start.Should().Be(points[0]);
        path.ReconstructionHints[0].End.Should().Be(points[1]);
        path.ReconstructionHints[0].StartP.Should().Be(0);
        path.ReconstructionHints[0].EndP.Should().Be(1);
        path.ReconstructionHints[0].Anchors.Should().NotBeNull();
        path.ReconstructionHints[1].Start.Should().Be(points[1]);
        path.ReconstructionHints[1].End.Should().Be(points[2]);
        path.ReconstructionHints[1].StartP.Should().Be(0.1);
        path.ReconstructionHints[1].EndP.Should().Be(0.2);
        path.ReconstructionHints[1].Anchors.Should().NotBeNull();
        path.ReconstructionHints[2].Start.Should().Be(points[2]);
        path.ReconstructionHints[2].End.Should().Be(points[8]);
        path.ReconstructionHints[2].StartP.Should().Be(0);
        path.ReconstructionHints[2].EndP.Should().Be(1);
        path.ReconstructionHints[2].Anchors.Should().NotBeNull();
        path.ReconstructionHints[3].Start.Should().Be(points[8]);
        path.ReconstructionHints[3].End.Should().Be(points[10]);
        path.ReconstructionHints[3].StartP.Should().Be(0.8);
        path.ReconstructionHints[3].EndP.Should().Be(1);
        path.ReconstructionHints[3].Anchors.Should().NotBeNull();

        path.AddReconstructionHint(new ReconstructionHint(points[9], points[10], 0, new List<Vector2>
        {
            new(9, 0),
            new(9.5, 1),
            new(10, 0),
        }));

        path.ReconstructionHints.Count.Should().Be(5);
        path.ReconstructionHints[0].Start.Should().Be(points[0]);
        path.ReconstructionHints[0].End.Should().Be(points[1]);
        path.ReconstructionHints[0].StartP.Should().Be(0);
        path.ReconstructionHints[0].EndP.Should().Be(1);
        path.ReconstructionHints[0].Anchors.Should().NotBeNull();
        path.ReconstructionHints[1].Start.Should().Be(points[1]);
        path.ReconstructionHints[1].End.Should().Be(points[2]);
        path.ReconstructionHints[1].StartP.Should().Be(0.1);
        path.ReconstructionHints[1].EndP.Should().Be(0.2);
        path.ReconstructionHints[1].Anchors.Should().NotBeNull();
        path.ReconstructionHints[2].Start.Should().Be(points[2]);
        path.ReconstructionHints[2].End.Should().Be(points[8]);
        path.ReconstructionHints[2].StartP.Should().Be(0);
        path.ReconstructionHints[2].EndP.Should().Be(1);
        path.ReconstructionHints[2].Anchors.Should().NotBeNull();
        path.ReconstructionHints[3].Start.Should().Be(points[8]);
        path.ReconstructionHints[3].End.Should().Be(points[9]);
        path.ReconstructionHints[3].StartP.Should().Be(0.8);
        path.ReconstructionHints[3].EndP.Should().Be(0.9);
        path.ReconstructionHints[3].Anchors.Should().NotBeNull();
        path.ReconstructionHints[4].Start.Should().Be(points[9]);
        path.ReconstructionHints[4].End.Should().Be(points[10]);
        path.ReconstructionHints[4].StartP.Should().Be(0);
        path.ReconstructionHints[4].EndP.Should().Be(1);
        path.ReconstructionHints[4].Anchors.Should().NotBeNull();

        path.AddReconstructionHint(new ReconstructionHint(points[1], points[2], 0, null));

        path.ReconstructionHints.Count.Should().Be(5);
        path.ReconstructionHints[0].Start.Should().Be(points[0]);
        path.ReconstructionHints[0].End.Should().Be(points[1]);
        path.ReconstructionHints[0].StartP.Should().Be(0);
        path.ReconstructionHints[0].EndP.Should().Be(1);
        path.ReconstructionHints[0].Anchors.Should().NotBeNull();
        path.ReconstructionHints[1].Start.Should().Be(points[1]);
        path.ReconstructionHints[1].End.Should().Be(points[2]);
        path.ReconstructionHints[1].StartP.Should().Be(0);
        path.ReconstructionHints[1].EndP.Should().Be(1);
        path.ReconstructionHints[1].Anchors.Should().BeNull();
        path.ReconstructionHints[2].Start.Should().Be(points[2]);
        path.ReconstructionHints[2].End.Should().Be(points[8]);
        path.ReconstructionHints[2].StartP.Should().Be(0);
        path.ReconstructionHints[2].EndP.Should().Be(1);
        path.ReconstructionHints[2].Anchors.Should().NotBeNull();
        path.ReconstructionHints[3].Start.Should().Be(points[8]);
        path.ReconstructionHints[3].End.Should().Be(points[9]);
        path.ReconstructionHints[3].StartP.Should().Be(0.8);
        path.ReconstructionHints[3].EndP.Should().Be(0.9);
        path.ReconstructionHints[3].Anchors.Should().NotBeNull();
        path.ReconstructionHints[4].Start.Should().Be(points[9]);
        path.ReconstructionHints[4].End.Should().Be(points[10]);
        path.ReconstructionHints[4].StartP.Should().Be(0);
        path.ReconstructionHints[4].EndP.Should().Be(1);
        path.ReconstructionHints[4].Anchors.Should().NotBeNull();
    }

    [TestMethod]
    public void AddReconstructionHint_LeftSameLayerOverlap_SplitsHintsCorrectly()
    {
        // Arrange
        path.AddReconstructionHint(new ReconstructionHint(points[2], points[8], 0, new List<Vector2>
        {
            new(2, 0),
            new(5, 1),
            new(8, 0),
        }));

        // Act
        path.AddReconstructionHint(new ReconstructionHint(points[1], points[3], 0, new List<Vector2>
        {
            new(1, 0),
            new(2, 1),
            new(3, 0),
        }));

        // Assert
        path.ReconstructionHints.Count.Should().Be(5);
        path.ReconstructionHints[0].Start.Should().Be(points[0]);
        path.ReconstructionHints[0].End.Should().Be(points[1]);
        path.ReconstructionHints[0].StartP.Should().Be(0);
        path.ReconstructionHints[0].EndP.Should().Be(0.1);
        path.ReconstructionHints[0].Anchors.Should().NotBeNull();
        path.ReconstructionHints[1].Start.Should().Be(points[1]);
        path.ReconstructionHints[1].End.Should().Be(points[2]);
        path.ReconstructionHints[1].StartP.Should().Be(0);
        path.ReconstructionHints[1].EndP.Should().Be(0.5);
        path.ReconstructionHints[1].Anchors.Should().NotBeNull();
        path.ReconstructionHints[2].Start.Should().Be(points[2]);
        path.ReconstructionHints[2].End.Should().Be(points[3]);
        path.ReconstructionHints[2].StartP.Should().Be(0);
        path.ReconstructionHints[2].EndP.Should().Be(1);
        path.ReconstructionHints[2].Anchors.Should().BeNull();
        path.ReconstructionHints[3].Start.Should().Be(points[3]);
        path.ReconstructionHints[3].End.Should().Be(points[8]);
        path.ReconstructionHints[3].StartP.Should().BeApproximately(1 / 6d, Precision.DOUBLE_EPSILON);
        path.ReconstructionHints[3].EndP.Should().Be(1);
        path.ReconstructionHints[3].Anchors.Should().NotBeNull();
        path.ReconstructionHints[4].Start.Should().Be(points[8]);
        path.ReconstructionHints[4].End.Should().Be(points[10]);
        path.ReconstructionHints[4].StartP.Should().Be(0.8);
        path.ReconstructionHints[4].EndP.Should().Be(1);
        path.ReconstructionHints[4].Anchors.Should().NotBeNull();
    }

    [TestMethod]
    public void AddReconstructionHint_RightSameLayerOverlap_SplitsHintsCorrectly()
    {
        // Arrange
        path.AddReconstructionHint(new ReconstructionHint(points[2], points[8], 0, new List<Vector2>
        {
            new(2, 0),
            new(5, 1),
            new(8, 0),
        }));

        // Act
        path.AddReconstructionHint(new ReconstructionHint(points[7], points[9], 0, new List<Vector2>
        {
            new(7, 0),
            new(8, 1),
            new(9, 0),
        }));

        // Assert
        path.ReconstructionHints.Count.Should().Be(5);
        path.ReconstructionHints[0].Start.Should().Be(points[0]);
        path.ReconstructionHints[0].End.Should().Be(points[2]);
        path.ReconstructionHints[0].StartP.Should().Be(0);
        path.ReconstructionHints[0].EndP.Should().Be(0.2);
        path.ReconstructionHints[0].Anchors.Should().NotBeNull();
        path.ReconstructionHints[1].Start.Should().Be(points[2]);
        path.ReconstructionHints[1].End.Should().Be(points[7]);
        path.ReconstructionHints[1].StartP.Should().Be(0);
        path.ReconstructionHints[1].EndP.Should().BeApproximately(1 - 1 / 6d, Precision.DOUBLE_EPSILON);
        path.ReconstructionHints[1].Anchors.Should().NotBeNull();
        path.ReconstructionHints[2].Start.Should().Be(points[7]);
        path.ReconstructionHints[2].End.Should().Be(points[8]);
        path.ReconstructionHints[2].StartP.Should().Be(0);
        path.ReconstructionHints[2].EndP.Should().Be(1);
        path.ReconstructionHints[2].Anchors.Should().BeNull();
        path.ReconstructionHints[3].Start.Should().Be(points[8]);
        path.ReconstructionHints[3].End.Should().Be(points[9]);
        path.ReconstructionHints[3].StartP.Should().Be(0.5);
        path.ReconstructionHints[3].EndP.Should().Be(1);
        path.ReconstructionHints[3].Anchors.Should().NotBeNull();
        path.ReconstructionHints[4].Start.Should().Be(points[9]);
        path.ReconstructionHints[4].End.Should().Be(points[10]);
        path.ReconstructionHints[4].StartP.Should().Be(0.9);
        path.ReconstructionHints[4].EndP.Should().Be(1);
        path.ReconstructionHints[4].Anchors.Should().NotBeNull();
    }

    [TestMethod]
    public void AddReconstructionHint_MiddleSameLayerOverlap_SplitsHintsCorrectly()
    {
        // Arrange
        path.AddReconstructionHint(new ReconstructionHint(points[2], points[8], 0, new List<Vector2>
        {
            new(2, 0),
            new(5, 1),
            new(8, 0),
        }));

        // Act
        path.AddReconstructionHint(new ReconstructionHint(points[3], points[7], 0, new List<Vector2>
        {
            new(3, 0),
            new(5, 1),
            new(7, 0),
        }));

        // Assert
        path.ReconstructionHints.Count.Should().Be(5);
        path.ReconstructionHints[0].Start.Should().Be(points[0]);
        path.ReconstructionHints[0].End.Should().Be(points[2]);
        path.ReconstructionHints[0].StartP.Should().Be(0);
        path.ReconstructionHints[0].EndP.Should().Be(0.2);
        path.ReconstructionHints[0].Anchors.Should().NotBeNull();
        path.ReconstructionHints[1].Start.Should().Be(points[2]);
        path.ReconstructionHints[1].End.Should().Be(points[3]);
        path.ReconstructionHints[1].StartP.Should().Be(0);
        path.ReconstructionHints[1].EndP.Should().BeApproximately(1 / 6d, Precision.DOUBLE_EPSILON);
        path.ReconstructionHints[1].Anchors.Should().NotBeNull();
        path.ReconstructionHints[2].Start.Should().Be(points[3]);
        path.ReconstructionHints[2].End.Should().Be(points[7]);
        path.ReconstructionHints[2].StartP.Should().Be(0);
        path.ReconstructionHints[2].EndP.Should().Be(1);
        path.ReconstructionHints[2].Anchors.Should().BeNull();
        path.ReconstructionHints[3].Start.Should().Be(points[7]);
        path.ReconstructionHints[3].End.Should().Be(points[8]);
        path.ReconstructionHints[3].StartP.Should().BeApproximately(1 - 1 / 6d, Precision.DOUBLE_EPSILON);
        path.ReconstructionHints[3].EndP.Should().Be(1);
        path.ReconstructionHints[3].Anchors.Should().NotBeNull();
        path.ReconstructionHints[4].Start.Should().Be(points[8]);
        path.ReconstructionHints[4].End.Should().Be(points[10]);
        path.ReconstructionHints[4].StartP.Should().Be(0.8);
        path.ReconstructionHints[4].EndP.Should().Be(1);
        path.ReconstructionHints[4].Anchors.Should().NotBeNull();
    }
}
