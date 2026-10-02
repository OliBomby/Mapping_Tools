using Mapping_Tools.Application.Tools.SliderCompletionator;

namespace Mapping_Tools.Desktop.Tests.Tools.SliderCompletionator.ViewModels;

internal sealed class RecordingCompletionator : ISliderCompletionatorService
{
    public IReadOnlyList<string>? Paths { get; private set; }

    public SliderCompletionatorServiceOptions? Options { get; private set; }

    public Task<SliderCompletionatorResult> CompleteAsync(
        IReadOnlyList<string> paths,
        SliderCompletionatorServiceOptions options,
        bool quickRun = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Paths = paths.ToArray();
        Options = options;
        progress?.Report(1);
        return Task.FromResult(new SliderCompletionatorResult(paths, 2));
    }
}
