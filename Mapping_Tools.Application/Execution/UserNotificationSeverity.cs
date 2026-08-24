namespace Mapping_Tools.Application.Execution;

/// <summary>
///     Classifies user-facing messages without prescribing a snackbar, dialog,
///     status bar, or other frontend presentation.
/// </summary>
public enum UserNotificationSeverity
{
    /// <summary>
    ///     Communicates neutral state that does not require corrective action.
    /// </summary>
    Information,

    /// <summary>
    ///     Confirms that the requested operation completed as intended.
    /// </summary>
    Success,

    /// <summary>
    ///     Reports a recoverable condition that deserves attention but did not crash the operation.
    /// </summary>
    Warning,

    /// <summary>
    ///     Reports that an operation failed and may include its diagnostic exception.
    /// </summary>
    Error,
}

