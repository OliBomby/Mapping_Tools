using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Classes.SystemTools {
    internal sealed class LegacyBeatmapFilePicker : IFilePicker {
        public bool CanOpenFiles => true;

        public bool CanSaveFiles => false;

        public bool CanPickFolders => false;

        public Task<IReadOnlyList<string>> PickOpenFilesAsync(
            OpenFilePickerRequest request,
            CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            var dialog = new OpenFileDialog {
                Title = request.Title ?? "",
                InitialDirectory = request.SuggestedStartLocation ?? "",
                Filter = string.Join(
                    "|",
                    request.Filters.Select(
                        filter =>
                            $"{filter.Name} ({string.Join(';', filter.Patterns)})|" +
                            string.Join(';', filter.Patterns))),
                CheckFileExists = true,
                Multiselect = request.AllowMultiple,
                RestoreDirectory = true
            };

            bool? accepted = dialog.ShowDialog();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(
                accepted == true ? dialog.FileNames : []);
        }

        public Task<string> PickSaveFileAsync(
            SaveFilePickerRequest request,
            CancellationToken cancellationToken = default) {
            throw new PlatformNotSupportedException(
                "The legacy beatmap workspace adapter only presents open-file dialogs.");
        }

        public Task<IReadOnlyList<string>> PickFoldersAsync(
            OpenFolderPickerRequest request,
            CancellationToken cancellationToken = default) {
            throw new PlatformNotSupportedException(
                "The legacy beatmap workspace adapter does not present folder dialogs.");
        }
    }

    internal sealed class LegacyCurrentBeatmapLocator : ICurrentBeatmapLocator {
        public Task<string> FindCurrentBeatmapAsync(
            CancellationToken cancellationToken = default) {
            return Task.Run(IOHelper.GetCurrentBeatmap, cancellationToken);
        }
    }
}
