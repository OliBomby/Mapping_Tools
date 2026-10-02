using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Tests.Controls.FeatureLoading;

public sealed class FirstFeatureView : UserControl
{
    public FirstFeatureView()
    {
        Button action = new() { Name = "FeatureAction", Content = "Run" };
        action.Click += (_, _) => ((FirstFeatureViewModel)DataContext!).PerformAction();
        Content = new StackPanel
        {
            Children =
            {
                new TextBox { Name = "FeatureInput" },
                action,
            },
        };
    }
}
