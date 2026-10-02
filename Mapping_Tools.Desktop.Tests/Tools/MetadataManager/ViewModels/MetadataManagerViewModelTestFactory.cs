using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.MetadataManager.ViewModels;

namespace Mapping_Tools.Desktop.Tests.Tools.MetadataManager.ViewModels;

internal static class MetadataManagerViewModelTestFactory
{
    internal static MetadataManagerViewModel Create(
        RecordingMetadataManagerService? metadataManager = null,
        TestFilePicker? filePicker = null,
        TestBeatmapWorkspace? workspace = null)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(notifications, TimeProvider.System);

        return new MetadataManagerViewModel(
            metadataManager ?? new RecordingMetadataManagerService(),
            execution,
            filePicker ?? new TestFilePicker(),
            new TestCurrentBeatmapDialogService(),
            workspace ?? new TestBeatmapWorkspace(),
            notifications,
            new TestApplicationDirectories());
    }
}
