using Mapping_Tools.ApplicationServices.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class FilePickerFilterTests
{
    [TestMethod]
    public void ConstructorCleansAndDeduplicatesValues()
    {
        FilePickerFilter filter = new(
            "Beatmaps",
            ["*.osu", "", "*.OSU", "  *.osb  "],
            ["application/x-osu-beatmap", " "],
            ["public.data"]);

        CollectionAssert.AreEqual(new[] { "*.osu", "*.osb" }, filter.Patterns.ToArray());
        CollectionAssert.AreEqual(
            new[] { "application/x-osu-beatmap" },
            filter.MimeTypes.ToArray());
        CollectionAssert.AreEqual(
            new[] { "public.data" },
            filter.AppleUniformTypeIdentifiers.ToArray());
    }

    [TestMethod]
    public void ConstructorRejectsEmptyPatterns()
    {
        Assert.ThrowsException<ArgumentException>(
            () => new FilePickerFilter("Beatmaps", ["", " "]));
    }
}
