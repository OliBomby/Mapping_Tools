using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation;
using Mapping_Tools.Core.Graph.Markers;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Controls.Graph;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Tools.Sliderator.ViewModels;

namespace Mapping_Tools.Desktop.Tools.Sliderator.Views;

/// <summary>Displays Sliderator's graph, source controls, and shared object preview.</summary>
public sealed partial class SlideratorView : UserControl
{
    private readonly Stopwatch previewClock = new();
    private readonly DispatcherTimer previewTimer;
    private GraphState? acceptedGraphState;
    private bool fastNavigationRequested;
    private SlideratorViewModel? observedViewModel;
    private bool applyingGraphState;
    private bool restoringGraphState;

    /// <summary>Creates the Sliderator view and connects shared Core-backed controls.</summary>
    public SlideratorView()
    {
        InitializeComponent();
        GraphControlElement.SetBrush(new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)));
        GraphControlElement.StateChanged += GraphStateChanged;
        DataContextChanged += DataContextChangedHandler;
        previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        previewTimer.Tick += PreviewTimerTick;
        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is SlideratorViewModel && !ReferenceEquals(observedViewModel, DataContext))
                DataContextChangedHandler(this, EventArgs.Empty);

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

            if (TryClipGraphAnchorToVelocityLimit(viewModel, args.State, out var clippedState))
            {
                restoringGraphState = true;
                try
                {
                    GraphControlElement.SetGraphStatePreservingView(clippedState);
                    viewModel.ApplyGraphState(clippedState);
                    acceptedGraphState = clippedState.Clone();
                }
                finally
                {
                    restoringGraphState = false;
                }

                return;
            }

            acceptedGraphState = args.State.Clone();
            applyingGraphState = true;
            try
            {
                viewModel.ApplyGraphState(args.State);
            }
            finally
            {
                applyingGraphState = false;
            }
            previewClock.Restart();
        }
    }

    private bool TryClipGraphAnchorToVelocityLimit(SlideratorViewModel viewModel, GraphState candidate, out GraphState clippedState)
    {
        clippedState = candidate.Clone();
        if (!viewModel.ExportAsNormal || acceptedGraphState is null)
            return false;

        int anchorIndex = FindChangedAnchorIndex(acceptedGraphState, candidate);
        if (anchorIndex < 0) anchorIndex = GraphControlElement.SelectedAnchorIndex ?? -1;
        if (anchorIndex <= 0 || anchorIndex >= candidate.Anchors.Count || acceptedGraphState.Anchors.Count != candidate.Anchors.Count)
            return false;

        var candidateAnchor = candidate.Anchors[anchorIndex];
        var acceptedAnchor = acceptedGraphState.Anchors[anchorIndex];
        bool positionChanged = Math.Abs(candidateAnchor.Pos.X - acceptedAnchor.Pos.X) > 1e-9
            || Math.Abs(candidateAnchor.Pos.Y - acceptedAnchor.Pos.Y) > 1e-9;
        bool tensionChanged = Math.Abs(candidateAnchor.Tension - acceptedAnchor.Tension) > 1e-9;
        bool interpolatorChanged = candidateAnchor.Interpolator.GetType() != acceptedAnchor.Interpolator.GetType();
        if (!positionChanged && !tensionChanged && !interpolatorChanged)
            return false;

        if (!IsGraphOverSpeedLimit(viewModel, candidate, anchorIndex)) return false;

        if (tensionChanged && !positionChanged)
        {
            ClipAnchorTensionToVelocityLimit(viewModel, candidate, acceptedGraphState, anchorIndex);
            if (IsGraphOverSpeedLimit(viewModel, candidate, anchorIndex))
            {
                clippedState = acceptedGraphState.Clone();
                return true;
            }

            clippedState = candidate;
            return true;
        }

        if (viewModel.GraphModeSetting != SlideratorGraphMode.Position) return false;

        List<(double Min, double Max)> bounds = [];
        AddPreviousVelocityBounds(viewModel, candidate, anchorIndex, bounds);
        AddNextVelocityBounds(viewModel, candidate, anchorIndex, bounds);
        if (bounds.Count == 0) return false;

        double lowerBound = bounds.Max(bound => bound.Min);
        double upperBound = bounds.Min(bound => bound.Max);
        if (lowerBound <= upperBound)
        {
            double clippedY = Math.Clamp(candidateAnchor.Pos.Y, lowerBound, upperBound);
            candidateAnchor.Pos = new Vector2(candidateAnchor.Pos.X, clippedY);
        }
        else
        {
            ClipAnchorAlongMovement(viewModel, candidate, acceptedGraphState, anchorIndex);
        }

        if (IsGraphOverSpeedLimit(viewModel, candidate, anchorIndex))
        {
            clippedState = acceptedGraphState.Clone();
            return true;
        }

        clippedState = candidate;
        return true;
    }

    private static void ClipAnchorTensionToVelocityLimit(
        SlideratorViewModel viewModel,
        GraphState candidate,
        GraphState accepted,
        int anchorIndex)
    {
        double acceptedTension = GraphControl.ClampTension(accepted.Anchors[anchorIndex].Tension);
        double candidateTension = GraphControl.ClampTension(candidate.Anchors[anchorIndex].Tension);
        double lower = 0;
        double upper = 1;
        for (int iteration = 0; iteration < 24; iteration++)
        {
            double progress = (lower + upper) / 2;
            candidate.Anchors[anchorIndex].Tension = acceptedTension + (candidateTension - acceptedTension) * progress;
            if (IsGraphOverSpeedLimit(viewModel, candidate, anchorIndex)) upper = progress;
            else lower = progress;
        }

        candidate.Anchors[anchorIndex].Tension = GraphControl.ClampTension(
            acceptedTension + (candidateTension - acceptedTension) * lower);
    }

    private static void ClipAnchorAlongMovement(
        SlideratorViewModel viewModel,
        GraphState candidate,
        GraphState accepted,
        int anchorIndex)
    {
        var acceptedPosition = accepted.Anchors[anchorIndex].Pos;
        var candidateAnchor = candidate.Anchors[anchorIndex];
        var candidatePosition = candidateAnchor.Pos;
        double lower = 0;
        double upper = 1;
        for (int iteration = 0; iteration < 24; iteration++)
        {
            double progress = (lower + upper) / 2;
            candidateAnchor.Pos = new Vector2(
                acceptedPosition.X + (candidatePosition.X - acceptedPosition.X) * progress,
                acceptedPosition.Y + (candidatePosition.Y - acceptedPosition.Y) * progress);
            if (IsGraphOverSpeedLimit(viewModel, candidate, anchorIndex)) upper = progress;
            else lower = progress;
        }

        candidateAnchor.Pos = new Vector2(
            acceptedPosition.X + (candidatePosition.X - acceptedPosition.X) * lower,
            acceptedPosition.Y + (candidatePosition.Y - acceptedPosition.Y) * lower);
    }

    private static int FindChangedAnchorIndex(GraphState previous, GraphState candidate)
    {
        for (int index = 0; index < candidate.Anchors.Count; index++)
        {
            if (index >= previous.Anchors.Count
                || Math.Abs(previous.Anchors[index].Pos.X - candidate.Anchors[index].Pos.X) > 1e-9
                || Math.Abs(previous.Anchors[index].Pos.Y - candidate.Anchors[index].Pos.Y) > 1e-9
                || Math.Abs(previous.Anchors[index].Tension - candidate.Anchors[index].Tension) > 1e-9
                || previous.Anchors[index].Interpolator.GetType() != candidate.Anchors[index].Interpolator.GetType())
                return index;
        }

        return -1;
    }

    private static bool IsGraphOverSpeedLimit(SlideratorViewModel viewModel, GraphState state, int anchorIndex)
    {
        return IsAnchorOverSpeedLimit(viewModel, state, anchorIndex)
            || !viewModel.IsGraphWithinVelocityLimit(state);
    }

    private static bool IsAnchorOverSpeedLimit(SlideratorViewModel viewModel, GraphState state, int anchorIndex)
    {
        return IsPreviousSegmentOverSpeedLimit(viewModel, state, anchorIndex)
            || IsNextSegmentOverSpeedLimit(viewModel, state, anchorIndex);
    }

    private static bool IsPreviousSegmentOverSpeedLimit(SlideratorViewModel viewModel, GraphState state, int anchorIndex)
    {
        if (anchorIndex <= 0) return false;

        var anchor = state.Anchors[anchorIndex];
        var previous = state.Anchors[anchorIndex - 1];
        if (viewModel.GraphModeSetting == SlideratorGraphMode.Velocity)
            return Math.Abs(GraphInterpolatorCatalog.GetBiggestValue(anchor.Interpolator)) > viewModel.VelocityLimit;

        double difference = anchor.Pos.Y - previous.Pos.Y;
        double distance = anchor.Pos.X - previous.Pos.X;
        if (!double.IsFinite(distance) || Math.Abs(distance) <= Precision.DOUBLE_EPSILON)
            return true;

        double maximumDerivative = GraphInterpolatorCatalog.GetBiggestDerivative(anchor.Interpolator);
        double velocity = Math.Abs(maximumDerivative * difference / distance) / viewModel.SvGraphMultiplier;
        return !double.IsFinite(velocity) || velocity > viewModel.VelocityLimit + Precision.DOUBLE_EPSILON;
    }

    private static bool IsNextSegmentOverSpeedLimit(SlideratorViewModel viewModel, GraphState state, int anchorIndex)
    {
        if (anchorIndex >= state.Anchors.Count - 1) return false;

        var anchor = state.Anchors[anchorIndex];
        var next = state.Anchors[anchorIndex + 1];
        if (viewModel.GraphModeSetting == SlideratorGraphMode.Velocity)
            return Math.Abs(GraphInterpolatorCatalog.GetBiggestValue(next.Interpolator)) > viewModel.VelocityLimit;

        double difference = next.Pos.Y - anchor.Pos.Y;
        double distance = next.Pos.X - anchor.Pos.X;
        if (!double.IsFinite(distance) || Math.Abs(distance) <= Precision.DOUBLE_EPSILON)
            return true;

        double maximumDerivative = GraphInterpolatorCatalog.GetBiggestDerivative(next.Interpolator);
        double velocity = Math.Abs(maximumDerivative * difference / distance) / viewModel.SvGraphMultiplier;
        return !double.IsFinite(velocity) || velocity > viewModel.VelocityLimit + Precision.DOUBLE_EPSILON;
    }

    private static void AddPreviousVelocityBounds(
        SlideratorViewModel viewModel,
        GraphState state,
        int anchorIndex,
        ICollection<(double Min, double Max)> bounds)
    {
        var anchor = state.Anchors[anchorIndex];
        var previous = state.Anchors[anchorIndex - 1];
        double maximumDerivative = GraphInterpolatorCatalog.GetBiggestDerivative(anchor.Interpolator);
        double distance = anchor.Pos.X - previous.Pos.X;
        if (Math.Abs(distance) <= Precision.DOUBLE_EPSILON)
        {
            bounds.Add((previous.Pos.Y, previous.Pos.Y));
            return;
        }

        double allowedDifference = viewModel.VelocityLimit * viewModel.SvGraphMultiplier * distance / maximumDerivative;
        bounds.Add((
            previous.Pos.Y + Precision.DOUBLE_EPSILON - allowedDifference,
            previous.Pos.Y - Precision.DOUBLE_EPSILON + allowedDifference));
    }

    private static void AddNextVelocityBounds(
        SlideratorViewModel viewModel,
        GraphState state,
        int anchorIndex,
        ICollection<(double Min, double Max)> bounds)
    {
        if (anchorIndex >= state.Anchors.Count - 1) return;

        var anchor = state.Anchors[anchorIndex];
        var next = state.Anchors[anchorIndex + 1];
        double maximumDerivative = GraphInterpolatorCatalog.GetBiggestDerivative(next.Interpolator);
        double distance = next.Pos.X - anchor.Pos.X;
        if (Math.Abs(distance) <= Precision.DOUBLE_EPSILON)
        {
            bounds.Add((next.Pos.Y, next.Pos.Y));
            return;
        }

        double allowedDifference = viewModel.VelocityLimit * viewModel.SvGraphMultiplier * distance / maximumDerivative;
        bounds.Add((
            next.Pos.Y + Precision.DOUBLE_EPSILON - allowedDifference,
            next.Pos.Y - Precision.DOUBLE_EPSILON + allowedDifference));
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
            SetGraphStateFromViewModel(viewModel);
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
            if (args.PropertyName == nameof(SlideratorViewModel.GraphState)
                && !applyingGraphState
                && !restoringGraphState)
                SetGraphStateFromViewModel(viewModel);

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
            UpdatePreviewMarkers(markerViewModel);
        }
    }

    private void SetGraphStateFromViewModel(SlideratorViewModel viewModel)
    {
        if (!ReferenceEquals(observedViewModel, viewModel)) return;

        GraphControlElement.SetGraphState(viewModel.GraphState);
        acceptedGraphState = viewModel.GraphState.Clone();
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

        SlideratorEngineOptions options = new()
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
