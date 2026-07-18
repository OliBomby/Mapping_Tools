---
name: migrate-avalonia-feature
description: Migrate one small Mapping Tools feature from the legacy WPF/WinForms frontend to Avalonia 12.1.0. Use when porting a view, dialog, control, or simple feature slice; extracting its business logic from WPF code-behind; introducing UI-independent services and tests; creating ReactiveUI view models and Material.Avalonia views; or reviewing a feature migration for framework leakage and behavior parity.
---

# Migrate Avalonia Feature

Migrate one bounded feature while keeping both frontends buildable. Do not perform a repository-wide cleanup as part of one feature migration.

## Documentation gate

Before writing or changing any Avalonia C# API, AXAML, binding, style, control, routed event, property, or platform service, read [references/avalonia-12.1.md](references/avalonia-12.1.md) completely and consult the linked official documentation online. Treat this as a mandatory pre-edit gate, even for familiar APIs.

Use only official Avalonia documentation, the Avalonia 12.1.0 tagged source/release, exact-version NuGet metadata, ReactiveUI documentation, and the Material.Avalonia repository as authorities. Do not use WPF knowledge or Avalonia 11 examples as evidence that an API exists. When current documentation covers a later release, verify the API against the 12.1.0 tag or package before using it.

## Project boundaries

- Put pure models, value objects, calculations, and domain rules in `Mapping_Tools.Core`.
- Put feature use cases and UI/OS abstraction interfaces in `Mapping_Tools.Application`.
- Put filesystem, osu!, audio, network, and platform adapter implementations in `Mapping_Tools.Infrastructure`.
- Put Avalonia views, ReactiveUI presentation state, navigation, and UI-only adapters in `Mapping_Tools.Desktop`.
- Keep the existing `Mapping_Tools` WPF project runnable until the migrated feature is accepted.

Never add references to `System.Windows`, `System.Windows.Forms`, `MaterialDesignThemes.Wpf`, or Avalonia/ReactiveUI packages to Core, Application, or Infrastructure. Infrastructure may contain an explicitly Windows-specific adapter only when the feature inherently requires Windows; keep its interface platform-neutral and call out the limitation.

## Workflow

1. Inspect the selected WPF XAML, code-behind, view model, converters, custom controls, and services. Trace every static dependency such as `MainWindow.AppWindow`, dialogs, dispatcher calls, clipboard, cursor, keyboard hooks, and settings singletons.
2. Record observable behavior and establish focused tests before moving logic. Include success, validation failure, cancellation, and error behavior relevant to the feature.
3. Classify code into domain rules, application orchestration, infrastructure, and presentation. Keep only rendering, focus, pointer gestures, animation, and other genuinely visual behavior in a view.
4. Extract the smallest reusable slice. Introduce interfaces for file/folder pickers, notifications, clipboard, dispatching, window ownership, or other UI/OS effects. Preserve the WPF behavior through WPF-side adapters where necessary.
5. Confirm the extracted Core/Application code contains no frontend types with searches such as `rg "System\\.Windows|System\\.Windows\\.Forms|Avalonia|ReactiveUI" Mapping_Tools.Core Mapping_Tools.Application`.
6. Implement a ReactiveUI view model in `Mapping_Tools.Desktop/ViewModels`. Use `ReactiveObject`, `RaiseAndSetIfChanged`, and `ReactiveCommand` only when the verified 12.1-compatible documentation supports the pattern. Keep services constructor-injected and expose bindable state rather than controls.
7. Implement the Avalonia view in `Mapping_Tools.Desktop/Views` with compiled bindings and an explicit `x:DataType`. Use Material.Avalonia resources already registered in `App.axaml`. Do not translate WPF triggers, dependency properties, event names, or dialog APIs mechanically.
8. Register the feature in the Avalonia shell using the smallest navigation change required. Do not remove or redirect the WPF feature yet.
9. Build the new frontend, run focused tests, and manually verify behavior parity. If visual interaction matters, launch and inspect the view rather than relying on compilation alone.
10. Report migrated behavior, intentionally deferred behavior, platform limitations, tests run, and the exact Avalonia 12.1 documentation pages consulted.

## Completion criteria

Complete a feature migration only when:

- Both the WPF application and Avalonia desktop project build.
- Extracted logic has focused automated coverage or a documented reason coverage is impractical.
- Core and Application contain no WPF, WinForms, Avalonia, or ReactiveUI references.
- The Avalonia view uses APIs verified for 12.1.0 and compiled bindings where applicable.
- Cancellation and error paths do not depend on view code-behind.
- The legacy feature remains available until the user explicitly authorizes removal.

