using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests;

[TestClass]
public sealed class BeatmapEditingGatewayTests
{
    private const string MapPath = @"C:\osu!\Songs\123 Artist - Title\map.osu";

    [TestMethod]
    public async Task OpenBeatmapAsync_WithMatchingLiveState_OverlaysAndPreservesSelection()
    {
        // Arrange
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

        // Act
        BeatmapEditingSession session = await gateway.OpenBeatmapAsync(MapPath);

        // Assert
        session.Source.Should().Be(BeatmapEditingSource.LiveEditor);
        session.Editor.Beatmap.HitObjects.Select(item => item.Time).ToArray().Should().Equal(new[] { 1500d, 2500d });
        session.SelectedHitObjects.Count.Should().Be(1);
        session.SelectedHitObjects[0].Should().BeSameAs(selected);
        session.Editor.Beatmap.HitObjects.Contains(selected).Should().BeTrue();
        session.Editor.Beatmap.General["PreviewTime"].IntValue.Should().Be(1234);
        session.Editor.Beatmap.BeatmapTiming.SliderMultiplier.Should().Be(1.8);
        session.Editor.Beatmap.Bookmarks.ToArray().Should().Equal(new[] { 750d });
    }

    [TestMethod]
    public async Task OpenBeatmapAsync_WithDiskOnly_DoesNotReadLiveEditor()
    {
        // Arrange
        MemoryTextFileStore store = CreateStore();
        FakeLiveBeatmapReader reader = new(
            new InvalidOperationException("Reader must not be called."));
        BeatmapEditingGateway gateway = CreateGateway(store, reader);

        // Act
        BeatmapEditingSession session = await gateway.OpenBeatmapAsync(
            MapPath,
            LiveBeatmapPreference.DiskOnly);

        // Assert
        session.Source.Should().Be(BeatmapEditingSource.Disk);
        reader.ReadCount.Should().Be(0);
        session.LiveReadFailure.Should().BeNull();
    }

    [TestMethod]
    public async Task OpenBeatmapAsync_WhenLiveMapDiffers_KeepsDiskDocument()
    {
        // Arrange
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

        // Act
        BeatmapEditingSession session = await gateway.OpenBeatmapAsync(MapPath);

        // Assert
        session.Source.Should().Be(BeatmapEditingSource.Disk);
        (session.Editor.Beatmap.HitObjects.Count > 0).Should().BeTrue();
        session.Editor.Beatmap.HitObjects[0].ObjectType.Should().NotBe(0);
    }

    [TestMethod]
    public async Task OpenBeatmapAsync_WhenLiveReadFails_RecordsFallbackAndReportsRequiredFailure()
    {
        // Arrange
        MemoryTextFileStore store = CreateStore();
        InvalidDataException failure = new("corrupt memory");
        BeatmapEditingGateway gateway = CreateGateway(
            store,
            new FakeLiveBeatmapReader(failure));

        BeatmapEditingSession fallback = await gateway.OpenBeatmapAsync(MapPath);
        // Act
        Func<Task> act1 = () => gateway.OpenBeatmapAsync(
                MapPath,
                LiveBeatmapPreference.RequireLive);

        // Assert
        LiveBeatmapUnavailableException required = (await act1.Should().ThrowAsync<LiveBeatmapUnavailableException>()).Which;

        fallback.LiveReadFailure.Should().BeSameAs(failure);
        required.InnerException.Should().BeSameAs(failure);
    }

