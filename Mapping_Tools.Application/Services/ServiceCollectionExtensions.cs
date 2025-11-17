using Mapping_Tools.Application.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Mapping_Tools.Application.Services;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddApplicationServices(this IServiceCollection s) {
        s.AddSingleton<IPersistenceCoordinator, PersistenceCoordinator>();
        s.AddSingleton<INotificationService, NotificationService>();
        s.AddSingleton<IAppLifecycle, AppLifecycle>();
        return s;
    }
}