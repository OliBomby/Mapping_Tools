using Squirrel;
using System;
using System.Threading.Tasks;

namespace Mapping_Tools_Net5_Updater {

    public interface IUpdater {

        Task<bool> TryUpdating();
    }

    public class Updater :IUpdater {

        public async Task<bool> TryUpdating() {
            try {
                using( var mgr = UpdateManager.GitHubUpdateManager("https://github.com/olibomby/mapping_tools") ) {
                    await mgr.Result.UpdateApp();
                }

                return true;
            }
            catch( Exception ) {
                return false;
            }
        }
    }
}