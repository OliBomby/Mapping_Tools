---
name: migrate-feature
description: Use when migrating a feature from the legacy WPF project to the new Avalonia-based project.
---

# Migrate Feature

Migrate one bounded feature while keeping both frontends buildable. Do not perform a repository-wide cleanup as part of one feature migration.

If the feature includes UI, use the `$migrate-ui` skill.

## Project boundaries

- Put pure models, value objects, calculations, and domain rules in `Mapping_Tools.Core`.
- Put feature use cases and UI/OS abstraction interfaces in `Mapping_Tools.Application`.
- Put filesystem, osu!, audio, network, and platform adapter implementations in `Mapping_Tools.Infrastructure`.
- Put Avalonia views, CommunityToolkit.Mvvm presentation state, navigation, and UI-only adapters in `Mapping_Tools.Desktop`.
- Keep the existing `Mapping_Tools` WPF project runnable until the migrated feature is accepted.

Never add references to `System.Windows`, `System.Windows.Forms`, `MaterialDesignThemes.Wpf`, Avalonia, ReactiveUI, or CommunityToolkit.Mvvm packages to Core, Application, or Infrastructure. Infrastructure may contain an explicitly Windows-specific adapter only when the feature inherently requires Windows; keep its interface platform-neutral and call out the limitation.

Copy code from the WPF project exactly whenever possible, this will make manual review easier.

## Workflow

1. Inspect the original code carefully.
2. Record observable behavior and establish focused tests before moving logic.
3. Classify code into domain rules, application orchestration, infrastructure, and presentation. Keep only rendering, focus, pointer gestures, animation, and other genuinely visual behavior in a view.
4. Migrate the feature.
5. Build the project and run focused tests.
6. Report migrated behavior, intentionally deferred behavior, platform limitations, tests run.

## Completion criteria

Complete a feature migration only when:

- All tests pass.
- Extracted logic has focused automated coverage or a documented reason coverage is impractical.
- The legacy feature remains available until the user explicitly authorizes removal.
