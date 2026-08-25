using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Markers;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Controls.Graph;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Views;

/// <summary>Displays Sliderator's graph, source controls, and shared object preview.</summary>
public sealed partial class SlideratorView : UserControl
{
    private readonly Stopwatch previewClock = new();
    private readonly DispatcherTimer previewTimer;
    private GraphState? acceptedGraphState;
    private bool fastNavigationRequested;
    private SlideratorViewModel? observedViewModel;
    private bool restoringGraphState;
    private bool updatingGraphBounds;

    /// <summary>Creates the Sliderator view and connects shared Core-backed controls.</summary>
    public SlideratorView()
    {
        InitializeComponent();
        GraphControlElement.StateChanged += GraphStateChanged;
        DataContextChanged += DataContextChangedHandler;
        previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        previewTimer.Tick += PreviewTimerTick;
        AttachedToVisualTree += (_, _) =>
        {
            previewClock.Restart();
            previewTimer.Start();
            if (observedViewModel is not null) observedViewModel.Interaction ??= new AvaloniaSlideratorInteraction(observedViewModel);
        };
        DetachedFromVisualTree += (_, _) =>
        {
            previewTimer.Stop();
            if (observedViewModel is not null) observedViewModel.Interaction = null;
        };
    }

    /// <summary>Moves to the previous imported slider and honors Shift quick placement.</summary>
    /// <param name="sender">The navigation button.</param>
    /// <param name="args">The routed click event.</param>
    private async void MoveLeft(object? sender, RoutedEventArgs args)
    {
        if (DataContext is not SlideratorViewModel viewModel) return;

        bool fast = fastNavigationRequested;
        fastNavigationRequested = false;
        await viewModel.MoveLeftAsync(fast);
        args.Handled = true;
    }

    /// <summary>Moves to the next imported slider and honors Shift quick placement.</summary>
    /// <param name="sender">The navigation button.</param>
    /// <param name="args">The routed click event.</param>
    private async void MoveRight(object? sender, RoutedEventArgs args)
    {
        if (DataContext is not SlideratorViewModel viewModel) return;

        bool fast = fastNavigationRequested;
        fastNavigationRequested = false;
        await viewModel.MoveRightAsync(fast);
        args.Handled = true;
    }

    private void GraphStateChanged(object? sender, GraphStateChangedEventArgs args)
    {
        if (DataContext is SlideratorViewModel viewModel)
        {
            if (restoringGraphState) return;

            if (!viewModel.IsGraphWithinVelocityLimit(args.State) && acceptedGraphState is not null)
            {
                restoringGraphState = true;
                try
                {
                    GraphControlElement.GraphState = acceptedGraphState.Clone();
                    viewModel.ApplyGraphState(acceptedGraphState);
                }
                finally
                {
                    restoringGraphState = false;
                }

                return;
            }

            acceptedGraphState = args.State.Clone();
            viewModel.ApplyGraphState(args.State);
            previewClock.Restart();
        }
    }

