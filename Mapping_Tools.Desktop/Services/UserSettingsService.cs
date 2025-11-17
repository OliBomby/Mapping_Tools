using Mapping_Tools.Application;
using Mapping_Tools.Application.Persistence;
using Mapping_Tools.Desktop.Models;

namespace Mapping_Tools.Desktop.Services;

public class UserSettingsService
{
    public UserSettings Settings { get; }
    
    public UserSettingsService(IStateStore stateStore, IAppLifecycle appLifecycle)
    {
        Settings = stateStore.LoadAsync<UserSettings>("user_settings").GetAwaiter().GetResult() ?? new UserSettings();
        appLifecycle.UICleanup.Register(() => stateStore.SaveAsync("user_settings", Settings).GetAwaiter().GetResult());
    }
}