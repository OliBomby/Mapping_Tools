using Mapping_Tools.ApplicationServices.Abstractions;
using Mapping_Tools.ApplicationServices.BeatmapEditing;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class BeatmapEditingGatewayTests
{
    private const string MapPath = @"C:\osu!\Songs\123 Artist - Title\map.osu";

    [TestMethod]
    public async Task PreferLiveOverlaysMatchingStateAndPreservesSelectedObjectIdentity()
    {
        MemoryTextFileStore store = CreateStore();
        HitObject selected = new()
        {
            Pos = new Vector2(64, 96),
            EndPos = new Vector2(64, 96),
            Time = 2500,
            ObjectType = 1,
            IsSelected = true
        };
        HitObject earlier = new()
        {
            Pos = new Vector2(128, 192),
            EndPos = new Vector2(128, 192),
            Time = 1500,
            ObjectType = 1
        };
        LiveBeatmapSnapshot snapshot = new(
            MapPath,
            [750],
            [new TimingPoint(0, 500, 4, SampleSet.Normal, 0, 70, true, false, false)],
            [selected, earlier],
            1234,
            1.8,
            2);
        FakeLiveBeatmapReader reader = new(snapshot);
        BeatmapEditingGateway gateway = CreateGateway(store, reader);

        BeatmapEditingSession session = await gateway.OpenBeatmapAsync(MapPath);

        Assert.AreEqual(BeatmapEditingSource.LiveEditor, session.Source);
        CollectionAssert.AreEqual(
            new[] { 1500d, 2500d },
            session.Editor.Beatmap.HitObjects.Select(item => item.Time).ToArray());
        Assert.AreEqual(1, session.SelectedHitObjects.Count);
        Assert.AreSame(selected, session.SelectedHitObjects[0]);
        Assert.IsTrue(session.Editor.Beatmap.HitObjects.Contains(selected));
        Assert.AreEqual(1234, session.Editor.Beatmap.General["PreviewTime"].IntValue);
        Assert.AreEqual(1.8, session.Editor.Beatmap.BeatmapTiming.SliderMultiplier);
        CollectionAssert.AreEqual(
            new[] { 750d },
            session.Editor.Beatmap.Bookmarks.ToArray());
    }

    [TestMethod]
    public async Task DiskOnlyDoesNotTouchLiveReader()
    {
        MemoryTextFileStore store = CreateStore();
        FakeLiveBeatmapReader reader = new(
            new InvalidOperationException("Reader must not be called."));
        BeatmapEditingGateway gateway = CreateGateway(store, reader);

        BeatmapEditingSession session = await gateway.OpenBeatmapAsync(
            MapPath,
            LiveBeatmapPreference.DiskOnly);

        Assert.AreEqual(BeatmapEditingSource.Disk, session.Source);
        Assert.AreEqual(0, reader.ReadCount);
        Assert.IsNull(session.LiveReadFailure);
    }

    [TestMethod]
    public async Task PreferLiveKeepsDiskWhenOsuIsEditingAnotherMap()
    {
        MemoryTextFileStore store = CreateStore();
        LiveBeatmapSnapshot snapshot = new(
            MapPath.ToUpperInvariant(),
            [],
            [],
            [],
            -1,
            1.4,
            1);
        BeatmapEditingGateway gateway = CreateGateway(
            store,
            new FakeLiveBeatmapReader(snapshot));

        BeatmapEditingSession session = await gateway.OpenBeatmapAsync(MapPath);

        Assert.AreEqual(BeatmapEditingSource.Disk, session.Source);
        Assert.IsTrue(session.Editor.Beatmap.HitObjects.Count > 0);
        Assert.AreNotEqual(0, session.Editor.Beatmap.HitObjects[0].ObjectType);
    }

    [TestMethod]
    public async Task PreferLiveRecordsFailureWhileRequireLiveReportsIt()
    {
        MemoryTextFileStore store = CreateStore();
        InvalidDataException failure = new("corrupt memory");
        BeatmapEditingGateway gateway = CreateGateway(
            store,
            new FakeLiveBeatmapReader(failure));

        BeatmapEditingSession fallback = await gateway.OpenBeatmapAsync(MapPath);
        LiveBeatmapUnavailableException required = await Assert.ThrowsExceptionAsync<
            LiveBeatmapUnavailableException>(
            () => gateway.OpenBeatmapAsync(
                MapPath,
                LiveBeatmapPreference.RequireLive));

        Assert.AreSame(failure, fallback.LiveReadFailure);
        Assert.AreSame(failure, required.InnerException);
    }

    [TestMethod]
    public async Task RequireLiveReportsDisabledPreferenceWithoutReadingProcessMemory()
    {
        MemoryTextFileStore store = CreateStore();
        FakeLiveBeatmapReader reader = new((LiveBeatmapSnapshot?)null);
        BeatmapEditingGateway gateway = CreateGateway(
            store,
            reader,
            new ApplicationSettings { UseEditorReader = false });

        LiveBeatmapUnavailableException exception = await Assert.ThrowsExceptionAsync<
            LiveBeatmapUnavailableException>(
            () => gateway.OpenBeatmapAsync(
                MapPath,
                LiveBeatmapPreference.RequireLive));

        StringAssert.Contains(exception.Message, "disabled");
        Assert.AreEqual(0, reader.ReadCount);
    }

    [TestMethod]
    public async Task SaveWritesCurrentDocumentBeforeRequestingReload()
    {
        MemoryTextFileStore store = CreateStore();
        RecordingReloadService reload = new(store);
        BeatmapEditingGateway gateway = CreateGateway(
            store,
            new FakeLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            reloadService: reload);
        BeatmapEditingSession session = await gateway.OpenBeatmapAsync(
            MapPath,
            LiveBeatmapPreference.DiskOnly);
        session.Editor.Beatmap.Metadata["Version"] = new TValue("Edited");

        await gateway.SaveAsync(session.Editor, reloadEditor: true);

        Assert.AreEqual(1, store.WriteCount);
        Assert.AreEqual(1, reload.ReloadCount);
        Assert.IsTrue(reload.FileHadBeenWritten);
        Assert.IsTrue(store.Files[MapPath].Any(line => line == "Version:Edited"));
    }

    [TestMethod]
    public async Task CancellationBeforeOpenAvoidsDiskAndLiveReads()
    {
        MemoryTextFileStore store = CreateStore();
        FakeLiveBeatmapReader reader = new((LiveBeatmapSnapshot?)null);
        BeatmapEditingGateway gateway = CreateGateway(store, reader);
        using CancellationTokenSource source = new();
        source.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => gateway.OpenBeatmapAsync(
                MapPath,
                cancellationToken: source.Token));

        Assert.AreEqual(0, store.ReadCount);
        Assert.AreEqual(0, reader.ReadCount);
    }

    private static BeatmapEditingGateway CreateGateway(
        MemoryTextFileStore store,
        FakeLiveBeatmapReader reader,
        ApplicationSettings? settings = null,
        RecordingReloadService? reloadService = null)
    {
        return new BeatmapEditingGateway(
            store,
            reader,
            reloadService ?? new RecordingReloadService(store),
            settings ?? new ApplicationSettings());
    }

    private static MemoryTextFileStore CreateStore()
    {
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        return new MemoryTextFileStore(
            MapPath,
            File.ReadAllLines(fixture));
    }

    private sealed class FakeLiveBeatmapReader : ILiveBeatmapReader
    {
        private readonly LiveBeatmapSnapshot? _snapshot;
        private readonly Exception? _failure;

        public FakeLiveBeatmapReader(LiveBeatmapSnapshot? snapshot)
        {
            _snapshot = snapshot;
        }

        public FakeLiveBeatmapReader(Exception failure)
        {
            _failure = failure;
        }

        public int ReadCount { get; private set; }

        public Task<LiveBeatmapSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return _failure is null
                ? Task.FromResult(_snapshot)
                : Task.FromException<LiveBeatmapSnapshot?>(_failure);
        }
    }

    private sealed class RecordingReloadService : IEditorReloadService
    {
        private readonly MemoryTextFileStore _store;

        public RecordingReloadService(MemoryTextFileStore store)
        {
            _store = store;
        }

        public int ReloadCount { get; private set; }

        public bool FileHadBeenWritten { get; private set; }

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReloadCount++;
            FileHadBeenWritten = _store.WriteCount > 0;
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryTextFileStore : ITextFileStore
    {
        public MemoryTextFileStore(string path, IEnumerable<string> lines)
        {
            Files[path] = lines.ToList();
        }

        public Dictionary<string, List<string>> Files { get; } =
            new(StringComparer.Ordinal);

        public int ReadCount { get; private set; }

        public int WriteCount { get; private set; }

        public IReadOnlyList<string> ReadAllLines(string path)
        {
            ReadCount++;
            return Files[path].ToList();
        }

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
            WriteCount++;
            Files[path] = lines.ToList();
        }

        public void Delete(string path)
        {
            Files.Remove(path);
        }

        public string GetParentFolder(string path)
        {
            return Path.GetDirectoryName(path)!;
        }

        public string CombinePath(string parent, string child)
        {
            return Path.Combine(parent, child);
        }
    }
}
