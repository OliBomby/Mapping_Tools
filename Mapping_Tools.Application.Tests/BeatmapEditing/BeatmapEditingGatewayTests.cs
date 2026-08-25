using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.BeatmapEditing;

[TestClass]
public sealed class BeatmapEditingGatewayTests
{
    private const string map_path = @"C:\osu!\Songs\123 Artist - Title\map.osu";

    [TestMethod]
    public async Task OpenBeatmapAsync_WithMatchingLiveState_OverlaysAndPreservesSelection()
    {
        // Arrange
        var store = CreateStore();
        HitObject selected = new()
        {
            Pos = new Vector2(64, 96),
            EndPos = new Vector2(64, 96),
            Time = 2500,
            ObjectType = 1,
        };
        HitObject earlier = new()
        {
            Pos = new Vector2(128, 192),
            EndPos = new Vector2(128, 192),
            Time = 1500,
            ObjectType = 1,
        };
        LiveBeatmapSnapshot snapshot = new(
            map_path,
            [750],
            [new TimingPoint(0, 500, 4, SampleSet.Normal, 0, 70, true, false, false)],
            [selected, earlier],
            1234,
            1.8,
            2,
            2222,
            [selected]);
        RecordingLiveBeatmapReader reader = new(snapshot);
        var gateway = CreateGateway(store, reader);

        // Act
        var session = await gateway.OpenBeatmapAsync(map_path);

        // Assert
        session.Source.Should().Be(BeatmapEditingSource.LiveEditor);
        session.Editor.Beatmap.HitObjects.Select(item => item.Time).ToArray().Should().Equal(1500d, 2500d);
        session.SelectedHitObjects.Count.Should().Be(1);
        session.SelectedHitObjects[0].Should().BeSameAs(selected);
        session.Editor.Beatmap.HitObjects.Contains(selected).Should().BeTrue();
        session.Editor.Beatmap.General["PreviewTime"].IntValue.Should().Be(1234);
        session.Editor.Beatmap.BeatmapTiming.SliderMultiplier.Should().Be(1.8);
        session.Editor.Beatmap.Bookmarks.ToArray().Should().Equal(750d);
        session.LiveEditorTime.Should().Be(2222);
    }

