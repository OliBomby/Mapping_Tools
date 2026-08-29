# Wave 10 step 43 — Geometry Dashboard core and project models

Status: implemented in the current migration wave. This note covers step 43 only; Windows adapters and Geometry Dashboard UI remain steps 44 and 45.

## Scope and source of truth

The legacy WPF implementation was read as the normative behavioral specification before implementation, including:

- `Mapping_Tools/Views/GeometryDashboard/GeometryDashboardView.xaml` and code-behind
- `Mapping_Tools/Views/GeometryDashboard/GeometryDashboardProjectWindow.xaml` and code-behind
- `Mapping_Tools/Views/GeometryDashboard/GeometryDashboardPreferencesWindow.xaml` and code-behind
- `Mapping_Tools/Views/GeometryDashboard/GeneratorSettingsWindow.xaml` and code-behind
- `Mapping_Tools/Views/GeometryDashboard/GeometryDashboardOverlay.cs`
- `Mapping_Tools/Viewmodels/GeometryDashboardVm.cs`
- `Mapping_Tools/Classes/Tools/GeometryDashboard/CoordinateConverter.cs`
- every source under `Mapping_Tools/Classes/Tools/GeometryDashboard/DataStructure/`
- every source under `Mapping_Tools/Classes/Tools/GeometryDashboard/DataStructure/RelevantObjectGenerators/`
- `Mapping_Tools.Infrastructure.Tests/Projects/GeometryDashboardProjectPersistenceTests.cs` and the Geometry Dashboard fixtures under `Mapping_Tools.Infrastructure.Tests/Fixtures/GeometryDashboard/`

Step 43 moves the neutral geometry graph, all reflection-discovered generators and settings, layer/allocation rules, and project preferences/save-slot models. Windows coordinate formulas are implemented with the step-44 Infrastructure boundary. The Desktop-only `KeepRunning` lifecycle preference is intentionally kept out of the Core preferences model and belongs to the Desktop project model. Step 43 does not move process discovery, editor memory reads, global hotkeys, cursor/window tracking, overlay drawing, WPF commands, or any view.

## Architecture

- `Mapping_Tools.Core/Classes/Tools/GeometryDashboard/` owns geometry primitives, relevant-object ownership and mutation, layer generation, selection predicates, generator settings and calculations, and project state/defaults. Desktop coordinate conversion is intentionally outside Core.
- `Mapping_Tools.Application/GeometryDashboard/` exposes the typed `ProjectDefinition<GeometryDashboardProject>` used by later project workflows and the existing generic project-store/service ports.
- `Mapping_Tools.Infrastructure/Projects/LegacyProjectJsonSerializer.cs` reuses the shared legacy serializer and adds Geometry Dashboard dictionary/object-collection converters for legacy `Type` keys and nested `$type` metadata.
- `Mapping_Tools.Desktop/` remains unchanged in this step. WPF drawing and keyboard commands are not represented in Core.

## Preserved behavior and compatibility

The migration preserves the legacy generator catalog metadata, defaults, reflection method signatures, temporal positioning, deep/sequential pairing, strict duplicate distance, maximum layer count, graph parent/child ownership, relevancy propagation, disposal rules, locked copies, null/empty generator results, slider sampling formulas, line/circle/point calculations, and the WPF project preference defaults.

The project serializer continues to emit the legacy root and nested type names, simple `Mapping Tools` assembly names, indented JSON, omitted nulls, ignored runtime reference loops, legacy `#AARRGGBB` colours, WPF-compatible numeric hotkey values, type-keyed generator settings, and locked-object collections. It accepts the older namespace, assembly-version, and fallback type names used by the checked-in project and locked-object fixtures.

## Explicit platform substitutions

- Configuration-file reads, screen discovery, DPI queries, and window/process coordinates are owned by the Infrastructure coordinate context. Core contains no Geometry Dashboard coordinate converter.
- WPF `Color` is represented by the existing Core `RgbaColour` value type; its serializer keeps the legacy `#AARRGGBB` shape. Rendering dash conversion remains a Desktop concern; the neutral `DashStylesEnum` is retained as persisted data.
- WPF `Hotkey` is represented by the shared neutral Core `HotkeySettings` key/modifier pair whose numeric values match the legacy WPF enums. Global registration and activation callbacks remain step 44/45 behavior.
- WPF `CommandImplementation`, `CollectionView` grouping, `DrawingContext`, Overlay.NET, Process.NET, and editor-reader integration remain outside Core/Application.

## Verification

Focused tests cover legacy Geometry Dashboard project settings and type aliases, locked virtual-object collections, project save/load-slot ownership, generator/layer calculation, and default values:

- `Mapping_Tools.Core.Tests/Classes/Tools/GeometryDashboard/GeometryDashboardDomainTests.cs`
- `Mapping_Tools.Application.Tests/GeometryDashboard/GeometryDashboardContractsTests.cs`
- `Mapping_Tools.Infrastructure.Tests/Projects/GeometryDashboardProjectPersistenceTests.cs`
- `Mapping_Tools.Infrastructure.Tests/Projects/GeometryDashboardLockedObjectsPersistenceTests.cs`

Final verification for this step is recorded in the task handoff after the focused tests, architecture checks, diff check, and affected project builds complete.
