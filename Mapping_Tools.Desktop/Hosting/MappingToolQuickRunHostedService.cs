using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Desktop.Shell;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed record MappingToolQuickRunRegistration(
    string Id,
    string DisplayName,
    QuickRunTargets Targets,
    Func<CancellationToken, Task> Execute);

internal sealed class MappingToolQuickRunHostedService : IHostedService
{
    private readonly IQuickRunCommandRegistry _registry;
    private readonly IReadOnlyList<MappingToolQuickRunRegistration> _tools;
    private readonly IUiDispatcher _dispatcher;

    public MappingToolQuickRunHostedService(
        IQuickRunCommandRegistry registry,
        IEnumerable<MappingToolQuickRunRegistration> tools,
        IUiDispatcher dispatcher)
    {
        _registry = registry;
        _tools = tools.ToArray();
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (MappingToolQuickRunRegistration tool in _tools)
        {
            if (_registry.Commands.All(command => command.Id != tool.Id))
            {
                _registry.Register(new QuickRunCommand(
                    tool.Id,
                    tool.DisplayName,
                    tool.Targets,
                    cancellationToken => ExecuteOnUiThreadAsync(
                        tool.Execute,
                        cancellationToken)));
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (MappingToolQuickRunRegistration tool in _tools)
        {
            _registry.Remove(tool.Id);
        }

        return Task.CompletedTask;
    }

    private Task ExecuteOnUiThreadAsync(
        Func<CancellationToken, Task> execute,
        CancellationToken cancellationToken)
    {
        TaskCompletionSource completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher.Post(() => _ = CompleteAsync());
        return completion.Task;

        async Task CompleteAsync()
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
