using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.HitsoundCopier;

[TestClass]
public sealed class HitsoundCopierServiceTests
{
    [TestMethod]
    public async Task CopyAsync_WithMultipleTargets_UsesSourceAndSavesEveryTarget()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-feature-rich.osu");
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
        HitsoundCopierService service = new(
            gateway,
            new StubSampleService(),
            new TestDirectories(),
            new RecordingFileRevealService(),
            new ApplicationSettings());
        HitsoundCopierServiceOptions options = new()
        {
            PathFrom = "source.osu",
            PathTo = "first.osu|second.osu",
        };

        // Act
        var result = await service.CopyAsync(options);

        // Assert
        result.ProcessedPaths.Should().Equal("first.osu", "second.osu");
        gateway.OpenRequests.Select(request => request.Path)
            .Should().Equal("source.osu", "first.osu", "second.osu");
        gateway.SessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().Equal("first.osu", "second.osu");
    }

    [TestMethod]
    public async Task CopyAsync_WhenSampleExporterReportsExports_RevealsExportDirectory()
    {
        // Arrange
        TimingPoint sourceTiming = new(0, 1000, 4, SampleSet.Normal, 0, 100, true, false, false);
        HitObject sourceObject = new(500, 1, SampleSet.Normal, SampleSet.None)
        {
            CustomIndex = 1,
        };
        Beatmap source = new([sourceObject], [sourceTiming], sourceTiming, 1.4);
        TimingPoint targetTiming = sourceTiming.Copy();
        Beatmap target = new(
            [new HitObject("256,192,0,2,0,L|396:192,1,140,0|0:0,0:0:0:0:")],
            [targetTiming],
            targetTiming,
            1.4);
        RecordingBeatmapEditingGateway gateway = CreateGateway(source, target);
        RecordingSampleService samples = new();
        RecordingFileRevealService reveal = new();
        HitsoundCopierService service = new(
            gateway,
            samples,
            new TestDirectories(),
            reveal,
            new ApplicationSettings());
        HitsoundCopierServiceOptions options = new()
        {
            PathFrom = "source.osu",
            PathTo = "target.osu",
            CopyMode = HitsoundCopierCopyMode.OverwriteOnlyDefined,
            CopyToSliderSlides = true,
            CopyBodyHitsounds = false,
        };

        // Act
        await service.CopyAsync(options);

        // Assert
        samples.ExportCalls.Should().Be(1);
        reveal.Paths.Should().ContainSingle().Which.Should().Be(new TestDirectories().Exports);
    }

    [TestMethod]
    public async Task CopyAsync_WhenSampleExporterReportsNoExports_DoesNotRevealExportDirectory()
    {
        // Arrange
        TimingPoint sourceTiming = new(0, 1000, 4, SampleSet.Normal, 0, 100, true, false, false);
        HitObject sourceObject = new(500, 1, SampleSet.Normal, SampleSet.None)
        {
            CustomIndex = 1,
        };
        Beatmap source = new([sourceObject], [sourceTiming], sourceTiming, 1.4);
        TimingPoint targetTiming = sourceTiming.Copy();
        Beatmap target = new(
            [new HitObject("256,192,0,2,0,L|396:192,1,140,0|0:0,0:0:0:0:")],
            [targetTiming],
            targetTiming,
            1.4);
        RecordingBeatmapEditingGateway gateway = CreateGateway(source, target);
        RecordingSampleService samples = new() { ExportedCount = 0 };
        RecordingFileRevealService reveal = new();
        HitsoundCopierService service = new(
            gateway,
            samples,
            new TestDirectories(),
            reveal,
            new ApplicationSettings());
        HitsoundCopierServiceOptions options = new()
        {
            PathFrom = "source.osu",
            PathTo = "target.osu",
            CopyMode = HitsoundCopierCopyMode.OverwriteOnlyDefined,
            CopyToSliderSlides = true,
            CopyBodyHitsounds = false,
        };

        // Act
        await service.CopyAsync(options);

        // Assert
        samples.ExportCalls.Should().Be(1);
        reveal.Paths.Should().BeEmpty();
    }

    private sealed class StubSampleService : IHitsoundSampleService
    {
        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
            string directory,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        public HitsoundSampleAssignment? TryCreateAssignment(
            string directory,
            IReadOnlyList<string> sourceFilenames,
            IReadOnlyDictionary<string, string> firstSamples,
            string role,
            SampleSet sampleSet,
            int startIndex,
            SampleSchema existingSchema)
        {
            return null;
        }

        public Task<int> ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class RecordingSampleService : IHitsoundSampleService
    {
        public int ExportedCount { get; set; } = 1;

        public int ExportCalls { get; private set; }

        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
            string directory,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        public HitsoundSampleAssignment? TryCreateAssignment(
            string directory,
            IReadOnlyList<string> sourceFilenames,
            IReadOnlyDictionary<string, string> firstSamples,
            string role,
            SampleSet sampleSet,
            int startIndex,
            SampleSchema existingSchema)
        {
            SampleSchema schema = new();
            schema.Add("normal-sliderslide100", []);
            return new HitsoundSampleAssignment(100, SampleSet.Normal, schema);
        }

        public Task<int> ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default)
        {
            ExportCalls++;
            return Task.FromResult(ExportedCount);
        }
    }

    private sealed class RecordingFileRevealService : IFileRevealService
    {
        public List<string> Paths { get; } = [];

        public Task<bool> RevealAsync(string path, CancellationToken cancellationToken = default)
        {
            Paths.Add(path);
            return Task.FromResult(true);
        }
    }

    private sealed class TestDirectories : IApplicationDirectories
    {
        public string LocalApplicationData => @"C:\MappingToolsTests";

        public string ApplicationData => @"C:\MappingToolsTests\Mapping Tools";

        public string Exports => @"C:\MappingToolsTests\Mapping Tools\Exports";

        public string ConfigurationFile => Path.Combine(ApplicationData, "config.json");

        public string PreferencesFile => Path.Combine(ApplicationData, "preferences.json");

        public void EnsureCreated()
        {
        }
    }

    private static RecordingBeatmapEditingGateway CreateGateway(Beatmap source, Beatmap target)
    {
        return new RecordingBeatmapEditingGateway
        {
            OpenBeatmapFactory = (path, _) =>
            {
                Beatmap beatmap = path == "source.osu" ? source : target;
                BeatmapEditor editor = new(
                    beatmap.GetLines(),
                    new NoOpTextFileStore { ReadResult = [] })
                {
                    Path = path,
                };
                return new BeatmapEditingSession(editor, BeatmapEditingSource.Disk, []);
            },
        };
    }

    private static RecordingBeatmapEditingGateway CreateGateway(string fixture)
    {
        return new RecordingBeatmapEditingGateway
        {
            OpenBeatmapFactory = (path, _) =>
            {
                BeatmapEditor editor = new(
                    File.ReadAllLines(fixture).ToList(),
                    new NoOpTextFileStore { ReadResult = [] })
                {
                    Path = path,
                };
                return new BeatmapEditingSession(editor, BeatmapEditingSource.Disk, []);
            },
        };
    }

}
