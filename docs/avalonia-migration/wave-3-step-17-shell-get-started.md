# Wave 3, step 17: shell and Get started

Status: implemented, 2026-07-25.

## Scope delivered

The temporary greeting window has been replaced by the first production
Avalonia shell slice:

- `ShellFeatureRegistration` and `IShellFeatureRegistry` provide an ordered,
  explicit feature list with stable IDs, descriptions, categories, and search
  terms;
- registration rejects duplicate IDs and never scans assemblies or reflects
  over view types;
- `MainViewModel` supports case-insensitive partial and exact search, clearing
  the query, favorite-first ordering, immediate favorite persistence, lazy
  feature creation, current-content switching, and lifecycle activation;
- `IShellFeatureActivation` gives presentation models explicit
  activate/deactivate callbacks without requiring a control or window;
- the shell uses a collapsible `SplitView` pane and a typed
  `ContentControl` data template, so future migrations add one registration
  and one typed view template rather than editing a reflective view
  collection;
- the process-lifetime notification stream is marshalled through
  `IUiDispatcher` and shown as an ordered, independently dismissible queue;
  repeated messages remain distinct, severity remains available to
  presentation state, and the visible surface follows the legacy bottom
  snackbar rather than introducing notification cards; and
- the initial registry contains Get started, the only feature migrated in this
  step. Preferences and tool registrations are added by their own later
  migration steps rather than presenting non-functional placeholders.

## Get started

`GetStartedViewModel` and `GetStartedView` preserve the legacy landing-page
structure and presentation: the original numbered text, non-wrapping
onboarding list, two equal columns, splitters, blue 32-point Get started,
Changelog, and Recent headings, and the Path/Date recent-map table on the dark
Material surface. No cards, badges, extra landing-page buttons, rewritten
instructions, release notes, or empty-state prose are added.

The Avalonia view does not perform a network request during construction, so
the legacy empty changelog state remains deterministic and safe offline.
Website and source links remain available from the shell's legacy-positioned
About menu through `IPlatformLauncher`. If the operating system rejects a
support link, the page publishes a warning through the shared notification
surface.
Opening recent maps remains part of step 19's current-map lifecycle rather
than bypassing that pending workflow.

## Window persistence

`MainWindow` persists its last normal-state position and size plus maximized
state through the existing `ApplicationSettings` and `ISettingsService`.
Positions and working areas are translated between Avalonia pixel coordinates
and device-independent settings using each screen's scaling factor.

`WindowPlacementCalculator` validates finite positive geometry, constrains
oversized windows to the usable working area, preserves bounds on a still
connected monitor, and moves off-screen bounds to the primary monitor after a
display is disconnected. Minimized state is never persisted as the next
startup state. A parameterless shell constructor exists only for XAML tooling
and deterministic rendering; desktop DI selects the settings-aware
constructor.

## Automated and visual coverage

`Mapping_Tools.Platform.Tests` verifies duplicate registration rejection;
partial, exact, and cleared search; favorite persistence and ordering; lazy
activation, deactivation, and instance reuse; repeated queued notifications
and independent dismissal; disconnected-monitor recovery; and invalid or
oversized window geometry. The focused suite passes 121 tests. The architecture
suite passes all 3 dependency tests.

Both frontends build. The Avalonia project builds with zero warnings and zero
errors. The unchanged WPF project builds with its existing SDK/package and
legacy analyzer warnings. A real Avalonia process remained running through the
startup smoke interval and was then stopped explicitly.

Matching 1280 by 800 WPF/Avalonia captures were generated and inspected:

- `artifacts/view-renders/wave3-step17-get-started-wpf-parity.png`
- `artifacts/view-renders/wave3-step17-get-started-avalonia-parity-final.png`
- `artifacts/view-renders/wave3-step17-shell-wpf.png`
- `artifacts/view-renders/wave3-step17-shell-avalonia-parity-final.png`

The renderer supplies the complete legacy navigation list to the Avalonia
shell as deterministic visual data while production still registers only
migrated features. This makes the comparison equal-state instead of accepting
an almost-empty drawer as an intentional difference.

The inspected renders align the 35-pixel custom chrome, blue primary and dark
current-map zones, caption buttons, File/About positions, 200-pixel drawer,
search field, selected/default/tool row densities, divider, 20-pixel content
inset, exact onboarding copy, equal content columns, section headings,
splitters, horizontal scroll controls, Recent table, dark paper, and empty
changelog/recent states. WPF and Avalonia still rasterize text and the
scrollbar thumb at slightly different subpixel boundaries; those framework
rasterization differences do not change the application-controlled geometry,
content, color, typography settings, or control hierarchy.

## Documentation consulted

Avalonia 12.1 APIs and patterns used by this step:

- <https://docs.avaloniaui.net/controls/layout/containers/splitview>
- <https://docs.avaloniaui.net/controls/data-display/contentcontrol>
- <https://docs.avaloniaui.net/docs/data-binding/compiled-bindings>
- <https://docs.avaloniaui.net/docs/app-development/window-management>
- <https://docs.avaloniaui.net/docs/how-to/window-how-to>
- <https://docs.avaloniaui.net/controls/primitives/window>
- <https://docs.avaloniaui.net/docs/styling/control-themes>
- <https://docs.avaloniaui.net/docs/styling/typography>
- <https://docs.avaloniaui.net/controls/menus/menuflyout>
- <https://docs.avaloniaui.net/docs/how-to/scrollviewer-how-to>
