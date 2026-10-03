using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing.Models;

namespace Mapping_Tools.Application.Localization;

/// <summary>Provides localized, user-safe summaries for expected application failures.</summary>
public static class ApplicationExceptionText
{
    /// <summary>Gets a localized summary for an exception without exposing its diagnostic message.</summary>
    /// <param name="exception">The original failure, retained separately for technical details.</param>
    /// <param name="fallbackSummary">An optional translated operation-specific message for otherwise unexpected failures.</param>
    /// <returns>A localized explanation selected from the exception category.</returns>
    public static string GetSummary(Exception exception, string? fallbackSummary = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            LiveBeatmapUnavailableException => ApplicationStrings.Exception_LiveEditorUnavailable,
            BeatmapBackupIncompatibleException => ApplicationStrings.Exception_BackupBeatmapMismatch,
            FileNotFoundException => ApplicationStrings.Exception_FileMissing,
            DirectoryNotFoundException => ApplicationStrings.Exception_FileMissing,
            UnauthorizedAccessException => ApplicationStrings.Exception_AccessDenied,
            ArgumentException => ApplicationStrings.Exception_InvalidInput,
            _ => fallbackSummary ?? ApplicationStrings.Exception_UnexpectedFailure,
        };
    }
}
