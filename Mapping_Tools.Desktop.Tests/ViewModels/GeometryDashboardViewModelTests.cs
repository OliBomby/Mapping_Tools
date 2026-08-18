using Avalonia.Input;
using FluentAssertions;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class GeometryDashboardViewModelTests
{
    [TestMethod]
    public void Constructor_WithCoreGenerators_GroupsAndFiltersRows()
    {
        // Arrange
        using GeometryDashboardViewModel viewModel = CreateViewModel();

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
        using GeometryDashboardViewModel viewModel = CreateViewModel(inputSupported: false);

        // Act
        await viewModel.RefreshOnceAsync();

        // Assert
        viewModel.Status.Should().Be("Geometry Dashboard requires Windows.");
    }

    [TestMethod]
    public void ToggleSelected_WithShiftModifierAndEmptyGraph_DoesNotCreateObjects()
    {
        // Arrange
        using GeometryDashboardViewModel viewModel = CreateViewModel();

        // Act
        viewModel.ToggleSelected(KeyModifiers.Shift);

        // Assert
        viewModel.DrawableCount.Should().Be(0);
        viewModel.SelectedCount.Should().Be(0);
    }

    private static GeometryDashboardViewModel CreateViewModel(bool inputSupported = true) =>
        new(
            new ApplicationSettings(),
            new RuntimeStub(),
            new InputStub(inputSupported),
            new OverlayFactoryStub(),
            new SerializerStub(),
            new FilePickerStub(),
            new TextFileStoreStub(),
            new NotificationStub(),
            new DialogStub(),
            new DispatcherStub());

    private sealed class RuntimeStub : IGeometryDashboardRuntime
    {
        public Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<GeometryDashboardRuntimeSnapshot?>(null);
    }

    private sealed class InputStub(bool isSupported) : IGeometryDashboardInputService
    {
        public bool IsSupported => isSupported;
        public bool IsHotkeyDown(Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization.Hotkey? hotkey) => false;
        public bool IsMouseButtonDown(GeometryDashboardMouseButton button) => false;
        public bool TryGetCursorPosition(out Vector2 position) { position = Vector2.Zero; return false; }
        public bool TrySetCursorPosition(Vector2 position) => false;
    }

    private sealed class OverlayFactoryStub : IGeometryDashboardOverlayHostFactory
    {
        public IGeometryDashboardOverlayHost Create() => new OverlayStub();
    }

    private sealed class OverlayStub : IGeometryDashboardOverlayHost
    {
        public bool IsSupported => false;
        public bool IsVisible => false;
        public PlatformWindowId? TargetWindow => null;
        public void Initialize(PlatformWindowId targetWindow) { }
        public void Enable() { }
        public void Disable() { }
        public void Update(Box2 physicalBounds, Vector2 dpiMultiplier, bool dpiSourceAvailable) { }
        public void SetBorder(bool enabled) { }
        public void SetFrame(GeometryDashboardOverlayFrame frame) { }
        public void Invalidate() { }
        public void Dispose() { }
    }

    private sealed class SerializerStub : IProjectSerializer
    {
        public string Serialize<TProject>(TProject project) => "{}";
        public TProject Deserialize<TProject>(string json) => Activator.CreateInstance<TProject>();
    }

    private sealed class FilePickerStub : IFilePicker
    {
        public bool CanOpenFiles => false;
        public bool CanSaveFiles => false;
        public bool CanPickFolders => false;
        public Task<IReadOnlyList<string>> PickOpenFilesAsync(OpenFilePickerRequest request, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> PickSaveFileAsync(SaveFilePickerRequest request, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<IReadOnlyList<string>> PickFoldersAsync(OpenFolderPickerRequest request, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class TextFileStoreStub : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path) => [];
        public void WriteAllLines(string path, IEnumerable<string> lines) { }
        public void Delete(string path) { }
        public string GetParentFolder(string path) => string.Empty;
        public string CombinePath(string parent, string child) => child;
    }

    private sealed class NotificationStub : IUserNotificationService
    {
        public event EventHandler<UserNotificationPublishedEventArgs>? Published;
        public Task PublishAsync(UserNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DialogStub : IGeometryDashboardDialogService
    {
        public Task<SnappingToolsPreferences?> ShowPreferencesAsync(SnappingToolsPreferences preferences) => Task.FromResult<SnappingToolsPreferences?>(null);
        public Task ShowProjectSlotsAsync(
            SnappingToolsProject project,
            Action<SnappingToolsSaveSlot> loadSlot,
            Action refreshHotkeys) => Task.CompletedTask;
        public Task<bool> ShowGeneratorSettingsAsync(GeneratorSettings settings) => Task.FromResult(false);
    }

    private sealed class DispatcherStub : Mapping_Tools.Desktop.Shell.IUiDispatcher
    {
        public void Post(Action action) => action();
    }
}
