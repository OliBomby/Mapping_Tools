using Mapping_Tools.Application.Execution.ToolExecution;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed class ToolExecutionHostedService : IHostedService
{
    private readonly IToolExecutionService execution;

    public ToolExecutionHostedService(IToolExecutionService execution)
    {
        this.execution = execution ?? throw new ArgumentNullException(nameof(execution));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return execution.StopAsync(cancellationToken);
    }
}
