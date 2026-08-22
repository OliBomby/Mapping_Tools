using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Audio;
using Mapping_Tools.Application.AutoFail;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.ComboColourStudio;
using Mapping_Tools.Application.HitsoundCopier;
using Mapping_Tools.Application.HitsoundPreviewHelper;
using Mapping_Tools.Application.HitsoundStudio;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.MapsetMerger;
using Mapping_Tools.Application.MetadataManager;
using Mapping_Tools.Application.PatternGallery;
using Mapping_Tools.Application.PropertyTransformer;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.RhythmGuide;
using Mapping_Tools.Application.SliderCompletionator;
using Mapping_Tools.Application.SliderMerger;
using Mapping_Tools.Application.SliderPicturator;
using Mapping_Tools.Application.Sliderator;
using Mapping_Tools.Application.TimingCopier;
using Mapping_Tools.Application.TimingHelper;
using Mapping_Tools.Application.TumourGenerator;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.AutoFail;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Mapping_Tools.Core.Tools.HitsoundCopier;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Core.Tools.HitsoundStudio;
using Mapping_Tools.Core.Tools.MapCleaner;
using Mapping_Tools.Core.Tools.MetadataManager;
using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Core.Tools.PropertyTransformer;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.SliderCompletionator;
using Mapping_Tools.Core.Tools.SliderMerger;
using Mapping_Tools.Core.Tools.SliderPicturator;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Core.Tools.TimingCopier;
using Mapping_Tools.Core.Tools.TimingHelper;
using Mapping_Tools.Infrastructure.Audio;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Images;
using Mapping_Tools.Infrastructure.MapsetMerger;
using Mapping_Tools.Infrastructure.PatternGallery;
using Mapping_Tools.Infrastructure.Projects;
using Mapping_Tools.Infrastructure.Workspace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mapping_Tools.Application.Tests.TestDoubles;
using Newtonsoft.Json.Linq;
using TextJsonSerializer = System.Text.Json.JsonSerializer;

namespace Mapping_Tools.Application.Tests.Execution;

[TestClass]
public sealed class TransformationFixtureTests
{
    private static readonly IProjectSerializer ProjectJson = new LegacyProjectJsonSerializer();

