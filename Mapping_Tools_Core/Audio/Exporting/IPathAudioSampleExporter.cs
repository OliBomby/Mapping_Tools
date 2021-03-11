namespace Mapping_Tools_Core.Audio.Exporting {
    /// <summary>
    /// Audio sample exporter that exports to a path in the file system.
    /// </summary>
    public interface IPathAudioSampleExporter : IAudioSampleExporter {
        /// <summary>
        /// The path to the file to copy if <see cref="CanCopyPaste"/>.
        /// </summary>
        string CopyPath { get; set; }

        /// <summary>
        /// Whether the sample can be exported by copying the file at <see cref="CopyPath"/>.
        /// Default to true.
        /// Only copy paste if <see cref="CopyPath"/> is valid.
        /// </summary>
        bool CanCopyPaste { get; set; }

        /// <summary>
        /// The path to the folder to export to.
        /// </summary>
        string ExportFolder { get; set; }

        /// <summary>
        /// The filename of the exported sample without extension.
        /// </summary>
        string ExportName { get; set; }
    }
}