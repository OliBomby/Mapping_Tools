using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

public sealed class MapCleanerQuickRunHostedService : IHostedService
{
    private readonly IQuickRunCommandRegistry _registry;
    private readonly MapCleanerViewModel _viewModel;
    public MapCleanerQuickRunHostedService(IQuickRunCommandRegistry registry, MapCleanerViewModel viewModel) =>
        (_registry, _viewModel) = (registry, viewModel);
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_registry.Commands.All(command => command.Id != MapCleanerViewModel.OperationId))
            _registry.Register(new QuickRunCommand(MapCleanerViewModel.OperationId, "Map Cleaner", QuickRunTargets.Always, _viewModel.RunQuickAsync));
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _registry.Remove(MapCleanerViewModel.OperationId);
        return Task.CompletedTask;
    }
}