    [TestMethod]
    public async Task OpenBeatmapAsync_WithDisabledLivePreference_ReportsWithoutReaderAccess()
    {
        // Arrange
        MemoryTextFileStore store = CreateStore();
        FakeLiveBeatmapReader reader = new((LiveBeatmapSnapshot?)null);
        BeatmapEditingGateway gateway = CreateGateway(
            store,
            reader,
            new ApplicationSettings { UseEditorReader = false });

        // Act
        Func<Task> act2 = () => gateway.OpenBeatmapAsync(
                MapPath,
                LiveBeatmapPreference.RequireLive);

        // Assert
        LiveBeatmapUnavailableException exception = (await act2.Should().ThrowAsync<LiveBeatmapUnavailableException>()).Which;

        exception.Message.Should().Contain("disabled");
        reader.ReadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task SaveAsync_WithReload_WritesBeforeReload()
    {
        // Arrange
        MemoryTextFileStore store = CreateStore();
        RecordingReloadService reload = new(store);
        RecordingBackupService backup = new(store);
        BeatmapEditingGateway gateway = CreateGateway(
            store,
            new FakeLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            reloadService: reload,
            backupService: backup);
        BeatmapEditingSession session = await gateway.OpenBeatmapAsync(
            MapPath,
            LiveBeatmapPreference.DiskOnly);
        session.Editor.Beatmap.Metadata["Version"] = new TValue("Edited");

        // Act
        await gateway.SaveAsync(session, reloadEditor: true);

        // Assert
        store.WriteCount.Should().Be(1);
        backup.CreateCount.Should().Be(1);
        backup.BackupPrecededWrite.Should().BeTrue();
        reload.ReloadCount.Should().Be(1);
        reload.FileHadBeenWritten.Should().BeTrue();
        store.Files[MapPath].Any(line => line == "Version:Edited").Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveAsync_WhenMandatoryBackupFails_DoesNotWriteOrReload()
    {
        // Arrange
        MemoryTextFileStore store = CreateStore();
        RecordingReloadService reload = new(store);
        IOException failure = new("backup volume unavailable");
        RecordingBackupService backup = new(store, failure);
        BeatmapEditingGateway gateway = CreateGateway(
            store,
            new FakeLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            reloadService: reload,
            backupService: backup);
        BeatmapEditingSession session = await gateway.OpenBeatmapAsync(
            MapPath,
            LiveBeatmapPreference.DiskOnly);

        // Act
        Func<Task> act3 = () => gateway.SaveAsync(session.Editor, reloadEditor: true);

        // Assert
        IOException exception = (await act3.Should().ThrowAsync<IOException>()).Which;

        exception.Should().BeSameAs(failure);
        store.WriteCount.Should().Be(0);
        reload.ReloadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task OpenBeatmapAsync_WithPreCancelledToken_AvoidsDiskAndLiveReads()
    {
        // Arrange
        MemoryTextFileStore store = CreateStore();
        FakeLiveBeatmapReader reader = new((LiveBeatmapSnapshot?)null);
        BeatmapEditingGateway gateway = CreateGateway(store, reader);
        using CancellationTokenSource source = new();
        source.Cancel();

        // Act
        Func<Task> act4 = () => gateway.OpenBeatmapAsync(
                MapPath,
                cancellationToken: source.Token);

        // Assert
        await act4.Should().ThrowAsync<OperationCanceledException>();

        store.ReadCount.Should().Be(0);
        reader.ReadCount.Should().Be(0);
    }

    private static BeatmapEditingGateway CreateGateway(
        MemoryTextFileStore store,
        FakeLiveBeatmapReader reader,
        ApplicationSettings? settings = null,
        RecordingReloadService? reloadService = null,
        RecordingBackupService? backupService = null)
    {
        return new BeatmapEditingGateway(
            store,
            backupService ?? new RecordingBackupService(store),
            reader,
            reloadService ?? new RecordingReloadService(store),
            settings ?? new ApplicationSettings());
    }

    private sealed class RecordingBackupService : IBeatmapBackupService
    {
        private readonly MemoryTextFileStore _store;
        private readonly Exception? _failure;

        public RecordingBackupService(
            MemoryTextFileStore store,
            Exception? failure = null)
        {
            _store = store;
            _failure = failure;
        }

        public int CreateCount { get; private set; }

        public bool BackupPrecededWrite { get; private set; }

        public Task<BeatmapBackupResult> CreateAsync(
            IEnumerable<string> sourcePaths,
            BeatmapBackupReason reason,
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateCount++;
            BackupPrecededWrite = _store.WriteCount == 0;
            if (_failure is not null)
            {
                return Task.FromException<BeatmapBackupResult>(_failure);
            }

            BeatmapBackupArtifact artifact = new(
                "backup.osu",
                sourcePaths.Single(),
                reason,
                false,
                DateTimeOffset.UnixEpoch);
            return Task.FromResult(
                new BeatmapBackupResult([artifact], false));
        }

        public Task<BeatmapBackupResult> CreateAsync(
            BeatmapEditingSession session,
            BeatmapBackupReason reason,
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateCount++;
            BackupPrecededWrite = _store.WriteCount == 0;
            if (_failure is not null)
            {
                return Task.FromException<BeatmapBackupResult>(_failure);
            }

            BeatmapBackupArtifact artifact = new(
                "backup.osu",
                session.Editor.Path,
                reason,
                session.Source == BeatmapEditingSource.LiveEditor,
                DateTimeOffset.UnixEpoch);
            return Task.FromResult(
                new BeatmapBackupResult([artifact], false));
        }

        public Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
            BeatmapEditingSession session,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BeatmapRestoreResult> RestoreAsync(
            string backupPath,
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BeatmapRestoreResult?> QuickUndoAsync(
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
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
