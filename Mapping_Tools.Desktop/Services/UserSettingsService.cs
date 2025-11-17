using System;
using System.Globalization;
using Mapping_Tools.Application.Types;
using Mapping_Tools.Desktop.Models;

namespace Mapping_Tools.Desktop.Services;

public class UserSettingsService
{
    public UserSettings Settings { get; }

    public UserSettingsService(IStateStore stateStore, IAppLifecycle appLifecycle, INotificationService notificationService)
    {
        try
        {
            throw new Exception("test");
            Settings = stateStore.LoadAsync<UserSettings>("user_settings").GetAwaiter().GetResult() ?? new UserSettings();
        } catch (Exception e)
        {
            Settings = new UserSettings();
            
            notificationService.AddNotification("Error loading user settings",
                "Failed to load user settings, default settings will be used. Error: " + e.Message,
                NotificationType.Error);
        }

        appLifecycle.UICleanup.Register(() => stateStore.SaveAsync("user_settings", Settings).GetAwaiter().GetResult());
    }

    public void SetCurrentBeatmaps(string[] paths)
    {
        Settings.CurrentBeatmaps = paths;
        AddRecentMap(paths, DateTime.Now);

        // if (maps.Any(o => !File.Exists(o)))
        // {
        //     var model = new SnackbarModel("It seems like one of the selected beatmaps does not exist. Please re-select the file with 'File > Open beatmap'.",
        //         TimeSpan.FromSeconds(10));
        //     SnackbarHost.Post(model, "MainSnackbar", DispatcherPriority.Normal);
        // }
    }

    private void AddRecentMap(string[] paths, DateTime date)
    {
        foreach (var path in paths)
        {
            Settings.RecentMaps.RemoveAll(o => o[0] == path);
            if (Settings.RecentMaps.Count >= 20)
            {
                Settings.RecentMaps.RemoveAt(Settings.RecentMaps.Count - 1);
            }

            Settings.RecentMaps.Insert(0, [path, date.ToString(CultureInfo.CurrentCulture)]);
        }
    }
}