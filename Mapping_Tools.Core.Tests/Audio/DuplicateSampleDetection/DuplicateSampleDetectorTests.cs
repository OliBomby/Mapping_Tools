using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Core.Audio.DuplicateDetection;
using Mapping_Tools.Core.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Audio.DuplicateSampleDetection;

[TestClass]
public class DuplicateSampleDetectorTests {
    private List<IBeatmapSetFileInfo> files;

    [TestInitialize]
    public void Init() {
        var path = Path.Combine("Resources", "TestMapset1");
        var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        files = allFiles
            .Select(p => Path.GetRelativePath(path, p))
            .Select(p => (IBeatmapSetFileInfo) new BeatmapSetFileInfo(path, p))
            .ToList();
    }

    [TestMethod]
    [Timeout(5000)]
    public void MonolithicDuplicateSampleDetectorTest() {
        var map = new MonolithicDuplicateSampleDetector().AnalyzeSamples(files, out var exception);

        Assert.IsNull(exception);

        Assert.IsTrue(map.IsDuplicate("soft-hitclap2.wav", "soft-hitclap3.wav"));
        Assert.IsFalse(map.IsDuplicate("soft-hitclap2.wav", "soft-hitwhistle3.wav"));
    }
}