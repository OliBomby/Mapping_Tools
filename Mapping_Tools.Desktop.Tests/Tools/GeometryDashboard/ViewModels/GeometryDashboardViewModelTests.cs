using Avalonia.Input;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.ViewModels;

[TestClass]
public sealed class GeometryDashboardViewModelTests
{
    [TestMethod]
    public void Constructor_WithCoreGenerators_GroupsAndFiltersRows()
    {
        // Arrange
        using var viewModel = CreateViewModel();

        // Act
        viewModel.Generators.Should().NotBeEmpty();
        viewModel.GeneratorGroups.Should().NotBeEmpty();

        viewModel.Filter = "circle";

        // Assert
        viewModel.GeneratorGroups.SelectMany(group => group.Generators)
            .Should().OnlyContain(generator => generator.Name.Contains("circle", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WhenInputPlatformIsUnavailable_ShowsGracefulStatus()
    {
        // Arrange
        using var viewModel = CreateViewModel(false);

        // Act
        await viewModel.RefreshOnceAsync();

        // Assert
        viewModel.Status.Should().Be("Geometry Dashboard requires Windows.");
    }

    [TestMethod]
    public async Task Activate_WithSaveSlot_RegistersGlobalBindingAndLoadsSlot()
    {
        // Arrange
        var globalHotkeys = new RecordingGlobalHotkeyService();
        using var viewModel = CreateViewModel(globalHotkeys: globalHotkeys);
        var slot = new GeometryDashboardSaveSlot
        {
            ProjectHotkey = new HotkeySettings(56, 2),
        };
        slot.Preferences.AcceptableDifference = 70;
        viewModel.Project.CurrentPreferences.AcceptableDifference = 2;
        viewModel.Project.SaveSlots.Add(slot);

        // Act
        viewModel.Activate();
        string bindingId = globalHotkeys.Bindings.Single().Key;
        await globalHotkeys.Callbacks[bindingId](CancellationToken.None);

        // Assert
        globalHotkeys.Bindings[bindingId].Should().Be(new HotkeySettings(56, 2));
        viewModel.Project.CurrentPreferences.AcceptableDifference.Should().Be(70);
    }

    [TestMethod]
    public void Deactivate_WithActiveSaveSlot_RemovesGlobalBinding()
    {
        // Arrange
        var globalHotkeys = new RecordingGlobalHotkeyService();
        using var viewModel = CreateViewModel(globalHotkeys: globalHotkeys);
        viewModel.Project.SaveSlots.Add(new GeometryDashboardSaveSlot
        {
            ProjectHotkey = new HotkeySettings(56, 0),
        });
        viewModel.Activate();
        string bindingId = globalHotkeys.Bindings.Single().Key;

        // Act
        viewModel.Deactivate();

        // Assert
        globalHotkeys.Bindings[bindingId].Should().BeNull();
    }

    [TestMethod]
    public void ToggleSelected_WithShiftModifierAndEmptyGraph_DoesNotCreateObjects()
    {
        // Arrange
        using var viewModel = CreateViewModel();

        // Act
        viewModel.ToggleSelected(KeyModifiers.Shift);

        // Assert
        viewModel.DrawableCount.Should().Be(0);
        viewModel.SelectedCount.Should().Be(0);
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WhenEditorSelectionChanges_SynchronizesRootSelectionState()
    {
        // Arrange
        HitObject initialHitObject = new("64,96,1000,1,0,0:0:0:0:");
        HitObject selectedHitObject = new("64,96,1000,1,0,0:0:0:0:");
        HitObject finalHitObject = new("64,96,1000,1,0,0:0:0:0:");
        using var viewModel = CreateViewModel(
            snapshots:
            [
                CreateRuntimeSnapshot(initialHitObject, 0, []),
                CreateRuntimeSnapshot(selectedHitObject, 1, [selectedHitObject]),
                CreateRuntimeSnapshot(finalHitObject, 2, []),
            ]);

        // Act
        await viewModel.RefreshOnceAsync();
        int unselectedCount = viewModel.SelectedCount;
        await viewModel.RefreshOnceAsync();
        int selectedCount = viewModel.SelectedCount;
        await viewModel.RefreshOnceAsync();

        // Assert
        unselectedCount.Should().Be(0);
        selectedCount.Should().BeGreaterThan(0);
        viewModel.SelectedCount.Should().Be(0);
    }

    private static GeometryDashboardViewModel CreateViewModel(
        bool inputSupported = true,
        IGlobalHotkeyService? globalHotkeys = null,
        params GeometryDashboardRuntimeSnapshot?[] snapshots)
    {
        return new GeometryDashboardViewModel(
            new ApplicationSettings(),
            globalHotkeys ?? new RecordingGlobalHotkeyService(),
            new RuntimeStub(snapshots),
            new InputStub(inputSupported),
            new OverlayStub(),
            new SerializerStub(),
            new TestFilePicker
            {
                CanOpenFiles = false,
                CanSaveFiles = false,
                CanPickFolders = false,
            },
            new TextFileStoreStub(),
            new NotificationStub(),
            static () => null!,
            new ImmediateTestDispatcher());
    }

    private static GeometryDashboardRuntimeSnapshot CreateRuntimeSnapshot(
        HitObject hitObject,
        int editorTime,
        IReadOnlyList<HitObject> selectedHitObjects)
    {
        return new GeometryDashboardRuntimeSnapshot(
            new LiveBeatmapSnapshot(
                "C:/Songs/map/map.osu",
                [],
                [],
                [hitObject],
                0,
                1.4,
                1,
                5,
                4,
                editorTime,
                selectedHitObjects),
            true);
    }

    private sealed class RuntimeStub(IEnumerable<GeometryDashboardRuntimeSnapshot?> snapshots) : IGeometryDashboardRuntime
    {
        private readonly Queue<GeometryDashboardRuntimeSnapshot?> snapshots = new(snapshots);

        public Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(snapshots.Count == 0 ? null : snapshots.Dequeue());
        }
    }

    private sealed class InputStub(bool isSupported) : IGeometryDashboardInputService
    {
        public bool IsSupported => isSupported;

        public bool IsHotkeyDown(HotkeySettings? hotkey)
        {
            return false;
        }

        public bool IsMouseButtonDown(GeometryDashboardMouseButton button)
        {
            return false;
        }

        public bool TryGetCursorPosition(out Vector2 position)
        {
            position = Vector2.Zero;
            return false;
        }

        public bool TrySetCursorPosition(Vector2 position)
        {
            return false;
        }
    }

    private sealed class RecordingGlobalHotkeyService : IGlobalHotkeyService
    {
        public Dictionary<string, HotkeySettings?> Bindings { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, Func<CancellationToken, Task>> Callbacks { get; } =
            new(StringComparer.Ordinal);

        public void SetBinding(
            string id,
            HotkeySettings? hotkey,
            Func<CancellationToken, Task> callback)
        {
            Bindings[id] = hotkey;
            Callbacks[id] = callback;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }

    private sealed class OverlayStub : IGeometryDashboardOverlayService
    {
        public bool IsSupported => false;
        public bool IsVisible => false;
        public string? ConfigurationStatus => null;
        public void Update(GeometryDashboardOverlayScene scene, GeometryDashboardOverlayOptions options) { }
        public void Hide() { }
        public void Dispose() { }
    }

    private sealed class SerializerStub : IProjectSerializer
    {
        public string Serialize<TProject>(TProject project)
        {
            return "{}";
        }

        public TProject Deserialize<TProject>(string json)
        {
            return Activator.CreateInstance<TProject>();
        }
    }

    private sealed class TextFileStoreStub : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path)
        {
            return [];
        }

        public void WriteAllLines(string path, IEnumerable<string> lines) { }
        public void Delete(string path) { }

        public string GetParentFolder(string path)
        {
            return string.Empty;
        }

        public string CombinePath(string parent, string child)
        {
            return child;
        }
    }

    private sealed class NotificationStub : IUserNotificationService
    {
        public event EventHandler<UserNotificationPublishedEventArgs>? Published;

        public Task PublishAsync(UserNotification notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

}
