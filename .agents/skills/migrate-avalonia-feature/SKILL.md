---
name: migrate-avalonia-feature
description: Migrate one small Mapping Tools feature from the legacy WPF/WinForms frontend to Avalonia 12.1.0. Use when porting a view, dialog, control, or simple feature slice; extracting its business logic from WPF code-behind; introducing UI-independent services and tests; creating CommunityToolkit.Mvvm view models and Material.Avalonia views; or reviewing a feature migration for framework leakage and behavior parity.
---

# Migrate Avalonia Feature

Migrate one bounded feature while keeping both frontends buildable. Do not perform a repository-wide cleanup as part of one feature migration.

## Documentation gate

Before writing or changing any Avalonia C# API, AXAML, binding, style, control, routed event, property, or platform service, read [references/avalonia-12.1.md](references/avalonia-12.1.md) completely and consult the linked official documentation online. Treat this as a mandatory pre-edit gate, even for familiar APIs.

For any UI-bearing migration, also read
[references/control-parity.md](references/control-parity.md) completely before
choosing controls or styles. Treat semantic control parity, interaction parity,
and visual parity as one acceptance gate.

Use only official Avalonia documentation, the Avalonia 12.1.0 tagged source/release, exact-version NuGet metadata, official CommunityToolkit.Mvvm documentation, and the Material.Avalonia repository as authorities. Do not use WPF knowledge or Avalonia 11 examples as evidence that an API exists. When current documentation covers a later release, verify the API against the 12.1.0 tag or package before using it.

## Project boundaries

- Put pure models, value objects, calculations, and domain rules in `Mapping_Tools.Core`.
- Put feature use cases and UI/OS abstraction interfaces in `Mapping_Tools.Application`.
- Put filesystem, osu!, audio, network, and platform adapter implementations in `Mapping_Tools.Infrastructure`.
- Put Avalonia views, CommunityToolkit.Mvvm presentation state, navigation, and UI-only adapters in `Mapping_Tools.Desktop`.
- Keep the existing `Mapping_Tools` WPF project runnable until the migrated feature is accepted.

Never add references to `System.Windows`, `System.Windows.Forms`, `MaterialDesignThemes.Wpf`, Avalonia, ReactiveUI, or CommunityToolkit.Mvvm packages to Core, Application, or Infrastructure. Infrastructure may contain an explicitly Windows-specific adapter only when the feature inherently requires Windows; keep its interface platform-neutral and call out the limitation.

Copy code from the WPF project exactly whenever possible, this will make manual review easier.

## XML documentation standard

Every public or protected API added to non-legacy production or tool projects
must have meaningful XML documentation. This includes types, constructors,
methods, properties, fields, events, delegates, operators, enum types and enum
members. Document parameters, type parameters, return values, exceptions, and
important platform or cancellation behavior where applicable. Prefer a
specific summary of the contract or behavior over restating the identifier.
Never generate documentation mechanically from a symbol name or signature.
Read the implementation and relevant call sites before writing each comment,
and make the documentation add information that the identifier does not:
units, ranges, invariants, ordering, ownership, mutation, side effects,
fallbacks, format compatibility, cancellation, or failure behavior. Reject
placeholder prose such as "Represents X", "Gets or sets X", "Performs X",
"The operation result", empty summaries, and parameter descriptions that only
repeat the parameter name. Every word of new documentation must be
context-specific prose written after understanding the API.
Use `<inheritdoc/>` only when an inherited or implemented contract already
describes the member accurately.

The legacy `Mapping_Tools` and `Mapping_Tools_Tests` projects and all test
projects are exempt. `Directory.Build.targets` generates documentation files
and treats CS1591 as an error for every other project. Do not suppress CS1591;
build every affected non-test project and resolve the diagnostic before
completing a migration.

## Workflow

1. Inspect the selected WPF XAML, code-behind, view model, converters, custom controls, and services. Trace every static dependency such as `MainWindow.AppWindow`, dialogs, dispatcher calls, clipboard, cursor, keyboard hooks, and settings singletons.
2. Record observable behavior and establish focused tests before moving logic. Include success, validation failure, cancellation, error behavior, hover, focus, checked/selected states, resizing, dragging, keyboard access, open menus, populated data, and empty data whenever relevant.
3. Classify code into domain rules, application orchestration, infrastructure, and presentation. Keep only rendering, focus, pointer gestures, animation, and other genuinely visual behavior in a view.
4. Extract the smallest reusable slice. Introduce interfaces for file/folder pickers, notifications, clipboard, dispatching, window ownership, or other UI/OS effects. Preserve the WPF behavior through WPF-side adapters where necessary.
5. Confirm the extracted Core/Application code contains no frontend types with searches such as `rg "System\\.Windows|System\\.Windows\\.Forms|Avalonia|ReactiveUI|CommunityToolkit\\.Mvvm" Mapping_Tools.Core Mapping_Tools.Application`.
6. Implement a CommunityToolkit.Mvvm view model in `Mapping_Tools.Desktop/ViewModels` using the view-model implementation standard below. Keep services constructor-injected and expose bindable typed state rather than controls or string mirrors of numeric, enum, date, duration, or other non-string values. Use DataAnnotations with `ObservableValidator` and `[NotifyDataErrorInfo]`; do not hand-write field-specific error dictionaries or recreate WPF `Binding.ValidationRules` syntax.
7. Implement the Avalonia view in `Mapping_Tools.Desktop/Views` with compiled bindings and an explicit `x:DataType`. Use reusable two-way `IValueConverter` implementations for text presentation and parsing of typed values; do not parse or format those values in the view model. Use Material.Avalonia's semantic dynamic resources and the application-level styles already registered in `App.axaml`. Do not translate WPF triggers, dependency properties, event names, dialog APIs, floating-hint labels, or literal colors mechanically.
8. Register the feature in the Avalonia shell using the smallest navigation change required. Do not remove or redirect the WPF feature yet.
9. Build the new frontend and run focused tests. Use `$render-desktop-view` to capture the WPF view and Avalonia port with identical deterministic state and dimensions, inspect both PNGs, and resolve or record visible differences. Use a real desktop run for native dialogs, overlays, global input, audio, or other platform behavior.
10. Run the XML-documentation build gate for every affected non-legacy,
    non-test project and resolve all CS1591 diagnostics. Also search the
    affected files for empty or identifier-paraphrasing documentation; CS1591
    alone does not detect low-quality comments.
