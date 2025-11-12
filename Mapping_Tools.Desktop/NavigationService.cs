using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using Mapping_Tools.Application.Persistence;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop;

public class NavigationService(IServiceProvider serviceProvider, IStateStore stateStore)
{
    public event Action<ViewModelBase>? OnNavigate;

    public async Task NavigateAsync<T>() where T : ViewModelBase
    {
        var viewModel = await Task.Run(async () =>
        {
            // Mouse pointer load animation
            
            
            var viewModel = serviceProvider.GetRequiredService<T>();

            if (!TryGetModelType(viewModel, out var modelType))
                return viewModel;

            // Load the Model from the state store
            var loadMethod = typeof(IStateStore).GetMethod(nameof(IStateStore.LoadAsync))?.MakeGenericMethod(modelType!)!;
            var loadTask = (Task) loadMethod.Invoke(stateStore, [modelType!.Name, null])!;
            await loadTask;
            var resultProperty = loadTask.GetType().GetProperty("Result")!;
            object? model = resultProperty.GetValue(loadTask);

            if (model == null)
                return viewModel;

            var prop = viewModel.GetType().GetMethod(nameof(IHasModel<ViewModelBase>.SetModel), BindingFlags.Public | BindingFlags.Instance)!;
            prop.Invoke(viewModel, [model]);

            return viewModel;
        });

        // UI dispatcher invocation may be required depending on the context from which this method is called.
        await Dispatcher.UIThread.InvokeAsync(() => OnNavigate?.Invoke(viewModel));
    }

    public async Task DisposeCurrentAsync(ViewModelBase? currentViewModel)
    {
        if (currentViewModel is null) return;

        if (TryGetModelType(currentViewModel, out var modelType))
        {
            // Get the Model from the viewmodel and save it to the state store
            var prop = currentViewModel.GetType().GetMethod(nameof(IHasModel<ViewModelBase>.GetModel), BindingFlags.Public | BindingFlags.Instance)!;
            object model = prop.Invoke(currentViewModel, [])!;
            
            var saveMethod = typeof(IStateStore).GetMethod(nameof(IStateStore.SaveAsync))?.MakeGenericMethod(modelType!)!;
            await (Task) saveMethod.Invoke(stateStore, [modelType!.Name, model, null])!;
        }
    }

    private static bool TryGetModelType(object obj, out Type? modelType)
    {
        modelType = null;

        var type = obj.GetType();

        var hasModelInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IHasModel<>));

        if (hasModelInterface is null) return false;

        modelType = hasModelInterface.GetGenericArguments()[0];
        return true;
    }
}