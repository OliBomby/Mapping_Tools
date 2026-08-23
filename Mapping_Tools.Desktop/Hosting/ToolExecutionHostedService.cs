using Mapping_Tools.Application.Execution;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed class ToolExecutionHostedService : IHostedService
{
    private readonly IToolExecutionService _execution;

    public ToolExecutionHostedService(IToolExecutionService execution)
    {
        _execution = execution ?? throw new ArgumentNullException(nameof(execution));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _execution.StopAsync(cancellationToken);
    }
}
