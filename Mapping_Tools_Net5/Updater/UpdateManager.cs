using Onova;
using Onova.Models;
using Onova.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mapping_Tools_Net5.Updater {

    public interface IUpdateManager {
        Progress<double> Progress { get; }
        IPackageResolver PackageResolver { get; }

        event EventHandler IsReadyToUpdate;

        void Release();

        void Complete(bool doUpdate = true, bool restartAfterUpdate = true);

        /// <exception cref="Onova.Exceptions.LockFileNotAcquiredException"></exception>
        /// <exception cref="Onova.Exceptions.UpdaterAlreadyLaunchedException"></exception>
        Task<bool> FetchUpdateAsync();

        /// <exception cref="Onova.Exceptions.LockFileNotAcquiredException"></exception>
        /// <exception cref="Onova.Exceptions.UpdaterAlreadyLaunchedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        Task StartUpdateProcessAsync();
    }

    public class UpdateManager :IUpdateManager {
        private bool _doUpdate = false;
        private bool _restartAfterUpdate = false;
        private readonly EventWaitHandle _lockHandler = new(false, EventResetMode.ManualReset);
        private readonly object _lock = new();

        public Progress<double> Progress { get; }
        public IPackageResolver PackageResolver { get; }
        public CheckForUpdatesResult UpdatesResult { get; private set; }

        public event EventHandler IsReadyToUpdate;

        public UpdateManager(IPackageResolver packageResolver) {
            PackageResolver = packageResolver;
            Progress = new Progress<double>();
        }

        public UpdateManager(string repoOwner, string repoName, string assetNamePattern) {
            PackageResolver = new GithubPackageResolver(repoOwner, repoName, assetNamePattern);
            Progress = new Progress<double>();
        }

        public void Release() {
            _lockHandler.Set();
        }

        public void Complete(bool doUpdate = true, bool restartAfterUpdate = true) {
            lock( _lock ) {
                _doUpdate = doUpdate;
                _restartAfterUpdate = restartAfterUpdate;
            }

            _lockHandler.Set();
        }

        /// <exception cref="Onova.Exceptions.LockFileNotAcquiredException"></exception>
        /// <exception cref="Onova.Exceptions.UpdaterAlreadyLaunchedException"></exception>
        public async Task<bool> FetchUpdateAsync() {
            using var manager = new Onova.UpdateManager(PackageResolver, new ZipPackageExtractor());

            UpdatesResult = await manager.CheckForUpdatesAsync();

            return UpdatesResult.CanUpdate;
        }

        /// <exception cref="Onova.Exceptions.LockFileNotAcquiredException"></exception>
        /// <exception cref="Onova.Exceptions.UpdaterAlreadyLaunchedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task StartUpdateProcessAsync() {
            if( UpdatesResult == null ) {
                throw new InvalidOperationException("Do not call this method before fetching updates!");
            }

            if( !UpdatesResult.CanUpdate ) {
                throw new InvalidOperationException("Do not call this method if there are no updates!");
            }

            using var manager = new Onova.UpdateManager(PackageResolver, new ZipPackageExtractor());

            await manager.PrepareUpdateAsync(UpdatesResult.LastVersion, Progress);

            IsReadyToUpdate?.Invoke(this, null);

            _lockHandler.WaitOne();

            lock( _lock ) {
                if( _doUpdate ) {
                    manager.LaunchUpdater(UpdatesResult.LastVersion, _restartAfterUpdate);
                }
            }
        }
    }
}