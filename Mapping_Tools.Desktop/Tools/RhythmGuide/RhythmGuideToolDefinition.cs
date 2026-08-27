using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Interactions;
using Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Views;
using Microsoft.Extensions.DependencyInjection;
using ApplicationDefinition = Mapping_Tools.Application.Tools.RhythmGuide.RhythmGuideToolDefinition;
using MainWindow = Mapping_Tools.Desktop.Views.MainWindow;

namespace Mapping_Tools.Desktop.Tools.RhythmGuide;

/// <summary>Describes and composes the Rhythm Guide plugin feature.</summary>
[MappingToolDefinition]
public sealed class RhythmGuideToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public string Category => "Tools";

    /// <inheritdoc />
    public bool StartsSection => false;

    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 120;

    /// <inheritdoc />
    public ToolDefinition Definition => ApplicationDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(RhythmGuideViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(RhythmGuideView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IRhythmGuideService, RhythmGuideService>();
        services.AddSingleton<IRhythmGuideWindowService>(provider =>
            new AvaloniaRhythmGuideWindowService(
                () => provider.GetRequiredService<MainWindow>()));
    }
}
