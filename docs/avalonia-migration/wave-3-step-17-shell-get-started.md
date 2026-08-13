# Wave 3, step 17: shell and Get started

Status: implemented, 2026-07-25.

## 2026-08-13 compatibility clarification

`TableView` is the explicit platform substitution for WPF's read-only
`ListView`/`GridView`: it is Avalonia 12.1's integrated selectable table with
column headers and user resizing. Material.Avalonia 3.17 does not supply an
Avalonia 12.1 `TableView` theme, so the co-located compact compatibility themes
are unavoidable view-owned presentation code. The Path column is measured from
the actual filename content and remains user-resizable; the Date column fills
the remaining space. This preserves the legacy auto-content/fill contract
without introducing a second table model.

The onboarding instructions are again literal presentation content in AXAML,
and the recent section binds its empty state. The shell once again owns feature
scrolling; each registration declares the horizontal and vertical behavior of
its corresponding WPF content presenter.

The shell's owner-modal dialogs and queued notification surface remain an
explicit Avalonia architecture exception to WPF `DialogHost` and Snackbar.
They retain typed choices, owner modality, ordering, dismissal, and the legacy
bottom notification placement without leaking framework controls into the
application layer.

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
- the shell uses a collapsible `SplitView` pane, a real selected-item
  `ListBox`, and a typed `ContentControl` data template, so future migrations
  add one registration and one typed view template rather than editing a
  reflective view collection;
- the chrome now uses Material `ColorZone` surfaces, an Avalonia `Menu` with
  `MenuItem` children, and `Material.Icons.Avalonia` 3.0.2 controls instead of
  button/flyout, border, and path facades. Version 3.0.2 is required because
  the 2.x icon control targets Avalonia 11 and fails at runtime on Avalonia 12;
- title-bar controls are explicitly marked as user-interactive decoration
  elements. The hamburger, File/About menus, minimize, maximize/restore, and
  close controls therefore receive input. The flat, darker current-map
  `ColorZone` explicitly uses shadow depth zero and handles a left press with
  `Window.BeginMoveDrag`, preserving title dragging on that user-interactive
  surface;
- the search field uses Material.Avalonia's outlined text-box theme, including
  the clear-button behavior, rather than a hand-drawn border. Its scoped
  template-part sizing keeps all four outline edges visible at the legacy
  35-pixel height;
- popup menus and context menus use compact 32-pixel item containers while the
  top-level File/About menu retains the legacy chrome geometry;
- the navigation toggle's icon is brighter while the pane is open and dimmer
  while it is closed, using the toggle's real checked pseudo-class; and
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
structure and presentation: the original numbered text in a real `ListBox`,
the selectable changelog list and recent-map table, two equal columns, movable
`GridSplitter` controls, blue 32-point Get started, Changelog, and Recent
headings, and the Path/Date recent-map table on the dark Material surface. No
cards, badges, extra landing-page buttons, rewritten instructions, release
notes, or empty-state prose are added.

Recent maps use Avalonia 12.1's real `TableView`: column headers, rows,
selection, scrolling, and resizer thumbs belong to the same control. The Path
column is initially sized from representative filename content and remains
user-resizable; the Date column consumes the remaining width. Both empty and
populated WPF/Avalonia states were rendered because an empty table cannot
verify shared header/cell column boundaries.

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

Both frontends build. The Avalonia Release project builds with zero warnings
and zero errors. The unchanged WPF project builds with its existing
SDK/package and legacy analyzer warnings. A real Avalonia process remained
running through the startup smoke interval and was then closed through its own
caption button.

Windows UI Automation exercised the real desktop window, not the image
renderer. It dragged the window from the current-map surface by exactly 40 by
25 pixels and restored the original position, toggled navigation
On -> Off -> On, opened File and found all five submenu items at 32 pixels
high, opened the current-map context menu with the same compact item height,
dragged the Recent Path column 40 pixels and observed the Date header move by
the same 40 pixels, maximized and restored the window, and invoked Close.
Native captures of the navigation icon measured a brighter open-state region
than the closed state. A before/after screen capture of the maximize button
changed 1,554 of 1,645 pixels while hovered, proving that native title-bar hit
testing and visual hover state both run.

Matching 1280 by 800 WPF/Avalonia captures were generated and inspected:

- `artifacts/view-renders/wave3-step17-get-started-wpf-parity.png`
- `artifacts/view-renders/wave3-step17-get-started-avalonia-1280x800.png`
- `artifacts/view-renders/wave3-step17-shell-wpf.png`
- `artifacts/view-renders/wave3-step17-shell-avalonia-final.png`
- `artifacts/view-renders/wave3-step17-shell-wpf-recent-maps.png`
- `artifacts/view-renders/wave3-step17-shell-avalonia-recent-maps.png`
- `artifacts/view-renders/wave3-step17-navigation-toggle-open.png`
- `artifacts/view-renders/wave3-step17-navigation-toggle-closed.png`

The renderer supplies the complete legacy navigation list to the Avalonia
shell as deterministic visual data while production still registers only
migrated features. This makes the comparison equal-state instead of accepting
an almost-empty drawer as an intentional difference.

The parity correction embeds the same Roboto regular, medium, bold, italic,
and bold-italic faces used by the legacy MaterialDesignThemes.Wpf shell.
The inspected full-shell difference image aligns the 35-pixel custom chrome,
flat dark current-map color zone, File/About positions,
200-pixel drawer, outlined search geometry and border color,
selected/default/tool row densities, unclipped tool labels, navigation thumb,
20-pixel content inset, exact onboarding copy and baselines, equal content
columns, two-pixel vertical splitter, horizontal splitter, native scroll
controls, integrated and resizable Recent columns, Recent header/rule, dark
paper, and empty and populated changelog/recent states.

The remaining overlay noise follows individual glyph edges and advances
produced by WPF's text renderer versus Avalonia's Skia renderer. It does not
represent a shifted row, section, control, color block, or other
application-controlled layout difference.

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
- <https://docs.avaloniaui.net/docs/styling/custom-fonts>
- <https://docs.avaloniaui.net/docs/styling/styles>
- <https://docs.avaloniaui.net/api/avalonia/styling/selectors>
- <https://docs.avaloniaui.net/controls/menus/menu>
- <https://docs.avaloniaui.net/api/avalonia/controls/menuitem>
- <https://docs.avaloniaui.net/controls/layout/panels/gridsplitter>
- <https://docs.avaloniaui.net/controls/input/text-input/textbox>
- <https://docs.avaloniaui.net/controls/data-display/collections/listbox>
- <https://docs.avaloniaui.net/api/avalonia/controls/tableview>
- <https://docs.avaloniaui.net/api/avalonia/controls/tableviewcolumn>
- <https://docs.avaloniaui.net/api/avalonia/controls/window>
- <https://docs.avaloniaui.net/controls/buttons/togglebutton>
- <https://docs.avaloniaui.net/docs/input-interaction/keyboard-and-hotkeys>
- <https://docs.avaloniaui.net/controls/primitives/scrollbar>
- <https://docs.avaloniaui.net/docs/how-to/scrollviewer-how-to>