11. Report migrated behavior, intentionally deferred behavior, platform limitations, tests run, and the exact Avalonia 12.1 documentation pages consulted.

## View-model implementation standard

- Prefer small, feature-specific `partial` view models based on CommunityToolkit.Mvvm's `ObservableObject` or `ObservableValidator`.
- Use `[ObservableProperty]` for bindable mutable state whenever its generated equality, accessibility, and change-hook behavior preserve the feature contract. Use generated partial-property declarations when a non-public setter or XML documentation must remain explicit.
- Put DataAnnotations validation attributes and `[NotifyDataErrorInfo]` on generated properties in `ObservableValidator` types. Keep validation state in the toolkit rather than maintaining error dictionaries or duplicating validation events.
- Use `[NotifyPropertyChangedFor]` and `[NotifyCanExecuteChangedFor]` for static dependent notifications instead of manual `OnPropertyChanged` calls.
- Use `[RelayCommand]` for synchronous and asynchronous view-model actions when the generated command has the required execution and cancellation semantics.
- Keep a manual property when it normalizes input, directly adapts a non-observable model without duplicate state, or requires setter ordering that generator hooks cannot preserve clearly.
- Do not introduce broad observable adapter or base classes merely to consolidate generated properties. Keep state beside the feature that owns its behavior unless multiple real consumers justify a separate abstraction.

## Visual parity gate

Treat the legacy WPF rendering as the specification unless the user explicitly
requests a redesign. "Similar", "modernized", "cleaner", and "structurally
equivalent" are not visual parity.

Before editing the Avalonia view:

1. Render the complete legacy view in its real shell, not only the isolated
   child control.
2. Record the exact viewport, semantic state, theme, visible text, hierarchy,
   column and row sizes, margins, padding, typography, colors, borders,
   control density, scrollbars, chrome, menus, and empty states.
3. Reuse the legacy text and layout measurements exactly where the frameworks
   permit. Do not add badges, cards, buttons, labels, release notes, empty-state
   prose, wider navigation, different chrome, or other design changes that the
   WPF reference does not contain.
4. Map each interactive WPF control to the corresponding interactive Avalonia
   control. A collection of borders, text blocks, grids, or pointer handlers
   that only resembles a menu, table, list, splitter, outlined text box,
   icon, or Material color surface fails this gate.

After editing:

1. Render WPF and Avalonia at identical dimensions and with identical data.
2. Open both images and compare them side by side. Inspect the complete shell
   and each migrated child view.
3. Compare non-default states too: opened menus and context menus, hover,
   focus, checked and unchecked toggles, selected rows, populated tables,
   resized columns or splitters, overflow, and empty states.
4. Iterate on every visible mismatch that is under application control.
   Framework rasterization differences may be documented only after font,
   size, weight, line height, spacing, and colors have been matched.
5. Do not claim parity or completion while obvious differences remain.
   Successful compilation, matching feature structure, shared colors, or a
   subjective impression are not substitutes for this gate.
6. Prove native behaviors such as title-bar dragging, window controls, popup
   placement, and platform dialogs in a real desktop run; a headless render
   cannot prove them.
7. In the handoff, list any remaining visible difference precisely. If none
   was explicitly authorized by the user, treat it as unfinished work rather
   than an intentional design change.

## Completion criteria

Complete a feature migration only when:

- Both the WPF application and Avalonia desktop project build.
- Extracted logic has focused automated coverage or a documented reason coverage is impractical.
- Core and Application contain no WPF, WinForms, Avalonia, ReactiveUI, or CommunityToolkit.Mvvm references.
- The Avalonia view uses APIs verified for 12.1.0 and compiled bindings where applicable.
- Each legacy interactive element is represented by the correct Avalonia
  control and preserves its selection, resizing, dragging, menu, hover,
  focus, checked, and keyboard behavior where applicable.
- Equal-state, equal-viewport WPF and Avalonia renders have been opened and
  inspected, and no unrequested visible design differences remain.
- Cancellation and error paths do not depend on view code-behind.
- Every public and protected API in affected non-legacy, non-test projects has
  meaningful XML documentation, and the CS1591 build gate passes without
  suppressions.
- The legacy feature remains available until the user explicitly authorizes removal.
