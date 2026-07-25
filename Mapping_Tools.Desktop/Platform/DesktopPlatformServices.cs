using Mapping_Tools.ApplicationServices.Platform;

namespace Mapping_Tools.Desktop.Platform;

public sealed record DesktopPlatformServices(
    IFilePicker FilePicker,
    IClipboardService Clipboard,
    IPlatformLauncher Launcher,
    IFileRevealService FileReveal,
    IApplicationDirectories Directories);
