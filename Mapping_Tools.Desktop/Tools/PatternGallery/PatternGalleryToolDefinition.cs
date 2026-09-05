using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.PatternGallery;
using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Desktop.Plugin;
using Mapping_Tools.Desktop.Tools.PatternGallery.ViewModels;
using Mapping_Tools.Desktop.Tools.PatternGallery.Views;
using Mapping_Tools.Infrastructure.Tools.PatternGallery;
using Microsoft.Extensions.DependencyInjection;


namespace Mapping_Tools.Desktop.Tools.PatternGallery;

/// <summary>Describes and composes the Pattern Gallery plugin feature.</summary>
[MappingToolDefinition]
public sealed class PatternGalleryToolRegistration : IMappingToolDefinition
{
    /// <inheritdoc />
    public ToolScrollBarVisibility HorizontalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public ToolScrollBarVisibility VerticalScrollBarVisibility => ToolScrollBarVisibility.Disabled;

    /// <inheritdoc />
    public int Order => 270;

    /// <inheritdoc />
    public ToolDefinition Definition => PatternGalleryToolDefinition.Definition;

    /// <inheritdoc />
    public Type ViewModelType => typeof(PatternGalleryViewModel);

    /// <inheritdoc />
    public Type ViewType => typeof(PatternGalleryView);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IPatternGalleryService, PatternGalleryService>();
        services.AddSingleton<IPatternGalleryFileService, PatternGalleryFileService>();
        services.AddSingleton<IPatternGalleryArchiveService, PatternGalleryArchiveService>();
    }
}