    [DataTestMethod]
    [DataRow("auto-fail-detector", "Auto-fail Detector")]
    [DataRow("combo-colour-studio", "Combo Colour Studio")]
    [DataRow("hitsound-copier", "Hitsound Copier")]
    [DataRow("hitsound-preview", "Hitsound Preview Helper")]
    [DataRow("hitsound-studio", "Hitsound Studio")]
    [DataRow("map-cleaner", "Map Cleaner")]
    [DataRow("mapset-merger", "Mapset Merger")]
    [DataRow("metadata-manager", "Metadata Manager")]
    [DataRow("pattern-gallery", "Pattern Gallery")]
    [DataRow("property-transformer", "Property Transformer")]
    [DataRow("rhythm-guide", "Rhythm Guide")]
    [DataRow("slider-completionator", "Slider Completionator")]
    [DataRow("slider-merger", "Slider Merger")]
    [DataRow("slider-picturator", "Slider Picturator")]
    [DataRow("sliderator", "Sliderator")]
    [DataRow("timing-copier", "Timing Copier")]
    [DataRow("timing-helper", "Timing Helper")]
    [DataRow("tumour-generator", "Tumour Generator 2")]
    public async Task ExecuteFixture_ProducesEquivalentToolOutput(string fixtureName, string feature)
    {
        // Arrange
        using FixtureWorkspace workspace = new(Path.Combine(AppContext.BaseDirectory, "Fixtures"));
        string fixtureRoot = Path.Combine(workspace.Root, "Transformations");
        string recordPath = Path.Combine(fixtureRoot, $"{fixtureName}.json");
        string optionsPath = Path.Combine(fixtureRoot, $"{fixtureName}.options.json");
        using JsonDocument record = JsonDocument.Parse(File.ReadAllText(recordPath));
        using JsonDocument options = JsonDocument.Parse(File.ReadAllText(optionsPath));
        JsonElement recordRoot = record.RootElement;
        JsonElement optionsRoot = options.RootElement;
        string expectedOutputPath = ResolveFixturePath(fixtureRoot, StringProperty(recordRoot, "expectedOutput"));
        string seedInput = ResolveFixturePath(fixtureRoot, StringProperty(recordRoot, "seedInput"));
        string? secondaryInput = OptionalStringProperty(recordRoot, "secondaryInput") is { } secondary
            ? ResolveFixturePath(fixtureRoot, secondary)
            : null;
        optionsRoot.ValueKind.Should().Be(JsonValueKind.Object);
        File.ReadAllBytes(seedInput).Should().NotBeEmpty();
        if (secondaryInput is not null)
        {
            File.ReadAllBytes(secondaryInput).Should().NotBeEmpty();
        }
        File.ReadAllText(Path.Combine(fixtureRoot, $"{fixtureName}-report.md")).Should().NotBeNullOrWhiteSpace();
        File.ReadAllText(expectedOutputPath).Should().NotBeNullOrWhiteSpace();
        FileBackedEditingGateway gateway = new();

        // Act
        FixtureExecutionResult actual = await ExecuteFixtureAsync(
            fixtureName, fixtureRoot, seedInput, optionsRoot, gateway, CancellationToken.None);

        // Assert
        recordRoot.GetProperty("feature").GetString().Should().Be(feature);
        recordRoot.GetProperty("status").GetString().Should().Be("accepted");
        actual.WasExecuted.Should().BeTrue();
        if (fixtureName == "auto-fail-detector")
        {
            JsonNode.DeepEquals(
                    JsonNode.Parse(File.ReadAllText(expectedOutputPath)),
                    JsonNode.Parse(actual.JsonOutput!))
                .Should().BeTrue();
        }
        else if (fixtureName == "mapset-merger")
        {
            AssertMapsetOutputEquivalent(fixtureRoot, expectedOutputPath, actual.OutputDirectory!);
        }
        else
        {
            actual.OutputPaths.Should().ContainSingle();
            AssertTextOutputEquivalent(expectedOutputPath, actual.OutputPaths.Single());
        }
    }

