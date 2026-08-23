using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Desktop.Shell;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed record MappingToolQuickRunRegistration(
    ToolDefinition Definition,
    Func<CancellationToken, Task> Execute);

internal sealed class MappingToolQuickRunHostedService : IHostedService
{
    private readonly IUiDispatcher dispatcher;
    private readonly IQuickRunCommandRegistry registry;
    private readonly IReadOnlyList<MappingToolQuickRunRegistration> tools;

    public MappingToolQuickRunHostedService(
        IQuickRunCommandRegistry registry,
        IEnumerable<MappingToolQuickRunRegistration> tools,
        IUiDispatcher dispatcher)
    {
        this.registry = registry;
        this.tools = tools.ToArray();
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var tool in tools)
            if (registry.Commands.All(command => command.Id != tool.Definition.Id))
                registry.Register(new QuickRunCommand(
                    tool.Definition,
                    cancellationToken => ExecuteOnUiThreadAsync(
                        tool.Execute,
                        cancellationToken)));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var tool in tools) registry.Remove(tool.Definition.Id);

        return Task.CompletedTask;
    }

    private Task ExecuteOnUiThreadAsync(
        Func<CancellationToken, Task> execute,
        CancellationToken cancellationToken)
    {
        TaskCompletionSource completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.Post(() => _ = completeAsync());
        return completion.Task;

        async Task completeAsync()
        {
            try
            {
                await execute(cancellationToken);
                completion.TrySetResult();
            }
            catch (OperationCanceledException exception) when (
                cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }
    }
}
