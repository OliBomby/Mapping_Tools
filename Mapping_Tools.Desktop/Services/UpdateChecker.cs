using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Services;

public class UpdateChecker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        // TODO: Implement update checking logic
    }

    // private async Task Update(bool allowSkip = true, bool notifyUser = false) {
    //     try {
    //         var assetNamePattern = Environment.Is64BitProcess ? "release_x64.zip" : "release.zip";
    //         updateManager = new UpdateManager("OliBomby", "Mapping_Tools", assetNamePattern);
    //         var hasUpdate = await updateManager.FetchUpdateAsync();
    //
    //         if (!hasUpdate) {
    //             if (notifyUser)
    //                 MessageQueue.Enqueue("No new versions available.");
    //             return;
    //         }
    //
    //         // Check if this version is newer than the version we skip
    //         var skipVersion = SettingsManager.Settings.SkipVersion;
    //         if (allowSkip && skipVersion != null && !(updateManager.UpdatesResult.LastVersion > skipVersion)) {
    //             if (notifyUser)
    //                 MessageQueue.Enqueue($"Version {updateManager.UpdatesResult.LastVersion} skipped because of user config.");
    //             return;
    //         }
    //
    //         Dispatcher.Invoke(() => {
    //             updaterWindow = new UpdaterWindow(updateManager.Progress) {
    //                 ShowActivated = true
    //             };
    //
    //             updaterWindow.Closed += disposeUpdateManager;
    //
    //             updaterWindow.ActionSelected += async (_, action) => {
    //                 switch (action) {
    //                     case UpdateAction.Restart:
    //                         updateAfterClose = false;
    //                         await updateManager.DownloadUpdateAsync();
    //                         updateManager.RestartAfterUpdate = true;
    //                         updateManager.StartUpdateProcess();
    //
    //                         updaterWindow.Close();
    //                         Close();
    //                         break;
    //
    //                     case UpdateAction.Wait:
    //                         updateAfterClose = true;
    //                         updateManager.RestartAfterUpdate = false;
    //                         downloadUpdateTask = updateManager.DownloadUpdateAsync();
    //
    //                         // Preserve the update manager so it can be used later to download the update
    //                         updaterWindow.Closed -= disposeUpdateManager;
    //
    //                         updaterWindow.Close();
    //                         break;
    //
    //                     case UpdateAction.Skip:
    //                         updateAfterClose = false;
    //                         // Update the skip version so we skip this version in the future
    //                         SettingsManager.Settings.SkipVersion = updateManager.UpdatesResult.LastVersion;
    //                         updaterWindow.Close();
    //                         break;
    //
    //                     default:
    //                         updaterWindow.Close();
    //                         break;
    //                 }
    //             };
    //
    //             void disposeUpdateManager(object o, EventArgs eventArgs) {
    //                 updateManager.Dispose();
    //                 updateManager = null;
    //             }
    //
    //             updaterWindow.Show();
    //         });
    //     } catch (Exception e) {
    //         MessageBox.Show("UPDATER_EXCEPTION: " + e.Message);
    //         if (notifyUser) {
    //             MessageQueue.Enqueue("Error fetching update: " + e.Message);
    //         }
    //     }
    // }
    //
    // private void AutoUpdateOnClose() {
    //     if (!updateAfterClose || updateManager == null) {
    //         return;
    //     }
    //
    //     if (downloadUpdateTask is { IsCompletedSuccessfully: true }) {
    //         updateManager.StartUpdateProcess();
    //         return;
    //     }
    //
    //     if (downloadUpdateTask is null or { IsFaulted: true }) {
    //         downloadUpdateTask = updateManager.DownloadUpdateAsync();
    //     }
    //
    //     updaterWindow = new UpdaterWindow(updateManager.Progress, true) {
    //         ShowActivated = true
    //     };
    //
    //     _ = Dispatcher.Invoke(async () => {
    //         await downloadUpdateTask;
    //         updateManager.StartUpdateProcess();
    //         updaterWindow.Close();
    //     });
    //
    //     updaterWindow.ShowDialog();
    // }
}