    private static async Task<FixtureExecutionResult> ExecuteFixtureAsync(
        string fixtureName,
        string fixtureRoot,
        string seedInput,
        JsonElement options,
        FileBackedEditingGateway gateway,
        CancellationToken cancellationToken)
    {
        string target = OptionalPath(options, "Target", fixtureRoot) ??
            OptionalPath(options, "BaseBeatmap", fixtureRoot) ??
            OptionalPath(options, "ExportPath", fixtureRoot) ??
            seedInput;
        switch (fixtureName)
        {
            case "auto-fail-detector":
            {
                AutoFailOptions autoFailOptions = new(
                    target,
                    NumberProperty(options, "ApproachRateOverride"),
                    NumberProperty(options, "OverallDifficultyOverride"),
                    IntProperty(options, "PhysicsUpdateLeniency"));
                AutoFailService service = new(gateway);
                AutoFailRun positive = await service.AnalyzeAsync(autoFailOptions, cancellationToken);
                AutoFailRun negative = await service.AnalyzeAsync(
                    autoFailOptions with { Path = RequiredPath(options, "NegativeControl", fixtureRoot) },
                    cancellationToken);
                return new FixtureExecutionResult(
                    JsonOutput: TextJsonSerializer.Serialize(new
                    {
                        autoFailDetected = positive.Analysis.HasAutoFail,
                        unloadingObjects = positive.Analysis.UnloadingObjects.Count,
                        potentialUnloadingObjects = positive.Analysis.PotentialUnloadingObjects.Count,
                        message = AutoFailMessage(positive.Analysis),
                        negativeControlMessage = AutoFailMessage(negative.Analysis)
                    }));
            }
            case "combo-colour-studio":
                await new ComboColourStudioService(gateway).ApplyAsync(
                    [target],
                    ReadTransformationProject<ComboColourProject>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "hitsound-copier":
                await new HitsoundCopierService(
                        gateway,
                        new EmptyHitsoundSampleService(),
                        new ApplicationSettings { AutoReload = false })
                    .CopyAsync(
                        ReadTransformationProject<HitsoundCopierProject>(fixtureRoot, fixtureName),
                        cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "hitsound-preview":
                await new HitsoundPreviewHelperService(gateway).ApplyAsync(
                    [target],
                    ReadTransformationProject<HitsoundPreviewHelperProject>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "hitsound-studio":
            {
                HitsoundStudioProject project = ReadTransformationProject<HitsoundStudioProject>(
                    fixtureRoot,
                    fixtureName);
                project.ExportFolder = Path.Combine(Path.GetDirectoryName(target)!, "hitsound-studio-export");
                HitsoundStudioExportResult result = await CreateHitsoundStudioService(gateway)
                    .ExportAsync(project, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([result.MapPath!]);
            }
            case "map-cleaner":
            {
                MapCleanerProject project = ReadTransformationProject<MapCleanerProject>(fixtureRoot, fixtureName);
                await new MapCleanerService(
                        gateway, new PhysicalBeatmapFileSystem(), new EmptyMapCleanerSampleService())
                    .CleanAsync([target], project.MapCleanerArgs, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            }
            case "mapset-merger":
            {
                MapsetMergerProject project = ReadTransformationProject<MapsetMergerProject>(
                    fixtureRoot,
                    fixtureName);
                StageMapsetMergerSources(options, fixtureRoot, project);
                await new MapsetMergerService(
                        gateway, new PhysicalMapsetFileSystem(), new FileSystemFileStore())
                    .MergeAsync(
                        project,
                        cancellationToken: cancellationToken);
                return new FixtureExecutionResult(OutputDirectory: project.ExportPath);
            }
            case "metadata-manager":
            {
                MetadataManagerResult result = await new MetadataManagerService(
                        gateway, new TestBeatmapBackupService())
                    .ExportAsync(
                        ReadTransformationProject<MetadataManagerProject>(fixtureRoot, fixtureName),
                        cancellationToken: cancellationToken);
                return new FixtureExecutionResult(result.ProcessedPaths);
            }
            case "pattern-gallery":
            {
                PatternGalleryProject project = ReadTransformationProject<PatternGalleryProject>(
                    fixtureRoot,
                    fixtureName);
                PatternGalleryCollectionPaths paths = ReadPatternGalleryPaths(options, fixtureRoot);
                PatternGalleryPattern pattern = project.Patterns.Single(item =>
                    item.Name.Equals(StringProperty(options, "Pattern"), StringComparison.Ordinal));
                await new PatternGalleryService(gateway, new PatternGalleryFileService())
                    .ExportAsync(target, [pattern], project, paths, quick: false, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            }
            case "property-transformer":
                await new PropertyTransformerService(gateway).TransformAsync(
                    [target],
                    ReadTransformationProject<PropertyTransformerProject>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "rhythm-guide":
            {
                RhythmGuideOptions rhythmOptions = ReadTransformationProject<RhythmGuideProject>(
                    fixtureRoot,
                    fixtureName).GuideGeneratorArgs;
                await new RhythmGuideService(
                        gateway, new TestBeatmapBackupService(),
                        new PhysicalBeatmapFileSystem(), new FileSystemFileStore())
                    .GenerateAsync(rhythmOptions, cancellationToken);
                return new FixtureExecutionResult([rhythmOptions.ExportPath]);
            }
            case "slider-completionator":
                await new SliderCompletionatorService(gateway).CompleteAsync(
                    [target],
                    ReadTransformationProject<SliderCompletionatorProject>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "slider-merger":
                await new SliderMergerService(gateway).MergeAsync(
                    [target],
                    ReadTransformationProject<SliderMergerProject>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "slider-picturator":
                await new SliderPicturatorService(gateway, new SystemDrawingImageFileService())
                    .PicturateAsync(
                        target,
                        ReadTransformationProject<SliderPicturatorProject>(fixtureRoot, fixtureName),
                        cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "sliderator":
            {
                SlideratorProject project = ReadTransformationProject<SlideratorProject>(fixtureRoot, fixtureName);
                HitObject sourceSlider = ReadLegacySlider(fixtureRoot, fixtureName);
                ApplySlideratorTransientState(project, sourceSlider);
                await new SlideratorService(gateway).RunAsync(
                    target, project, sourceSlider, reloadEditor: false, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            }
            case "timing-copier":
                await new TimingCopierService(gateway).CopyAsync(
                    ReadTransformationProject<TimingCopierProject>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "timing-helper":
                await new TimingHelperService(gateway).AdjustAsync(
                    [target],
                    ReadTransformationProject<TimingHelperProject>(fixtureRoot, fixtureName),
                    cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            case "tumour-generator":
            {
                TumourGeneratorProject project = ReadTransformationProject<TumourGeneratorProject>(
                    fixtureRoot,
                    fixtureName);
                project.TumourLayers = project.TumourLayers
                    .Take(IntProperty(options, "LayerCount"))
                    .ToList();
                await new TumourGeneratorService(gateway).RunAsync(
                    [target],
                    project,
                    reloadEditor: false, cancellationToken: cancellationToken);
                return new FixtureExecutionResult([target]);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(fixtureName), fixtureName, "Unknown transformation fixture.");
        }
    }

    private static string AutoFailMessage(AutoFailAnalysis analysis) =>
        analysis.HasAutoFail
            ? $"{analysis.UnloadingObjects.Count} unloading objects detected and {analysis.PotentialUnloadingObjects.Count} potential unloading objects detected!"
            : "No auto-fail detected.";

    private static HitsoundStudioService CreateHitsoundStudioService(FileBackedEditingGateway gateway)
    {
        NaudioAudioDecoder decoder = new();
        NaudioAudioGenerator generator = new(decoder, new NaudioSoundFontRenderer(), new NaudioAudioEffectService());
        AudioPreviewService preview = new(
            decoder, generator, new NaudioAudioPlaybackService(), new FastFourierSpectrumCalculator());
        return new HitsoundStudioService(
            gateway, new EmptyMapCleanerSampleService(), generator, preview,
            new NaudioAudioExporter(), new NaudioAudioClipMixer(), new NaudioMidiService(),
            new PhysicalHitsoundStudioFileSystem(), new NoopFileRevealService(), new HitsoundStudioEngine());
    }

    private static T ReadTransformationProject<T>(string fixtureRoot, string fixtureName)
    {
        string projectPath = Path.Combine(fixtureRoot, "Projects", $"{fixtureName}.json");
        string fixturesRoot = Path.GetDirectoryName(fixtureRoot)!;
        string json = File.ReadAllText(projectPath).Replace(
            "{fixtureRoot}",
            fixturesRoot.Replace('\\', '/'),
            StringComparison.Ordinal);
        return ReadProjectJson<T>(json);
    }

    private static HitObject ReadLegacySlider(string fixtureRoot, string fixtureName)
    {
        string projectPath = Path.Combine(fixtureRoot, "Projects", $"{fixtureName}.json");
        JObject document = JObject.Parse(File.ReadAllText(projectPath));
        string sliderJson = document["LoadedHitObjects"]?.SingleOrDefault()?.ToString()
            ?? throw new InvalidDataException("The legacy Sliderator fixture contained no source slider.");
        return ProjectJson.Deserialize<HitObject>(sliderJson);
    }

    private static void ApplySlideratorTransientState(SlideratorProject project, HitObject sourceSlider)
    {
        // These values are JsonIgnore in the legacy project format. The desktop
        // view model derives them when it installs the loaded source slider.
        double temporalLength = sourceSlider.TemporalLength;
        double beatsPerMinute = sourceSlider.UnInheritedTimingPoint?.GetBpm() ?? 180;
        project.BeatsPerMinute = beatsPerMinute > 0 ? beatsPerMinute : 180;
        project.GraphBeats = project.BeatsPerMinute * temporalLength / 60000;
        project.PixelLength = sourceSlider.PixelLength;
        if (!project.ManualVelocity)
        {
            project.NewVelocity = SlideratorEngine.GetMaximumVelocity(project);
        }
    }

    private static void StageMapsetMergerSources(
        JsonElement options,
        string fixtureRoot,
        MapsetMergerProject project)
    {
        string sourceRoot = ResolveFixturePath(fixtureRoot, "../Mapsets/multi-difficulty");
        foreach (JsonElement mapsetOptions in options.GetProperty("Mapsets").EnumerateArray())
        {
            string name = StringProperty(mapsetOptions, "Name");
            MapsetMergerProject.MapsetItem mapset = project.Mapsets.Single(item => item.Name == name);
            Directory.CreateDirectory(mapset.Path);
            string beatmapPath = RequiredPath(
                mapsetOptions.GetProperty("Beatmaps").EnumerateArray().Single(),
                fixtureRoot);
            File.Copy(
                beatmapPath,
                Path.Combine(mapset.Path, Path.GetFileName(beatmapPath)),
                overwrite: true);
            foreach (string asset in options.GetProperty("Assets").EnumerateArray().Select(item => item.GetString()!))
            {
                string sourceAsset = Path.Combine(sourceRoot, asset);
                if (File.Exists(sourceAsset))
                {
                    File.Copy(sourceAsset, Path.Combine(mapset.Path, asset), overwrite: true);
                }
            }
        }
    }

    private static PatternGalleryCollectionPaths ReadPatternGalleryPaths(JsonElement options, string fixtureRoot)
    {
        string projectPath = RequiredPath(options, "PatternCollection", fixtureRoot);
        string patternFile = RequiredPath(options, "PatternFile", fixtureRoot);
        string collectionRoot = Path.GetDirectoryName(projectPath)!;
        return new PatternGalleryCollectionPaths(
            Path.GetDirectoryName(collectionRoot)!,
            collectionRoot,
            Path.GetDirectoryName(patternFile)!,
            projectPath);
    }

    private static T ReadProjectJson<T>(string json) => ProjectJson.Deserialize<T>(json);

    private static void AssertMapsetOutputEquivalent(string fixtureRoot, string expectedManifestPath, string actualDirectory)
    {
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(expectedManifestPath));
        foreach (JsonElement item in manifest.RootElement.GetProperty("beatmaps").EnumerateArray())
        {
            string expectedPath = ResolveFixturePath(fixtureRoot, StringProperty(item, "path"));
            string actualPath = Path.Combine(actualDirectory, Path.GetFileName(expectedPath));
            AssertTextOutputEquivalent(expectedPath, actualPath);
        }
        foreach (JsonElement item in manifest.RootElement.GetProperty("exportedAssets").EnumerateArray())
        {
            string relativePath = StringProperty(item, "path");
            string actualPath = Path.Combine(actualDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(actualPath).Should().BeTrue($"Expected merged asset does not exist: {relativePath}");
            FileHash(actualPath).Should().Be(StringProperty(item, "sha256"));
        }
    }

    private static void AssertTextOutputEquivalent(string expectedPath, string actualPath)
    {
        File.Exists(actualPath).Should().BeTrue($"Tool did not write output: {actualPath}");
        string actual = File.ReadAllText(actualPath);
        string expected = File.ReadAllText(expectedPath);
        actual.Should().Be(expected);
    }

    private static string ResolveFixturePath(string fixtureRoot, string relativePath)
    {
        string path = Path.GetFullPath(Path.Combine(fixtureRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        (File.Exists(path) || Directory.Exists(path)).Should().BeTrue($"Fixture path does not exist: {relativePath}");
        return path;
    }

    private static string? OptionalPath(JsonElement element, string property, string fixtureRoot) =>
        OptionalStringProperty(element, property) is { } value ? ResolveFixturePath(fixtureRoot, value) : null;

    private static string RequiredPath(JsonElement element, string property, string fixtureRoot) =>
        ResolveFixturePath(fixtureRoot, StringProperty(element, property));

    private static string RequiredPath(JsonElement value, string fixtureRoot) =>
        ResolveFixturePath(fixtureRoot, value.GetString()!);

    private static string StringProperty(JsonElement element, string property) =>
        element.GetProperty(property).GetString() ?? throw new InvalidDataException($"Fixture property {property} is null.");

    private static string? OptionalStringProperty(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString() : null;

    private static double NumberProperty(JsonElement element, string property) => element.GetProperty(property).GetDouble();
    private static int IntProperty(JsonElement element, string property) => element.GetProperty(property).GetInt32();
    private static bool BoolProperty(JsonElement element, string property) => element.GetProperty(property).GetBoolean();

    private static string FileHash(string path)
    {
        using SHA256 sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(File.ReadAllBytes(path)));
    }

    private sealed record FixtureExecutionResult(
        IReadOnlyList<string>? OutputPaths = null,
        string? JsonOutput = null,
        string? OutputDirectory = null)
    {
        public bool WasExecuted => OutputPaths is not null || JsonOutput is not null || OutputDirectory is not null;
    }

    private sealed class FixtureWorkspace : IDisposable
    {
        public FixtureWorkspace(string sourceRoot)
        {
            Root = Path.Combine(Path.GetTempPath(), "mapping-tools-transformations-" + Guid.NewGuid().ToString("N"));
            foreach (string sourcePath in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string destinationPath = Path.Combine(Root, Path.GetRelativePath(sourceRoot, sourcePath));
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(sourcePath, destinationPath);
            }
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class FileBackedEditingGateway : IBeatmapEditingGateway
    {
        private readonly FileSystemFileStore _files = new();

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new BeatmapEditingSession(
                new BeatmapEditor2(path, _files), BeatmapEditingSource.Disk, []));
        }

        public Task<StoryboardEditor2> OpenStoryboardAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new StoryboardEditor2(path, _files));
        }

        public Task SaveAsync(Editor2 value, bool reloadEditor = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            value.SaveFile();
            return Task.CompletedTask;
        }

        public Task SaveAsync(BeatmapEditingSession session, bool reloadEditor = false, CancellationToken cancellationToken = default) =>
            SaveAsync(session.Editor, reloadEditor, cancellationToken);
    }

    private sealed class EmptyHitsoundSampleService : IHitsoundSampleService
    {
        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(string directory, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        public HitsoundSampleAssignment? TryCreateAssignment(
            string directory, IReadOnlyList<string> sourceFilenames,
            IReadOnlyDictionary<string, string> firstSamples, string role,
            SampleSet sampleSet, int startIndex, SampleSchema existingSchema) => null;

        public Task ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmptyMapCleanerSampleService : IMapCleanerSampleService
    {
        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
            string directory, bool detectDuplicates, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        public Task<int> MoveUnusedToRecoveryAsync(
            string directory, string currentBeatmapPath, Beatmap currentBeatmap,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class NoopFileRevealService : Mapping_Tools.Application.Platform.IFileRevealService
    {
        public Task<bool> RevealAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
