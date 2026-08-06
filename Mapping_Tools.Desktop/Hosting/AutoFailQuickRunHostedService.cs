using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

/// <summary>Publishes Auto-fail Detector to QuickRun before preferences inspect the catalog.</summary>
public sealed class AutoFailQuickRunHostedService : IHostedService
{
    private readonly IQuickRunCommandRegistry _registry;
    private readonly AutoFailDetectorViewModel _viewModel;

    public AutoFailQuickRunHostedService(
        IQuickRunCommandRegistry registry,
        AutoFailDetectorViewModel viewModel)
    {
        _registry = registry;
        _viewModel = viewModel;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_registry.Commands.All(command => command.Id != AutoFailDetectorViewModel.OperationId))
        {
            _registry.Register(new QuickRunCommand(
                AutoFailDetectorViewModel.OperationId,
                "Auto-fail Detector",
                QuickRunTargets.Always,
                _viewModel.RunQuickAsync));
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _registry.Remove(AutoFailDetectorViewModel.OperationId);
        return Task.CompletedTask;
    }
}
