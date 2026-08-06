using Mapping_Tools.Application.QuickRun;
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

    public MappingToolQuickRunHostedService(
        IQuickRunCommandRegistry registry,
        IEnumerable<MappingToolQuickRunRegistration> tools)
    {
        _registry = registry;
        _tools = tools.ToArray();
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
                    tool.Execute));
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
}
