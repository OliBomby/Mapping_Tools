using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using Mapping_Tools.Application.Persistence;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Types;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Desktop;

public class NavigationService {
    private readonly IServiceProvider serviceProvider1;
    private readonly IStateStore stateStore1;

    private static readonly Type acceptableType = typeof(ViewModelBase);
    private static readonly Type mappingToolType = typeof(MappingTool);
    private static readonly Type quickRunType = typeof(IQuickRun);

    public Type[] AllViewTypes { get; }
    public Type[] AllToolTypes { get; }
    public Type[] AllQuickRunTypes { get; }

    public NavigationService(IServiceProvider serviceProvider, IStateStore stateStore) {
        serviceProvider1 = serviceProvider;
        stateStore1 = stateStore;

        AllViewTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => acceptableType.IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false }).ToArray();

        AllToolTypes = AllViewTypes
                .Where(x => mappingToolType.IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false }).ToArray();

        AllQuickRunTypes = AllToolTypes
                .Where(x => quickRunType.IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false }).ToArray();
    }

    public event Action<ViewModelBase>? OnNavigate;

    public async Task NavigateAsync(object viewModel)
    {
        if (viewModel is not ViewModelBase vm)
            throw new ArgumentException("viewModel must be of type ViewModelBase", nameof(viewModel));

        var viewModelSaturated = await Task.Run(async () =>
        {
            if (!TryGetModelType(vm, out var modelType))
                return vm;

            // Load the Model from the state store
            var loadMethod = typeof(IStateStore).GetMethod(nameof(IStateStore.LoadAsync))?.MakeGenericMethod(modelType!)!;
            var loadTask = (Task) loadMethod.Invoke(stateStore1, [modelType!.Name, null])!;
            await loadTask;
            var resultProperty = loadTask.GetType().GetProperty("Result")!;
            object? model = resultProperty.GetValue(loadTask);

            if (model == null)
                return vm;

            var prop = vm.GetType().GetMethod(nameof(IHasModel<ViewModelBase>.SetModel), BindingFlags.Public | BindingFlags.Instance)!;
            prop.Invoke(vm, [model]);

            return vm;
        });

        // UI dispatcher invocation may be required depending on the context from which this method is called.
        await Dispatcher.UIThread.InvokeAsync(() => OnNavigate?.Invoke(viewModelSaturated));
    }

    public async Task NavigateAsync(string name)
    {
        var viewModelType = GetType(name);

        if (viewModelType == null)
            throw new ArgumentException($"No view with the name {name} exists.", nameof(name));

        await NavigateAsync(viewModelType);
    }

    public async Task NavigateAsync(Type viewModelType)
    {
        var viewModel = serviceProvider1.GetRequiredService(viewModelType);
        await NavigateAsync(viewModel);
    }

    public async Task NavigateAsync<T>() where T : ViewModelBase
    {
        var viewModel = serviceProvider1.GetRequiredService<T>();
        await NavigateAsync(viewModel);
    }

    public async Task DisposeCurrentAsync(ViewModelBase? currentViewModel)
    {
        if (currentViewModel is null) return;

        await Task.Run(async () =>
        {
            if (TryGetModelType(currentViewModel, out var modelType))
            {
                // Get the Model from the viewmodel and save it to the state store
                var prop = currentViewModel.GetType().GetMethod(nameof(IHasModel<ViewModelBase>.GetModel), BindingFlags.Public | BindingFlags.Instance)!;
                object model = prop.Invoke(currentViewModel, [])!;

                var saveMethod = typeof(IStateStore).GetMethod(nameof(IStateStore.SaveAsync))?.MakeGenericMethod(modelType!)!;
                await (Task) saveMethod.Invoke(stateStore1, [modelType!.Name, model, null])!;
            }
        });
    }

    public static bool TryGetModelType(object obj, out Type? modelType)
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

    // public static Type[] GetAllQuickRunTypesWithTargets(SmartQuickRunTargets targets) {
    //     return GetAllQuickRunTypes().Where(o => {
    //             var attribute = o.GetCustomAttribute<SmartQuickRunUsageAttribute>();
    //             return attribute != null && attribute.Targets.HasFlag(targets);
    //         })
    //         .ToArray();
    // }

    public bool ViewExists(string name) {
        return AllViewTypes.Any(o => GetName(o) == name);
    }

    public string[] GetNames(Type[] types) {
        return types.Where(o => o.GetField("ToolName") != null)
            .Select(GetName).ToArray();
    }

    public string GetName(Type type) {
        return type.GetField("ToolName") == null ? type.Name : (string)type.GetField("ToolName")!.GetValue(null)!;
    }

    public string GetDescription(Type type) {
        return type.GetField("ToolDescription") == null ? "" : (string)type.GetField("ToolDescription")!.GetValue(null)!;
    }

    public Type? GetType(string name) {
        return AllViewTypes.FirstOrDefault(o => GetName(o) == name);
    }
}