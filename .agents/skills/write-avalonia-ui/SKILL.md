---
name: write-avalonia-ui
description: Use when writing a view in the Avalonia-based project.
---

# Write Avalonia UI

How to write a view for the Avalonia project.

## Documentation gate

Before writing or changing any Avalonia C# API, AXAML, binding, style, control, routed event, property, or platform service, read [references/avalonia-12.1.md](references/avalonia-12.1.md) completely and consult the linked official documentation online. Treat this as a mandatory pre-edit gate, even for familiar APIs.

For any UI-bearing migration, also read
[references/control-parity.md](references/control-parity.md) completely before
choosing controls or styles. Treat source, semantic-control, and interaction
parity as one acceptance gate.

Use only official Avalonia documentation, the Avalonia 12.1.0 tagged source/release, exact-version NuGet metadata, official CommunityToolkit.Mvvm documentation, and the Material.Avalonia repository as authorities. Do not use WPF knowledge or Avalonia 11 examples as evidence that an API exists. When current documentation covers a later release, verify the API against the 12.1.0 tag or package before using it.

## View-model implementation standard

- Prefer small, feature-specific `partial` view models based on CommunityToolkit.Mvvm's `ObservableObject` or `ObservableValidator`.
- Use `[ObservableProperty]` for bindable mutable state whenever its generated equality, accessibility, and change-hook behavior preserve the feature contract. Use generated partial-property declarations when a non-public setter or XML documentation must remain explicit.
- Put DataAnnotations validation attributes and `[NotifyDataErrorInfo]` on generated properties in `ObservableValidator` types. Keep validation in the attributes rather than custom logic in the viewmodel.
- Use `[NotifyPropertyChangedFor]` and `[NotifyCanExecuteChangedFor]` for static dependent notifications instead of manual `OnPropertyChanged` calls.
- Use `[RelayCommand]` for synchronous and asynchronous view-model actions when the generated command has the required execution and cancellation semantics.
- Keep a manual property when it normalizes input, directly adapts a non-observable model without duplicate state, or requires setter ordering that generator hooks cannot preserve clearly.

## View implementation standard

- Use bindings with converters and parameters for presenting non-string types.
- Prefer compiled bindings and an explicit `x:DataType`.
- Use `ToolViewHeader`, `ToolRunButton`, and `ToolProgressBar` for the corresponding mapping tool elements.
- Preserve shell-owned scrolling. Add a view-owned scroller only for the relevant sub-grids.
- Use Material.Avalonia's semantic dynamic resources.
- Keep custom-control styles in the control AXAML or a co-located control-owned style file. Keep view-only and shell-only styles in their respective AXAML files.
- Put application-wide Material compatibility overrides in focused dictionaries under `Mapping_Tools.Desktop/Resources/Styles` and compose them from `App.axaml`.
