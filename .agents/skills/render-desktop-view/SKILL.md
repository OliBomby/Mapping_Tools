---
name: render-desktop-view
description: Render Mapping Tools WPF or Avalonia views to deterministic PNG files and inspect them for migration visual parity. Use when an agent needs to see a desktop view, compare a legacy WPF view with its Avalonia port, establish a visual baseline, diagnose layout or styling differences, or add a render scenario for a view that cannot be constructed safely in isolation.
---

# Render Desktop View

Render a view at a fixed size without launching the full application. Avalonia uses its headless platform. WPF uses an isolated, never-visible designer host because WPF has no equivalent supported headless platform. Treat the PNG as evidence for static visual parity, not as proof of native-window, input, overlay, animation, or dialog behavior.

## Required workflow

1. Read [references/rendering.md](references/rendering.md) before adding or changing renderer code.
2. List available types when the exact class name is uncertain:

   ```powershell
   .\.agents\skills\render-desktop-view\scripts\render-view.ps1 -Framework avalonia -List
   .\.agents\skills\render-desktop-view\scripts\render-view.ps1 -Framework wpf -List
   ```

3. Render both implementations with the same width and height:

   ```powershell
   .\.agents\skills\render-desktop-view\scripts\render-view.ps1 -Framework wpf -View MainWindow -Output artifacts\view-renders\legacy.png -Width 1280 -Height 800
   .\.agents\skills\render-desktop-view\scripts\render-view.ps1 -Framework avalonia -View MainWindow -Output artifacts\view-renders\avalonia.png -Width 1280 -Height 800
   ```

4. Open every PNG with the local image-viewing tool. Do not claim parity from a successful render alone.
5. Compare hierarchy, alignment, spacing, typography, colors, wrapping, clipping, scrolling, enabled state, empty state, and focus cues.
6. Correct visible differences or explicitly record intentional design changes.

## Deterministic render scenarios

The generic renderer can construct parameterless controls. If construction reads user settings, starts network or editor integration, opens a native dialog, or depends on `MainWindow` globals, stop and add an explicit factory in the relevant renderer `Program.cs`. A scenario must:

- use fixed in-memory data and stable text;
- avoid real user profiles, app data, beatmaps, network calls, clocks, and randomness;
- set the view model before showing the host window;
- leave production views free of renderer-only conditionals;
- use the same semantic state on WPF and Avalonia sides.

Name factories by view and state, for example `PreferencesView.Empty`. Add a `--scenario` option when a view has multiple material states.

## Avalonia requirements

Before authoring or modifying Avalonia rendering code, verify the API against the official Avalonia 12.1 documentation linked in `references/rendering.md`. Keep `Avalonia.Headless` pinned to `12.1.0` and use Skia with `UseHeadlessDrawing = false`; the lightweight headless drawing implementation cannot produce useful screenshots.

## WPF designer host

Do not describe WPF rendering as truly headless. The WPF renderer is a separate `WinExe` process. Ordinary controls render directly; a `Window` is initialized without runtime services and its content is attached to an `HwndSource` that has no visible window style. Never call `Window.Show()`, add `WS_VISIBLE`, or activate the host. Any visible flash is a renderer regression.

## Limits

Use a real desktop run or automation test for native file dialogs, global hotkeys, WinForms hosts, editor-memory integration, audio devices, overlays, native window chrome, OS font fallback, DPI/multi-monitor behavior, drag-and-drop, and timing-sensitive animation. Image rendering remains the default parity check for ordinary control trees.
