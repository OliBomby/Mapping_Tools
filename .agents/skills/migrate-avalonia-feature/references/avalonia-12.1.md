# Avalonia 12.1 documentation policy

The desktop frontend pins Avalonia to `12.1.0`. Verify Avalonia work online before editing it; these links are pointers, not a substitute for consultation.

## Required authorities

- Avalonia 12.1.0 release and source: https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.0 and https://github.com/AvaloniaUI/Avalonia/tree/12.1.0
- Avalonia documentation: https://docs.avaloniaui.net/docs/
- Avalonia API reference: https://docs.avaloniaui.net/api/
- Binding validation and DataAnnotations: https://docs.avaloniaui.net/docs/data-binding/binding-validation
- Custom data-binding converters: https://docs.avaloniaui.net/docs/data-binding/how-to-create-a-custom-data-binding-converter
- WPF migration guide: https://docs.avaloniaui.net/docs/migration/wpf
- Avalonia 12 breaking changes: https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- Avalonia 12.1.0 package metadata: https://www.nuget.org/packages/Avalonia/12.1.0
- Official Avalonia templates: https://github.com/AvaloniaUI/avalonia-dotnet-templates
- ReactiveUI Avalonia integration: https://www.reactiveui.net/docs/getting-started/installation/avalonia and https://www.nuget.org/packages/ReactiveUI.Avalonia/12.0.3
- Material.Avalonia setup: https://github.com/AvaloniaCommunity/Material.Avalonia and https://www.nuget.org/packages/Material.Avalonia/3.17.0

## Verification rules

1. Read the relevant official conceptual and API pages before writing AXAML or Avalonia C#.
2. Confirm members, namespaces, signatures, and XAML syntax against the `12.1.0` tag or exact package when documentation is unversioned or describes a newer release.
3. Prefer patterns emitted by the official Avalonia 12.1.0 templates.
4. Do not cite an Avalonia 11 page as proof of 12.1 behavior.
5. Do not invent substitutes for missing WPF controls. Search the 12.1 docs and package catalog, then implement a small local control only when no supported equivalent exists.
6. Keep package versions pinned unless the user explicitly requests an upgrade.
7. Include the exact documentation URLs consulted in the migration handoff.
