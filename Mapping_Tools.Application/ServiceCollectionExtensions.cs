using Mapping_Tools.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Application;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddApplicationServices(this IServiceCollection s) {
        s.AddSingleton<IPersistenceCoordinator, PersistenceCoordinator>();
        s.AddSingleton<IAppLifecycle, AppLifecycle>();
        return s;
    }
}