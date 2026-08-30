using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.AutoFailDetector.ViewModels;
using Mapping_Tools.Desktop.Tools.AutoFailDetector.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop.Tools.AutoFailDetector;

/// <summary>Describes and composes the Auto-fail Detector plugin feature.</summary>
[MappingToolDefinition]
public sealed class AutoFailDetectorToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Auto;

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public ToolDefinition Definition => AutoFailDetectorToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(AutoFailDetectorViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(AutoFailDetectorView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IAutoFailService, AutoFailService>();
    }
}
