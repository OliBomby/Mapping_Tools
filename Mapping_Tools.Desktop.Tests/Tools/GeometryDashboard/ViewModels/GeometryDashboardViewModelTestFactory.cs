using Avalonia.Controls;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.GeometryDashboard;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.ViewModels;

internal static class GeometryDashboardViewModelTestFactory
{
    internal static GeometryDashboardViewModel CreateViewModel(
        bool inputSupported = true,
        IGlobalHotkeyService? globalHotkeys = null,
        params GeometryDashboardRuntimeSnapshot?[] snapshots)
    {
        return CreateViewModelSession(inputSupported, globalHotkeys, snapshots: snapshots).ViewModel;
    }

    internal static (GeometryDashboardViewModel ViewModel, GeometryDashboardService Service) CreateViewModelSession(
        bool inputSupported = true,
        IGlobalHotkeyService? globalHotkeys = null,
        bool overlaySupported = false,
        Func<Window>? owner = null,
        string? overlayConfigurationStatus = null,
        params GeometryDashboardRuntimeSnapshot?[] snapshots)
    {
        var project = new GeometryDashboardProject();
        var service = new GeometryDashboardService(
            new ApplicationSettings(),
            project,
            new RuntimeStub(snapshots),
            new InputStub(inputSupported),
            new OverlayStub { IsSupported = overlaySupported, ConfigurationStatus = overlayConfigurationStatus });
        var lifecycle = new GeometryDashboardLifecycleCoordinator(project, service);
        GeometryDashboardViewModel viewModel = new(
            project,
            service,
            lifecycle,
            globalHotkeys ?? new RecordingGlobalHotkeyService(),
            new SerializerStub(),
            new TestFilePicker { CanOpenFiles = false, CanSaveFiles = false, CanPickFolders = false },
            new TextFileStoreStub(),
            new NotificationStub(),
            owner ?? (() => null!),
            new ImmediateTestDispatcher());

        return (viewModel, service);
    }

    internal static GeometryDashboardRuntimeSnapshot CreateRuntimeSnapshot(
        HitObject hitObject,
        int editorTime,
        IReadOnlyList<HitObject> selectedHitObjects)
    {
        return new GeometryDashboardRuntimeSnapshot(
            new LiveBeatmapSnapshot("C:/Songs/map/map.osu", [], [], [hitObject], 0, 1.4, 1, 5, 4, editorTime, selectedHitObjects),
            true);
    }

    internal static GeometryDashboardRuntimeSnapshot CreateRuntimeSnapshot(IReadOnlyList<HitObject> hitObjects)
    {
        return new GeometryDashboardRuntimeSnapshot(
            new LiveBeatmapSnapshot("C:/Songs/map/map.osu", [], [], hitObjects, 0, 1.4, 1, 5, 4, 0, []),
            true);
    }

    private sealed class RuntimeStub(IEnumerable<GeometryDashboardRuntimeSnapshot?> snapshots) : IGeometryDashboardRuntime
    {
        private readonly Queue<GeometryDashboardRuntimeSnapshot?> snapshots = new(snapshots);
        public bool IsProcessRunning => true;
        public Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(snapshots.Count == 0 ? null : snapshots.Dequeue());
        }
    }

    private sealed class InputStub(bool isSupported) : IGeometryDashboardInputService
    {
        public bool IsSupported => isSupported;
        public bool IsHotkeyDown(HotkeySettings? hotkey) => false;
        public bool IsMouseButtonDown(GeometryDashboardMouseButton button) => false;
        public bool TryGetCursorPosition(out Vector2 position) { position = Vector2.Zero; return false; }
        public bool TrySetCursorPosition(Vector2 position) => false;
    }

    private sealed class OverlayStub : IGeometryDashboardOverlayService
    {
        public bool IsSupported { get; init; }
        public bool IsVisible => false;
        public string? ConfigurationStatus { get; init; }
        public void Update(GeometryDashboardOverlayScene scene, GeometryDashboardOverlayOptions options) { }
        public void Hide() { }
        public void Dispose() { }
    }

    private sealed class SerializerStub : IProjectSerializer
    {
        public string Serialize<TProject>(TProject project) => "{}";
        public TProject Deserialize<TProject>(string json) => Activator.CreateInstance<TProject>();
    }

    private sealed class TextFileStoreStub : ITextFileStore
    {
        public string ReadAllText(string path) => string.Empty;
        public void WriteAllText(string path, string text) { }
        public void Delete(string path) { }
        public string GetParentFolder(string path) => string.Empty;
        public string CombinePath(string parent, string child) => child;
    }

    private sealed class NotificationStub : IUserNotificationService
    {
        public event EventHandler<UserNotificationPublishedEventArgs>? Published;
        public Task PublishAsync(UserNotification notification, CancellationToken cancellationToken = default)
        {
            Published?.Invoke(this, new UserNotificationPublishedEventArgs(notification));
            return Task.CompletedTask;
        }
    }

}
