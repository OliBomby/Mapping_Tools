---
name: create-mapping-tool
description: Use when creating a new mapping tool feature.
---

# Create a Mapping Tool

Use for a new built-in or external tool. Follow the DDD boundaries and use
`SliderCompletionator` as the reference implementation.

## Required

- Put pure calculations/models in `Mapping_Tools.Core`; put options, results,
  contracts, and orchestration services in `Mapping_Tools.Application`.
  Use `IBeatmapEditingGateway` for beatmap I/O. Add Infrastructure only for a
  new platform or external-system adapter.
- Add a unique `ToolDefinition` and a parameterless
  `[MappingToolDefinition]`/`IMappingToolDefinition` registration exposing the
  view-model, Avalonia view, order, scrollbar policies, and DI registrations.
- Add a desktop view model. For one-shot tools, derive from
  `SingleRunToolViewModel` and run through `IToolExecutionService`. The view
  normally contains `ToolViewHeader`, a run button, a progress bar, and an author footer.
- Add focused tests in mirrored Core/Application/Desktop test folders.
- Files belonging to a tool go in the `Tools/<toolname>` subfolder in each respective project.

## Optional

- `IQuickRun` plus `ToolDefinition.QuickRunTargets` for a useful current-editor
  shortcut.
- `IShellProjectFeature<T>` plus a project model/`ProjectDefinition<T>` for
  saved or autosaved settings. Add a custom `ToolConfigSchema` only when
  migrations or a distinct persisted schema are needed.
- `IBeatmapWorkspace` for shell selection, picking, and recent paths; custom
  dialogs, previews, or shell interfaces only when needed.

## Defaults

- Normal runs use `IBeatmapWorkspace.SelectedPaths`. QuickRun and any operation
  needing the actual open editor use `ICurrentBeatmapLocator`; never infer the
  live map from shell selection.
- Open external beatmaps with `LiveBeatmapPreference.PreferLive`: matching live
  state wins and unavailable/unreadable/non-matching state falls back to disk.
  For selected hitobjects or editor time, locate the exact path using `ICurrentBeatmapLocator`
  and use `RequireLive`; fail rather than silently using stale disk state.
- Save through `IBeatmapEditingGateway`. QuickRun returns
  `ToolExecutionOutput<T>(..., reloadEditor: true)`; normal runs use `false`.
- Use `ToolExecutionRequest<T>`, propagate cancellation, and report normalized
  `IProgress<double>` from `0` to `1` per map/phase. Bridge it with
  `context.ReportProgress(...)` and `CreateProgress()`.
