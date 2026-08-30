using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.TimingHelper;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.TimingHelper.ViewModels;
using Mapping_Tools.Desktop.Tools.TimingHelper.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Tools.TimingHelper;

/// <summary>Describes and composes the Timing Helper plugin feature.</summary>
[MappingToolDefinition]
public sealed class TimingHelperToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 190;

    /// <inheritdoc />
    public ToolDefinition Definition => TimingHelperToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(TimingHelperViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(TimingHelperView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ITimingHelperService, TimingHelperService>();
    }
}
