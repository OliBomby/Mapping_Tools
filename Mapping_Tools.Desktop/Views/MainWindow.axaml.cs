using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.Views;

/// <summary>
/// Hosts the registered Avalonia features and persists safe normal-state window geometry.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly WindowBounds DefaultBounds = new(80, 60, 1500, 800);
    private readonly ApplicationSettings _settings;
    private readonly ISettingsService _settingsService;
    private WindowBounds _normalBounds = DefaultBounds;
    private bool _restored;

    /// <summary>
    /// Loads a standalone shell instance for XAML tooling and deterministic rendering.
    /// Runtime composition uses the settings-aware constructor.
    /// </summary>
    public MainWindow()
        : this(new ApplicationSettings(), new NoOpSettingsService())
    {
    }

    /// <summary>
    /// Loads the compiled shell and attaches settings-backed window persistence.
    /// </summary>
    public MainWindow(
        ApplicationSettings settings,
        ISettingsService settingsService)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        InitializeComponent();
        PositionChanged += (_, _) => CaptureNormalBounds();
        Resized += (_, _) => CaptureNormalBounds();
    }

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs eventArgs)
    {
        base.OnOpened(eventArgs);
        RestoreWindowPlacement();
    }

    /// <inheritdoc/>
    protected override void OnClosing(WindowClosingEventArgs eventArgs)
    {
        if (!eventArgs.IsProgrammatic)
        {
            CaptureNormalBounds();
        }

        _settings.MainWindowRestoreBounds = _normalBounds;
        _settings.MainWindowMaximized = WindowState == WindowState.Maximized;
        _settingsService.Save(_settings);
        base.OnClosing(eventArgs);
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

    private void DragCurrentMaps(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(eventArgs);
        }
    }

    private void OpenWebsite(object? sender, RoutedEventArgs eventArgs) =>
        ExecuteCurrentGetStartedCommand(openSource: false);

    private void OpenGitHub(object? sender, RoutedEventArgs eventArgs) =>
        ExecuteCurrentGetStartedCommand(openSource: true);

    private void ExecuteCurrentGetStartedCommand(bool openSource)
    {
        if (DataContext is not ViewModels.MainViewModel
            {
                CurrentFeature: ViewModels.GetStartedViewModel getStarted
            })
        {
            return;
        }

        _ = (openSource
                ? getStarted.OpenSourceCommand
                : getStarted.OpenWebsiteCommand)
            .Execute()
            .Subscribe();
    }

    private sealed class NoOpSettingsService : ISettingsService
    {
        public SettingsLoadResult LoadOrCreate() =>
            new(new ApplicationSettings(), false, false);

        public void Save(ApplicationSettings settings)
        {
        }
    }
}
