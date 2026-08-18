using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Material.Icons;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Hosting;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Updates;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Views;

/// <summary>
/// Hosts registered Avalonia features and captures safe normal-state window geometry.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly WindowBounds DefaultBounds = new(80, 60, 1500, 800);
    private readonly ApplicationSettings _settings;
    private readonly SettingsPersistenceHostedService? _settingsPersistence;
    private readonly IUpdaterInteractionService? _updaterInteraction;
    private WindowBounds _normalBounds = DefaultBounds;
    private bool _restored;
    private bool _updateCloseInProgress;

    /// <summary>
    /// Loads a standalone shell instance for XAML tooling and deterministic rendering.
    /// Runtime composition uses the settings-aware constructor.
    /// </summary>
    public MainWindow()
        : this(new ApplicationSettings(), null, null)
    {
    }

    /// <summary>
    /// Loads the compiled shell and attaches the shared window-placement state.
    /// </summary>
    public MainWindow(
        ApplicationSettings settings)
        : this(settings, null, null)
    {
    }

    /// <summary>
    /// Loads the compiled shell and attaches window placement and shutdown-persistence state.
    /// </summary>
    /// <param name="settings">The process-lifetime settings document.</param>
    /// <param name="settingsPersistence">The orderly-shutdown boundary used by Exit without saving.</param>
    public MainWindow(
        ApplicationSettings settings,
        SettingsPersistenceHostedService? settingsPersistence)
        : this(settings, settingsPersistence, null)
    {
    }

    /// <summary>
    /// Loads the compiled shell with persisted placement and updater shutdown coordination.
    /// </summary>
    /// <param name="settings">The process-lifetime settings document.</param>
    /// <param name="settingsPersistence">The orderly-shutdown boundary used by Exit without saving.</param>
    /// <param name="updaterInteraction">The updater interaction owned by runtime composition.</param>
    public MainWindow(
        ApplicationSettings settings,
        SettingsPersistenceHostedService? settingsPersistence,
        IUpdaterInteractionService? updaterInteraction)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsPersistence = settingsPersistence;
        _updaterInteraction = updaterInteraction;
        InitializeComponent();
        AddHandler(KeyDownEvent, HandleWindowKeyDown, RoutingStrategies.Tunnel);
        PositionChanged += (_, _) => CaptureNormalBounds();
        Resized += (_, _) => CaptureNormalBounds();
        PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.Property == WindowStateProperty)
            {
                UpdateWindowChrome();
            }
        };
        UpdateWindowChrome();
    }

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs eventArgs)
    {
        base.OnOpened(eventArgs);
        RestoreWindowPlacement();
        if (DataContext is MainViewModel viewModel)
        {
            _ = viewModel.CheckForUpdatesOnStartupAsync();
        }
    }

    /// <inheritdoc/>
    protected override void OnClosing(WindowClosingEventArgs eventArgs)
    {
        if (!_updateCloseInProgress && _updaterInteraction?.ShouldUpdateOnClose == true)
        {
            eventArgs.Cancel = true;
            _updateCloseInProgress = true;
            _ = CompleteUpdateAndCloseAsync();
            return;
        }

        if (!eventArgs.IsProgrammatic)
        {
            CaptureNormalBounds();
        }

        _settings.MainWindowRestoreBounds = _normalBounds;
        _settings.MainWindowMaximized = WindowState == WindowState.Maximized;
        base.OnClosing(eventArgs);
    }

    private async Task CompleteUpdateAndCloseAsync()
    {
        bool canClose = await _updaterInteraction!.CompleteUpdateOnCloseAsync();
        if (canClose)
        {
            Close();
        }
        else
        {
            _updateCloseInProgress = false;
        }
    }

    private void RestoreWindowPlacement()
    {
        IReadOnlyList<Screen> connected = Screens.All;
        List<DesktopWorkingArea> areas = connected
            .Select(ToWorkingArea)
            .ToList();
        _normalBounds = WindowPlacementCalculator.Restore(
            _settings.MainWindowRestoreBounds,
            areas,
            DefaultBounds);

        DesktopWorkingArea selectedArea = areas
            .OrderByDescending(area => IntersectionArea(_normalBounds, area))
            .FirstOrDefault(area => IntersectionArea(_normalBounds, area) > 0)
            ?? areas.FirstOrDefault(area => area.IsPrimary)
            ?? areas[0];
        Screen screen = connected[
            areas.FindIndex(area => ReferenceEquals(area, selectedArea) || area == selectedArea)];

        Width = _normalBounds.Width;
        Height = _normalBounds.Height;
        Position = new PixelPoint(
            (int)Math.Round(_normalBounds.X * screen.Scaling),
            (int)Math.Round(_normalBounds.Y * screen.Scaling));
        _restored = true;
        if (_settings.MainWindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void CaptureNormalBounds()
    {
        if (!_restored || WindowState != WindowState.Normal)
        {
            return;
        }

        Screen? screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        double scaling = screen?.Scaling ?? 1;
        _normalBounds = new WindowBounds(
            Position.X / scaling,
            Position.Y / scaling,
            Math.Max(MinWidth, Bounds.Width),
            Math.Max(MinHeight, Bounds.Height));
    }

    private static DesktopWorkingArea ToWorkingArea(Screen screen) =>
        new(
            screen.WorkingArea.X / screen.Scaling,
            screen.WorkingArea.Y / screen.Scaling,
            screen.WorkingArea.Width / screen.Scaling,
            screen.WorkingArea.Height / screen.Scaling,
            screen.IsPrimary);

    private static double IntersectionArea(
        WindowBounds bounds,
        DesktopWorkingArea area)
    {
        double width = Math.Max(
            0,
            Math.Min(bounds.X + bounds.Width, area.X + area.Width) -
            Math.Max(bounds.X, area.X));
        double height = Math.Max(
            0,
            Math.Min(bounds.Y + bounds.Height, area.Y + area.Height) -
            Math.Max(bounds.Y, area.Y));
        return width * height;
    }

    private void MinimizeWindow(object? sender, RoutedEventArgs eventArgs) =>
        WindowState = WindowState.Minimized;

    private void ToggleMaximizeWindow(object? sender, RoutedEventArgs eventArgs) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseWindow(object? sender, RoutedEventArgs eventArgs) => Close();

    private void CloseWithoutSaving(object? sender, RoutedEventArgs eventArgs)
    {
        _settingsPersistence?.SuppressSave();
        Close();
    }

    private void UpdateWindowChrome()
    {
        bool maximized = WindowState == WindowState.Maximized;
        RootGrid.Margin = maximized ? new Thickness(7) : new Thickness(0);
        WindowBorder.BorderThickness = maximized ? new Thickness(0) : new Thickness(1);
        MaximizeIcon.Kind = maximized ? MaterialIconKind.WindowRestore : MaterialIconKind.WindowMaximize;
    }

    private void HandleWindowKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.K || eventArgs.KeyModifiers != KeyModifiers.Control)
        {
            return;
        }

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.IsNavigationOpen = true;
            Dispatcher.UIThread.Post(
                () => ToolSearchBox.Focus(),
                DispatcherPriority.Input);
        }

        eventArgs.Handled = true;
    }

    private void DragCurrentMaps(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(eventArgs);
        }
    }

    private void AcceptFileDrop(object? sender, DragEventArgs eventArgs)
    {
        eventArgs.DragEffects = eventArgs.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        eventArgs.Handled = true;
    }

    private void OpenDroppedBeatmaps(object? sender, DragEventArgs eventArgs)
    {
        IReadOnlyList<string> paths = eventArgs.DataTransfer.TryGetFiles()?
            .Select(item => item.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray() ?? [];
        if (paths.Count > 0 && DataContext is MainViewModel viewModel)
        {
            viewModel.Workspace.SetDroppedPaths(paths);
            eventArgs.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            eventArgs.DragEffects = DragDropEffects.None;
        }

        eventArgs.Handled = true;
    }

}
