using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Core.Tools.MetadataManager;

namespace Mapping_Tools.Desktop.Tests.Tools.MetadataManager.ViewModels;

internal sealed class RecordingMetadataManagerService : IMetadataManagerService
{
    public MetadataManagerServiceOptions? Options { get; private set; }

    public Task<MetadataManagerEngineOptions> ImportAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MetadataManagerEngineOptions());
    }

    public Task<MetadataManagerResult> ExportAsync(
        MetadataManagerServiceOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Options = options;
        progress?.Report(1);
        string[] paths = options.ExportPath.Split('|', StringSplitOptions.RemoveEmptyEntries);
        return Task.FromResult(new MetadataManagerResult(paths));
    }
}
