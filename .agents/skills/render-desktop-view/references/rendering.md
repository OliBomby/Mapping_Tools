# Desktop view rendering reference

## Authoritative APIs

- Avalonia 12.1 headless setup and frame capture: https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform
- Avalonia testing overview: https://docs.avaloniaui.net/docs/testing/
- Avalonia control-to-image rendering: https://docs.avaloniaui.net/docs/how-to/images/how-to-render-control-to-image
- WPF `RenderTargetBitmap.Render`: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-create-a-bitmap-from-a-visual
- WPF `HwndSource` presentation hosting: https://learn.microsoft.com/en-us/dotnet/api/system.windows.interop.hwndsource?view=windowsdesktop-10.0
- Rider WPF preview architecture: https://blog.jetbrains.com/dotnet/2018/03/29/xaml-preview-tool-window-wpf-rider/

The Avalonia renderer follows the documented 12.1 recipe: configure the application, call `UseSkia()`, use the headless platform with `UseHeadlessDrawing = false`, show a host window, and call `CaptureRenderedFrame()`.

WPF has no corresponding public headless platform. Rider documents that it launches a separate process, renders XAML there, captures a bitmap, and displays that bitmap in the IDE. This repository follows the same isolation model. Its `WinExe` renderer never shows or activates a window. `Window` content is connected to a non-visible `HwndSource` so WPF performs presentation-source-dependent layout and rendering before `RenderTargetBitmap` captures it.

## What a PNG can diagnose

A frame exercises the renderer host's XAML loading, resources, styles,
templates, bindings, measure/arrange, and drawing. It can diagnose isolated
loading or drawing failures, but it is not migration parity or acceptance
evidence and does not prove native integration or interaction behavior.

Compare equal logical dimensions. Keep the environment, fonts, DPI, theme, culture, and data stable before pixel comparisons. Prefer structured visual inspection when WPF and Avalonia rasterize text differently.

## Renderer locations

- `tools/Mapping_Tools.Avalonia.ViewRenderer`: Avalonia 12.1 headless Skia host.
- `tools/Mapping_Tools.Wpf.ViewRenderer`: Windows-only WPF bitmap host.
- `.agents/skills/render-desktop-view/scripts/render-view.ps1`: stable agent entry point.

Add deterministic factories to the renderer instead of weakening production constructors or hiding setup failures.
