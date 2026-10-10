using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.SliderCompletionator.ViewModels;

namespace Mapping_Tools.Desktop.Tests.Tools.SliderCompletionator.ViewModels;

internal static class SliderCompletionatorViewModelTestFactory
{
    internal static SliderCompletionatorViewModel Create(
        ISliderCompletionatorService service,
        TestBeatmapWorkspace? workspace = null,
        DesktopApplicationSettings? settings = null)
    {
        return new SliderCompletionatorViewModel(
            service,
            new ToolExecutionService(new UserNotificationService(), TimeProvider.System),
            workspace ?? new TestBeatmapWorkspace(),
            settings ?? new DesktopApplicationSettings());
    }
}
