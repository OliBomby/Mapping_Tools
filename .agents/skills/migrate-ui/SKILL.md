---
name: migrate-ui
description: Use when migrating a view from the legacy WPF project to the new Avalonia-based project.
---

# Migrate UI

How to migrate a view from the legacy WPF project to the new Avalonia project.

## Documentation gate

Before writing or changing any Avalonia C# API, AXAML, binding, style, control, routed event, property, or platform service, read [references/avalonia-12.1.md](references/avalonia-12.1.md) completely and consult the linked official documentation online. Treat this as a mandatory pre-edit gate, even for familiar APIs.

For any UI-bearing migration, also read
[references/control-parity.md](references/control-parity.md) completely before
choosing controls or styles. Treat source, semantic-control, and interaction
parity as one acceptance gate.

Use only official Avalonia documentation, the Avalonia 12.1.0 tagged source/release, exact-version NuGet metadata, official CommunityToolkit.Mvvm documentation, and the Material.Avalonia repository as authorities. Do not use WPF knowledge or Avalonia 11 examples as evidence that an API exists. When current documentation covers a later release, verify the API against the 12.1.0 tag or package before using it.

## Workflow

1. Read `docs/avalonia-migration/feature-dependency-graph.md` and identify the current wave's scope. Do not report behavior assigned to a later wave as a current migration defect.
2. Inspect the selected WPF XAML, code-behind, view model, converters, custom controls, and services. Trace every binding, converter, tooltip, context-menu item, event handler, command, completion/error branch, and static dependency such as `MainWindow.AppWindow`, dialogs, dispatcher calls, clipboard, cursor, keyboard hooks, and settings singletons.
3. Record observable behavior and establish focused tests before moving logic. Include success, validation failure, cancellation, error behavior, hover, focus, checked/selected states, resizing, dragging, keyboard access, open menus, populated data, and empty data whenever relevant.
4. Classify code into domain rules, application orchestration, infrastructure, and presentation. Keep only rendering, focus, pointer gestures, animation, and other genuinely visual behavior in a view.
5. Extract the smallest reusable slice. Introduce interfaces for file/folder pickers, notifications, clipboard, dispatching, window ownership, or other UI/OS effects. Preserve the WPF behavior through WPF-side adapters where necessary.
6. Confirm the extracted Core/Application code contains no frontend types with searches such as `rg "System\\.Windows|System\\.Windows\\.Forms|Avalonia|CommunityToolkit\\.Mvvm" Mapping_Tools.Core Mapping_Tools.Application`.
7. Implement a CommunityToolkit.Mvvm view model in `Mapping_Tools.Desktop/ViewModels` using the view-model implementation standard below.
8. Implement the Avalonia view in `Mapping_Tools.Desktop/Views` using the view implementation standard below.
9. Register the feature in the Avalonia shell using the smallest navigation change required. Do not remove or redirect the WPF feature yet.
10. Review the WPF-to-Avalonia source diff, build both affected frontends, and run focused behavior tests. Do not use the PNG renderer as migration acceptance evidence.
11. Report migrated behavior, later-wave scope, platform limitations, tests run, and the exact Avalonia 12.1 documentation pages consulted. Behavior in the current wave may not be deferred while declaring the migration complete.

## View-model implementation standard

- Prefer small, feature-specific `partial` view models based on CommunityToolkit.Mvvm's `ObservableObject` or `ObservableValidator`.
- Use `[ObservableProperty]` for bindable mutable state whenever its generated equality, accessibility, and change-hook behavior preserve the feature contract. Use generated partial-property declarations when a non-public setter or XML documentation must remain explicit.
- Put DataAnnotations validation attributes and `[NotifyDataErrorInfo]` on generated properties in `ObservableValidator` types. Keep validation in the attributes rather than custom logic in the viewmodel.
- Use `[NotifyPropertyChangedFor]` and `[NotifyCanExecuteChangedFor]` for static dependent notifications instead of manual `OnPropertyChanged` calls.
- Use `[RelayCommand]` for synchronous and asynchronous view-model actions when the generated command has the required execution and cancellation semantics.
- Keep a manual property when it normalizes input, directly adapts a non-observable model without duplicate state, or requires setter ordering that generator hooks cannot preserve clearly.

## View implementation standard

- Start from the original WPF XAML and keep the smallest possible structural diff. Preserve element order, nesting, grid definitions, measurements, margins, copy, tooltips, bindings, and visibility rules unless an Avalonia substitution requires a documented change.
- Map components in the WPF source to Avalonia components using the `control-parity` document.
- Use bindings with converters and parameters matching the original WPF source.
- Prefer compiled bindings and an explicit `x:DataType`.
- Use `ToolViewHeader`, `ToolRunButton`, and `ToolProgressBar` for the corresponding legacy tool elements. Reuse must not change their WPF layout or behavior.
- Preserve shell-owned scrolling. Add a view-owned scroller only when one exists in the WPF view.
- Use Material.Avalonia's semantic dynamic resources.
- Keep custom-control styles in the control AXAML or a co-located control-owned style file. Keep view-only and shell-only styles in their respective AXAML files.
- Put application-wide Material compatibility overrides in focused dictionaries under `Mapping_Tools.Desktop/Resources/Styles` and compose them from `App.axaml`.
- Do not add migration-time product changes such as cancellation, new validation limits, alternate picker behavior, or rewritten completion messages without explicit approval.

## Completion criteria

Complete a feature migration only when:

- Both the WPF application and Avalonia desktop project build.
- Extracted logic has focused automated coverage or a documented reason coverage is impractical.
- Core and Application contain no WPF, WinForms, Avalonia, or CommunityToolkit.Mvvm references.
- The Avalonia view uses APIs verified for 12.1.0 and compiled bindings where applicable.
- The WPF and Avalonia source diff contains only required platform translations, approved shared-control substitutions, and documented exceptions.
- Each legacy interactive element is represented by the correct Avalonia
  control and preserves its selection, resizing, dragging, menu, hover,
  focus, checked, and keyboard behavior where applicable.
- Every behavior assigned to the current dependency-graph wave is implemented; later-wave work is identified as scope rather than a defect.
- Focused tests cover the migrated behavior. Renderer screenshots are not completion evidence.
