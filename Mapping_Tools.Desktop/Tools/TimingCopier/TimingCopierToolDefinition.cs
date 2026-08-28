using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.TimingCopier.ViewModels;
using Mapping_Tools.Desktop.Tools.TimingCopier.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Tools.TimingCopier;

/// <summary>Describes and composes the Timing Copier plugin feature.</summary>
[MappingToolDefinition]
public sealed class TimingCopierToolRegistration : IMappingToolDefinition
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
    public int Order => 180;

    /// <inheritdoc />
    public ToolDefinition Definition => TimingCopierToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(TimingCopierViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(TimingCopierView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ITimingCopierService, TimingCopierService>();
    }
}
