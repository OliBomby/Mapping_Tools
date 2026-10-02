using CommunityToolkit.Mvvm.ComponentModel;

namespace Mapping_Tools.Desktop.Tests.Controls.FeatureLoading;

public sealed class FirstFeatureViewModel : ObservableObject
{
    public int ActionCount { get; private set; }

    public void PerformAction() => ActionCount++;
}
