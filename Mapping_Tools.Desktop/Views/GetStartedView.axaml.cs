using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Mapping_Tools.Desktop.Views;

/// <summary>
/// Renders the offline Get started landing page inside the main shell.
/// </summary>
public partial class GetStartedView : UserControl
{
    /// <summary>Loads the compiled landing-page view.</summary>
    public GetStartedView()
    {
        InitializeComponent();
    }

    private void ScrollOnboardingLeft(object? sender, RoutedEventArgs eventArgs) =>
        OnboardingScroller.Offset = new Avalonia.Vector(
            OnboardingScroller.Offset.X - 32,
            OnboardingScroller.Offset.Y);

    private void ScrollOnboardingRight(object? sender, RoutedEventArgs eventArgs) =>
        OnboardingScroller.Offset = new Avalonia.Vector(
            OnboardingScroller.Offset.X + 32,
            OnboardingScroller.Offset.Y);
}