    private void NavigationPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        fastNavigationRequested = args.KeyModifiers.HasAllFlags(KeyModifiers.Shift);
    }

    private void DataContextChangedHandler(object? sender, EventArgs args)
    {
        if (observedViewModel is not null)
        {
            observedViewModel.PropertyChanged -= ViewModelPropertyChanged;
            observedViewModel.Interaction = null;
        }

        if (DataContext is SlideratorViewModel viewModel)
        {
            observedViewModel = viewModel;
            viewModel.Interaction = new AvaloniaSlideratorInteraction(viewModel);
            viewModel.PropertyChanged += ViewModelPropertyChanged;
            acceptedGraphState = viewModel.GraphState.Clone();
            UpdateGraphBounds(viewModel);
            UpdateGraphMarkers(viewModel);
            UpdatePreviewMarkers(viewModel);
        }
        else
        {
            observedViewModel = null;
            acceptedGraphState = null;
            PreviewControl.ExtraMarkers = [];
        }
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        previewClock.Restart();
        if (sender is SlideratorViewModel viewModel
            && args.PropertyName is nameof(SlideratorViewModel.GraphBeats) or
                nameof(SlideratorViewModel.GraphMinY) or
                nameof(SlideratorViewModel.GraphMaxY) or
                nameof(SlideratorViewModel.VelocityLimit) or
                nameof(SlideratorViewModel.GraphState) or
                nameof(SlideratorViewModel.BeatSnapDivisor) or
                nameof(SlideratorViewModel.BeatsPerMinute) or
                nameof(SlideratorViewModel.GraphModeSetting))
        {
            UpdateGraphBounds(viewModel);
            UpdateGraphMarkers(viewModel);
        }

        if (sender is SlideratorViewModel markerViewModel
            && args.PropertyName is nameof(SlideratorViewModel.GraphState) or
                nameof(SlideratorViewModel.GraphModeSetting) or
                nameof(SlideratorViewModel.GlobalSv) or
                nameof(SlideratorViewModel.PixelLength) or
                nameof(SlideratorViewModel.ShowRedAnchors) or
                nameof(SlideratorViewModel.ShowGraphAnchors) or
                nameof(SlideratorViewModel.VisibleHitObject))
        {
            if (args.PropertyName == nameof(SlideratorViewModel.GraphState))
                Dispatcher.UIThread.Post(() =>
                {
                    if (!restoringGraphState && ReferenceEquals(observedViewModel, markerViewModel)) acceptedGraphState = markerViewModel.GraphState.Clone();
                });

            UpdatePreviewMarkers(markerViewModel);
        }
    }

    private void UpdateGraphBounds(SlideratorViewModel viewModel)
    {
        if (updatingGraphBounds) return;

        updatingGraphBounds = true;
        try
        {
            GraphControlElement.MinX = 0;
            GraphControlElement.MaxX = viewModel.GraphBeats;
            GraphControlElement.MinY = viewModel.GraphMinY;
            GraphControlElement.MaxY = viewModel.GraphMaxY;
        }
        finally
        {
            updatingGraphBounds = false;
        }
    }

    private void UpdateGraphMarkers(SlideratorViewModel viewModel)
    {
        GraphControlElement.HorizontalMarkerGenerator = new CompositeMarkerGenerator(
        [
            new DividedBeatMarkerGenerator(viewModel.BeatSnapDivisor, true),
            new CustomMarkerGenerator
            {
                Snappable = true,
                StepSize = viewModel.BeatsPerMinute / 60000,
            },
        ]);
        GraphControlElement.VerticalMarkerGenerator = viewModel.GraphModeSetting == SlideratorGraphMode.Velocity
            ? new DoubleMarkerGenerator(0, 1 / 4d, "x")
            : new DoubleMarkerGenerator(0, 1 / 4d);

        if (viewModel.ShowRedAnchors && viewModel.GraphModeSetting == SlideratorGraphMode.Position && viewModel.VisibleHitObject?.IsSlider == true)
        {
            var sourcePath = viewModel.VisibleHitObject.GetSliderPath();
            GraphControlElement.Markers = SliderPathUtil
                .GetRedAnchorCompletions(sourcePath)
                .Select(completion => new GraphMarker
                {
                    Orientation = GraphMarkerOrientation.Horizontal,
                    Value = completion,
                    Text = null,
                    Snappable = true,
                    CustomLineColorArgb = 0xFFFF0000,
                })
                .ToArray();
        }
        else
        {
            GraphControlElement.Markers = [];
        }
    }

    private void UpdatePreviewMarkers(SlideratorViewModel viewModel)
    {
        if (!(viewModel.ShowRedAnchors || viewModel.ShowGraphAnchors) || viewModel.VisibleHitObject?.IsSlider != true)
        {
            PreviewControl.ExtraMarkers = [];
            return;
        }

        SlideratorOptions options = new()
        {
            GlobalSv = viewModel.GlobalSv,
            PixelLength = viewModel.PixelLength,
            GraphModeSetting = viewModel.GraphModeSetting,
            GraphState = viewModel.GraphState,
        };
        double maximum = SlideratorEngine.GetMaxCompletion(options);
        if (!double.IsFinite(maximum) || maximum <= 0)
        {
            PreviewControl.ExtraMarkers = [];
            return;
        }

        List<ObjectVisualiserMarker> markers = [];
        var sourcePath = viewModel.VisibleHitObject.GetSliderPath();
        if (viewModel.ShowRedAnchors)
            markers.AddRange(
                SliderPathUtil.GetRedAnchorCompletions(sourcePath)
                    .Select(completion => new ObjectVisualiserMarker(
                        completion / maximum,
                        0.2,
                        Brushes.Red)));

        if (viewModel.ShowGraphAnchors)
        {
            var completions = viewModel.GraphModeSetting == SlideratorGraphMode.Velocity
                ? viewModel.GraphState.Anchors.Select(anchor =>
                    viewModel.GraphState.GetIntegral(0, anchor.Pos.X) * SlideratorEngine.GetSvGraphMultiplier(options))
                : viewModel.GraphState.Anchors.Select(anchor => anchor.Pos.Y);
            markers.AddRange(
                completions.Select(completion => new ObjectVisualiserMarker(
                    completion / maximum,
                    0.2,
                    Brushes.DodgerBlue)));
        }

        PreviewControl.ExtraMarkers = markers;
    }

    private void PreviewTimerTick(object? sender, EventArgs args)
    {
        if (DataContext is SlideratorViewModel viewModel) viewModel.SetPreviewProgress(viewModel.EvaluatePreviewProgress(previewClock.Elapsed.TotalMilliseconds));
    }

    private sealed class AvaloniaSlideratorInteraction : ISlideratorInteraction
    {
        private readonly SlideratorViewModel viewModel;

        internal AvaloniaSlideratorInteraction(SlideratorViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public Task<bool> RunFastAsync(CancellationToken cancellationToken = default)
        {
            return viewModel.RunFastPlacementAsync(cancellationToken);
        }
    }
}