    [TestMethod]
    public async Task OpenBeatmapAsync_WithDiskOnly_DoesNotReadLiveEditor()
    {
        // Arrange
        var store = CreateStore();
        RecordingLiveBeatmapReader reader = new(
            new InvalidOperationException("Reader must not be called."));
        var gateway = CreateGateway(store, reader);

        // Act
        var session = await gateway.OpenBeatmapAsync(
            map_path,
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
        var store = CreateStore();
        LiveBeatmapSnapshot snapshot = new(
            map_path.ToUpperInvariant(),
            [],
            [],
            [],
            -1,
            1.4,
            1);
        var gateway = CreateGateway(
            store,
            new RecordingLiveBeatmapReader(snapshot));

        // Act
        var session = await gateway.OpenBeatmapAsync(map_path);

        // Assert
        session.Source.Should().Be(BeatmapEditingSource.Disk);
        (session.Editor.Beatmap.HitObjects.Count > 0).Should().BeTrue();
        session.Editor.Beatmap.HitObjects[0].ObjectType.Should().NotBe(0);
    }

    [TestMethod]
    public async Task OpenBeatmapAsync_WhenLiveReadFails_RecordsFallbackAndReportsRequiredFailure()
    {
        // Arrange
        var store = CreateStore();
        InvalidDataException failure = new("corrupt memory");
        var gateway = CreateGateway(
            store,
            new RecordingLiveBeatmapReader(failure));

        var fallback = await gateway.OpenBeatmapAsync(map_path);
        // Act
        Func<Task> act1 = () => gateway.OpenBeatmapAsync(
            map_path,
            LiveBeatmapPreference.RequireLive);

        // Assert
        var required = (await act1.Should().ThrowAsync<LiveBeatmapUnavailableException>()).Which;

        fallback.LiveReadFailure.Should().BeSameAs(failure);
        required.InnerException.Should().BeSameAs(failure);
    }

    [TestMethod]
    public async Task OpenBeatmapAsync_WithDisabledLivePreference_ReportsWithoutReaderAccess()
    {
        // Arrange
        var store = CreateStore();
        RecordingLiveBeatmapReader reader = new((LiveBeatmapSnapshot?)null);
        var gateway = CreateGateway(
            store,
            reader,
            new ApplicationSettings { UseEditorReader = false });

        // Act
        Func<Task> act2 = () => gateway.OpenBeatmapAsync(
            map_path,
            LiveBeatmapPreference.RequireLive);

        // Assert
        var exception = (await act2.Should().ThrowAsync<LiveBeatmapUnavailableException>()).Which;

        exception.Message.Should().Contain("disabled");
        reader.ReadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task SaveAsync_WithReload_WritesBeforeReload()
    {
        // Arrange
        var store = CreateStore();
        RecordingEditorReloadService reload = new()
        {
            FileWrittenResolver = () => store.WriteCount > 0,
        };
        RecordingBackupService backup = new(store);
        var gateway = CreateGateway(
            store,
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            reloadService: reload,
            backupService: backup);
        var session = await gateway.OpenBeatmapAsync(
            map_path,
            LiveBeatmapPreference.DiskOnly);
        session.Editor.Beatmap.Metadata["Version"] = new StringValue("Edited");

        // Act
        await gateway.SaveAsync(session, true);

        // Assert
        store.WriteCount.Should().Be(1);
        backup.CreateCount.Should().Be(1);
        backup.BackupPrecededWrite.Should().BeTrue();
        reload.ReloadCount.Should().Be(1);
        reload.FileHadBeenWritten.Should().BeTrue();
        store.Files[map_path].Any(line => line == "Version:Edited").Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveAsync_WhenMandatoryBackupFails_DoesNotWriteOrReload()
    {
        // Arrange
        var store = CreateStore();
        RecordingEditorReloadService reload = new()
        {
            FileWrittenResolver = () => store.WriteCount > 0,
        };
        IOException failure = new("backup volume unavailable");
        RecordingBackupService backup = new(store, failure);
        var gateway = CreateGateway(
            store,
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            reloadService: reload,
            backupService: backup);
        var session = await gateway.OpenBeatmapAsync(
            map_path,
            LiveBeatmapPreference.DiskOnly);

        // Act
        var act3 = () => gateway.SaveAsync(session.Editor, true);

        // Assert
        var exception = (await act3.Should().ThrowAsync<IOException>()).Which;

        exception.Should().BeSameAs(failure);
        store.WriteCount.Should().Be(0);
        reload.ReloadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task OpenBeatmapAsync_WithPreCancelledToken_AvoidsDiskAndLiveReads()
    {
        // Arrange
        var store = CreateStore();
        RecordingLiveBeatmapReader reader = new((LiveBeatmapSnapshot?)null);
        var gateway = CreateGateway(store, reader);
        using CancellationTokenSource source = new();
        source.Cancel();

        // Act
        Func<Task> act4 = () => gateway.OpenBeatmapAsync(
            map_path,
            cancellationToken: source.Token);

        // Assert
        await act4.Should().ThrowAsync<OperationCanceledException>();

        store.ReadCount.Should().Be(0);
        reader.ReadCount.Should().Be(0);
    }

    private static BeatmapEditingGateway CreateGateway(
        RecordingTextFileStore store,
        RecordingLiveBeatmapReader reader,
        ApplicationSettings? settings = null,
        RecordingEditorReloadService? reloadService = null,
        RecordingBackupService? backupService = null)
    {
        return new BeatmapEditingGateway(
            store,
            backupService ?? new RecordingBackupService(store),
            reader,
            reloadService ?? new RecordingEditorReloadService
            {
                FileWrittenResolver = () => store.WriteCount > 0,
            },
            settings ?? new ApplicationSettings());
    }

    private static RecordingTextFileStore CreateStore()
    {
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        return new RecordingTextFileStore(
            map_path,
            File.ReadAllLines(fixture));
    }

    private sealed class RecordingBackupService : IBeatmapBackupService
    {
        private readonly Exception? failure;
        private readonly RecordingTextFileStore store;

        public RecordingBackupService(
            RecordingTextFileStore store,
            Exception? failure = null)
        {
            this.store = store;
            this.failure = failure;
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
            BackupPrecededWrite = store.WriteCount == 0;
            if (failure is not null) return Task.FromException<BeatmapBackupResult>(failure);

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
            BackupPrecededWrite = store.WriteCount == 0;
            if (failure is not null) return Task.FromException<BeatmapBackupResult>(failure);

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
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<BeatmapRestoreResult> RestoreAsync(
            string backupPath,
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<BeatmapRestoreResult?> QuickUndoAsync(
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

}
