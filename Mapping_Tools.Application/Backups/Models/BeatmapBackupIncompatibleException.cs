namespace Mapping_Tools.Application.Backups.Models;

/// <summary>
///     Prevents a restore from silently replacing a different difficulty or mapset.
/// </summary>
public sealed class BeatmapBackupIncompatibleException : Exception
{
    /// <summary>
    ///     Creates an error that presents both metadata-derived filenames for an
    ///     informed explicit-override decision.
    /// </summary>
    /// <param name="backupFileName">The canonical filename derived from backup metadata.</param>
    /// <param name="destinationFileName">The canonical filename derived from destination metadata.</param>
    public BeatmapBackupIncompatibleException(
        string backupFileName,
        string destinationFileName)
        : base(
            "The backup and destination contain different beatmap metadata." + Environment.NewLine + backupFileName + Environment.NewLine + destinationFileName)
    {
        BackupFileName = backupFileName;
        DestinationFileName = destinationFileName;
    }

    /// <summary>
    ///     Exposes the filename implied by the backup's metadata, not its timestamped storage name.
    /// </summary>
    public string BackupFileName { get; }

    /// <summary>
    ///     Exposes the filename implied by the destination's current metadata.
    /// </summary>
    public string DestinationFileName { get; }
}
