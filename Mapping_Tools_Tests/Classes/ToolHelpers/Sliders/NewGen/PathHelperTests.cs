using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;
using NUnit.Framework;

namespace Mapping_Tools_Tests.Classes.ToolHelpers.Sliders.NewGen;

[TestFixture]
public class PathHelperTests {
    [Test]
    public void CreatePathWithHintsTest() {
        var slider =
            new HitObject("42,179,300,2,0,B|135:234|219:171|219:171|194:100|194:100|266:53|345:48|405:117,1,500");

        var sliderPath = slider.GetSliderPath();

        var result = PathHelper.CreatePathWithHints(sliderPath);

        int i = 0;
        foreach (PathPoint pathPoint in result.Path) {
            Console.WriteLine(++i + " : " + pathPoint);
            if (pathPoint.Pos == new Vector2(219, 171) ||
                pathPoint.Pos == new Vector2(194, 100)) {
                Assert.That(pathPoint.Red, Is.True, $"point {i} should be a red anchor");
            } else {
                Assert.That(pathPoint.Red, Is.False, $"point {i} should not be a red anchor");
            }
        }

        i = 0;
        foreach (var hint in result.ReconstructionHints) {
            Console.WriteLine(++i + " : " + hint.Layer);
            foreach (Vector2 anchor in hint.Anchors) {
                Console.WriteLine(anchor);
            }
        }

        Assert.That(result.Path.Count(o => o.Red), Is.EqualTo(2));
    }

    [Test]
    public void CreatePathWithHintsMultiRedTest() {
        var slider =
            new HitObject("42,179,300,2,0,B|42:179|42:179|42:179|42:179|135:234|219:171|219:171|219:171|219:171|194:100|194:100|194:100|194:100|194:100|194:100|266:53|345:48|405:117|405:117|405:117|405:117|405:117|405:117|405:117,1,450");

        var sliderPath = slider.GetSliderPath();

        var result = PathHelper.CreatePathWithHints(sliderPath);

        int i = 0;
        foreach (PathPoint pathPoint in result.Path) {
            Console.WriteLine(++i + " : " + pathPoint);
            if (pathPoint.Pos == new Vector2(219, 171) ||
                pathPoint.Pos == new Vector2(194, 100)) {
                Assert.That(pathPoint.Red, Is.True, $"point {i} should be a red anchor");
            } else {
                Assert.That(pathPoint.Red, Is.False, $"point {i} should not be a red anchor");
            }
        }

        i = 0;
        foreach (var hint in result.ReconstructionHints) {
            Console.WriteLine(++i + " : " + hint.Layer);
            foreach (Vector2 anchor in hint.Anchors) {
                Console.WriteLine(anchor);
            }
            Assert.That(hint.Anchors.Count > 1, Is.True, $"hint {i} does not have enough anchors");
        }

        Assert.That(result.Path.Count(o => o.Red), Is.EqualTo(2));
    }

    [Test]
    public void InterpolateTest() {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var path = new LinkedList<PathPoint>(new[] {
            new PathPoint(new Vector2(-9, 0)),
            new PathPoint(new Vector2(1, 0)),
            new PathPoint(new Vector2(2, 1)),
            new PathPoint(new Vector2(12, 1))
        });
        PathHelper.Recalculate(path);

        var p1 = path.First!.Next;
        PathHelper.Interpolate(p1, Enumerable.Range(1, 9).Select(i => i / 10d));

        foreach (var p in path) {
            Debug.WriteLine(p.Pos.ToInvariant());
        }

        var path2 = new LinkedList<PathPoint>(new[] {
            new PathPoint(new Vector2(-9, 0)),
            new PathPoint(new Vector2(1, 0)),
            new PathPoint(new Vector2(2, 1), red: true),
            new PathPoint(new Vector2(12, 1))
        });
        PathHelper.Recalculate(path2);

        var p2 = path2.First!.Next;
        PathHelper.Interpolate(p2, Enumerable.Range(1, 9).Select(i => i / 10d));

        foreach (var p in path2) {
            Debug.WriteLine(p.Pos.ToInvariant());
        }
    }

    [Test]
    public void SubdivideTest() {
        var path = new LinkedList<PathPoint>(new[] {
            new PathPoint(new Vector2(-9, 0)),
            new PathPoint(new Vector2(1, 0)),
            new PathPoint(new Vector2(2, 1)),
            new PathPoint(new Vector2(12, 1))
        });
        PathHelper.Recalculate(path);

        var start = path.First!.Next;
        var middle = start!.Next;
        var end = path.Last;
        var added = path.Subdivide(start, end, 5);

        Assert.That(added, Is.EqualTo(4));

        Assert.That(start!.Next!.Value > start.Value, Is.True);
        Assert.That(start.Next, Is.SameAs(middle));
        Assert.That(start.Next.Next!.Value > start.Next.Value, Is.True);
        Assert.That(start.Next.Next.Next!.Value > start.Next.Next.Value, Is.True);
        Assert.That(start.Next.Next.Next.Next!.Value > start.Next.Next.Next.Value, Is.True);
        Assert.That(start.Next.Next.Next.Next.Next!.Value > start.Next.Next.Next.Next.Value, Is.True);
        Assert.That(start.Next.Next.Next.Next.Next.Next!.Value > start.Next.Next.Next.Next.Next.Value, Is.True);
        Assert.That(start.Next.Next.Next.Next.Next.Next, Is.SameAs(end));
    }
}