using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Mapping_Tools.Desktop.Controls.FeatureLoading;

/// <summary>
///     Displays a feature view after yielding the UI thread, with a lightweight loading state
///     and one cached view instance per feature type.
/// </summary>
public sealed partial class FeatureContentHost : UserControl
{
    /// <summary>Identifies the feature view-model presented by this host.</summary>
    public static readonly StyledProperty<object?> FeatureProperty =
        AvaloniaProperty.Register<FeatureContentHost, object?>(nameof(Feature));

    /// <summary>Identifies the optional feature preparation error.</summary>
    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<FeatureContentHost, string?>(nameof(ErrorMessage));

    private readonly Dictionary<Type, Control> viewCache = [];
    private readonly ViewLocator viewLocator = new();
    private long loadVersion;

    /// <summary>Creates the deferred feature view host.</summary>
    public FeatureContentHost()
    {
        InitializeComponent();
        ShowLoading();
    }

    /// <summary>Gets or sets the feature view-model to display.</summary>
    public object? Feature
    {
        get => GetValue(FeatureProperty);
        set => SetValue(FeatureProperty, value);
    }

    /// <summary>Gets or sets the feature preparation error shown by the host.</summary>
    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FeatureProperty || change.Property == ErrorMessageProperty) BeginLoad();
    }

    private void BeginLoad()
    {
        long currentVersion = ++loadVersion;
        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            ShowError(ErrorMessage);
            return;
        }

        if (Feature is null)
        {
            ShowLoading();
            return;
        }

        if (viewCache.TryGetValue(Feature.GetType(), out Control? cachedView))
        {
            ShowView(cachedView);
            return;
        }

        ShowLoading();
        _ = LoadViewAsync(Feature, currentVersion);
    }

    private async Task LoadViewAsync(object feature, long currentVersion)
    {
        try
        {
            // Keep the loading surface interactive for one dispatcher turn before constructing AXAML.
            await Dispatcher.Yield(DispatcherPriority.Background);
            if (currentVersion != loadVersion || !ReferenceEquals(feature, Feature)) return;

            if (!string.IsNullOrWhiteSpace(ErrorMessage))
            {
                ShowError(ErrorMessage);
                return;
            }

            Control view;
            Type featureType = feature.GetType();
            if (viewCache.TryGetValue(featureType, out Control? cachedView))
            {
                view = cachedView;
            }
            else
            {
                view = viewLocator.Build(feature) ?? new TextBlock { Text = "No view registered." };
                view.DataContext = feature;
                view.IsVisible = false;
                view.IsHitTestVisible = false;
                viewCache.Add(featureType, view);
                CachedViews.Children.Add(view);
            }

            if (currentVersion == loadVersion && ReferenceEquals(feature, Feature)) ShowView(view);
        }
        catch (Exception exception)
        {
            if (currentVersion == loadVersion && ReferenceEquals(feature, Feature))
                ShowError(exception.Message);
        }
    }

    private void ShowLoading()
    {
        HideCachedViews();
        StateHost.Content = new FeatureLoadingSkeleton();
        StateHost.IsVisible = true;
    }

    private void ShowError(string? message)
    {
        HideCachedViews();
        StateHost.Content = new FeatureLoadErrorView { Message = message };
        StateHost.IsVisible = true;
    }

    private void ShowView(Control view)
    {
        StateHost.IsVisible = false;
        StateHost.Content = null;

        foreach (Control cachedView in CachedViews.Children)
        {
            bool isActive = ReferenceEquals(cachedView, view);
            cachedView.IsVisible = isActive;
            cachedView.IsHitTestVisible = isActive;
        }
    }

    private void HideCachedViews()
    {
        foreach (Control cachedView in CachedViews.Children)
        {
            cachedView.IsVisible = false;
            cachedView.IsHitTestVisible = false;
        }
    }

}
