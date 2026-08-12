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
choosing controls or styles. Treat semantic control parity, interaction parity,
and visual parity as one acceptance gate.

Use only official Avalonia documentation, the Avalonia 12.1.0 tagged source/release, exact-version NuGet metadata, official CommunityToolkit.Mvvm documentation, and the Material.Avalonia repository as authorities. Do not use WPF knowledge or Avalonia 11 examples as evidence that an API exists. When current documentation covers a later release, verify the API against the 12.1.0 tag or package before using it.

## Workflow

1. Inspect the selected WPF XAML, code-behind, view model, converters, custom controls, and services. Trace every static dependency such as `MainWindow.AppWindow`, dialogs, dispatcher calls, clipboard, cursor, keyboard hooks, and settings singletons.
2. Record observable behavior and establish focused tests before moving logic. Include success, validation failure, cancellation, error behavior, hover, focus, checked/selected states, resizing, dragging, keyboard access, open menus, populated data, and empty data whenever relevant.
3. Classify code into domain rules, application orchestration, infrastructure, and presentation. Keep only rendering, focus, pointer gestures, animation, and other genuinely visual behavior in a view.
4. Extract the smallest reusable slice. Introduce interfaces for file/folder pickers, notifications, clipboard, dispatching, window ownership, or other UI/OS effects. Preserve the WPF behavior through WPF-side adapters where necessary.
5. Confirm the extracted Core/Application code contains no frontend types with searches such as `rg "System\\.Windows|System\\.Windows\\.Forms|Avalonia|CommunityToolkit\\.Mvvm" Mapping_Tools.Core Mapping_Tools.Application`.
6. Implement a CommunityToolkit.Mvvm view model in `Mapping_Tools.Desktop/ViewModels` using the view-model implementation standard below.
7. Implement the Avalonia view in `Mapping_Tools.Desktop/Views` using the view implementation standard below.
8. Register the feature in the Avalonia shell using the smallest navigation change required. Do not remove or redirect the WPF feature yet.
9. Build the new frontend, run focused tests, and pass the visual parity gate.
10. Report migrated behavior, intentionally deferred behavior, platform limitations, tests run, and the exact Avalonia 12.1 documentation pages consulted.

## View-model implementation standard

- Prefer small, feature-specific `partial` view models based on CommunityToolkit.Mvvm's `ObservableObject` or `ObservableValidator`.
- Use `[ObservableProperty]` for bindable mutable state whenever its generated equality, accessibility, and change-hook behavior preserve the feature contract. Use generated partial-property declarations when a non-public setter or XML documentation must remain explicit.
- Put DataAnnotations validation attributes and `[NotifyDataErrorInfo]` on generated properties in `ObservableValidator` types. Keep validation in the attributes rather than custom logic in the viewmodel.
- Use `[NotifyPropertyChangedFor]` and `[NotifyCanExecuteChangedFor]` for static dependent notifications instead of manual `OnPropertyChanged` calls.
- Use `[RelayCommand]` for synchronous and asynchronous view-model actions when the generated command has the required execution and cancellation semantics.
- Keep a manual property when it normalizes input, directly adapts a non-observable model without duplicate state, or requires setter ordering that generator hooks cannot preserve clearly.

## View implementation standard

- Copy the exact layout used by the original WPF XAML source.
- Map components in the WPF source to Avalonia components using the `control-parity` document.
- Use bindings with converters and parameters matching the original WPF source.
- Prefer compiled bindings and an explicit `x:DataType`.
- Use Material.Avalonia's semantic dynamic resources and the application-level styles already registered in `App.axaml`. 
- Prefer using re-usable styles and components rather than re-defining the same styles in every view.

## Visual parity gate

Use `$render-desktop-view` to capture the WPF view and Avalonia port with identical deterministic state and dimensions.

Treat the legacy WPF rendering as the specification unless the user explicitly
requests a redesign. "Similar", "modernized", "cleaner", and "structurally
equivalent" are not visual parity.

After editing:

1. Render WPF and Avalonia at identical dimensions and with identical data.
2. Open both images and compare them. Write down **all** visible differences.
   Also focus on details such as clipped text, slight color differences, missing dividers, or uneven margins.
3. Compare all applicable non-default states too: light mode, opened menus and context menus, hover,
   focus, checked and unchecked toggles, selected rows, populated tables,
   resized windows or columns, overflow, filled and empty states, and validation errors.
4. Try to make your test states maximally useful, testing multiple state changes in a single image.
   Test at most 3 different states per view. Fewer is better.
5. Iterate on every visible mismatch that is under application control.
   Framework rasterization differences may be documented only after font,
   size, weight, line height, spacing, and colors have been matched.
6. Do not claim parity or completion while differences remain.
7. Prove native behaviors such as title-bar dragging, window controls, popup
   placement, and platform dialogs in a real desktop run; a headless render
   cannot prove them.
8. In the handoff, list any remaining visible difference precisely. If none
   was explicitly authorized by the user, treat it as unfinished work rather
   than an intentional design change.

## Completion criteria

Complete a feature migration only when:

- Both the WPF application and Avalonia desktop project build.
- Extracted logic has focused automated coverage or a documented reason coverage is impractical.
- Core and Application contain no WPF, WinForms, Avalonia, or CommunityToolkit.Mvvm references.
- The Avalonia view uses APIs verified for 12.1.0 and compiled bindings where applicable.
- Each legacy interactive element is represented by the correct Avalonia
  control and preserves its selection, resizing, dragging, menu, hover,
  focus, checked, and keyboard behavior where applicable.
- Equal-state, equal-viewport WPF and Avalonia renders have been opened and
  inspected, and no unrequested visible design differences remain.
