# Wave 3, step 16: common forms and typed dialogs

Status: implemented, 2026-07-25.

## Scope delivered

The Application layer now defines UI-independent primitives for the form and
dialog workflows used by the rest of Wave 3:

- `ValidationOutcome`, `IValueValidator<T>`, and
  `DelegateValueValidator<T>` express validation without WPF or Avalonia
  types;
- `ValueValidators` supplies required-text and inclusive-range rules while
  feature-specific rules remain explicit delegates;
- `ITextValueConverter<T>` and `TextValueConverters` provide exact string,
  invariant `double`, and invariant `int` editing formats;
- `MessageDialogRequest<TResult>` returns caller-owned result types instead
  of framework button enums and requires exactly one Enter/default action and
  one Escape/cancel action;
- `ValueDialogRequest<TValue>` combines an initial value, converter, ordered
  validators, and button labels, and returns a typed accepted/cancelled result;
- `IDialogService` exposes message and value interactions without leaking
  window, dispatcher, or control types into Application.

The contracts snapshot caller-owned lists so later collection mutations cannot
change an already-created dialog. Invalid request metadata fails before any
window opens.

## Avalonia presentation

`AvaloniaDialogService` creates short-lived owner-modal windows and marshals
calls to the Avalonia UI dispatcher. Cancellation closes an open window and
then completes with `OperationCanceledException`; user cancellation instead
returns the request's typed cancel outcome. Native title-bar dismissal returns
the explicit message fallback or a cancelled value result.

The message window supports wrapping primary content, optional nested
error/details text, scrolling for long content, ordered typed actions, and
default/cancel keyboard roles. The value window selects its initial text,
validates on each edit, displays the first correction message, disables invalid
submission, and keeps parsing, cancellation, and result creation outside view
code-behind. Both views use compiled bindings with explicit `x:DataType`.

The service is a desktop-lifetime singleton and always resolves the current
`MainWindow` as owner. The owner is disabled for the modal lifetime by
`Window.ShowDialog<T>`.

## Legacy compatibility and visual parity

The WPF `MessageDialog`, `TypeValueDialog`, `CustomDialog`, validation rules,
and their existing callers remain unchanged and runnable. New Avalonia features
must declare forms explicitly; Step 16 deliberately does not reproduce
`CustomDialog`'s reflection over arbitrary objects, WPF attributes, controls,
settings singletons, or static file-dialog helpers.

The Avalonia dialogs preserve the legacy dark paper surface, medium-weight
typography, blue flat action buttons, left-aligned action row, field underline,
spacing, and wrapping width. Caller-supplied labels and optional details remain
typed extensions of that presentation. The value dialog adds immediate
correction text and disables its default action for invalid input; valid and
empty-state parity renders keep that extra state hidden.

## Automated and visual coverage

`Mapping_Tools.Platform.Tests` verifies:

- valid and invalid invariant numeric parsing;
- required-text and inclusive-range validation;
- ambiguous message keyboard actions are rejected;
- dialog request choice collections are snapshotted;
- invalid text blocks acceptance and exposes its correction;
- valid text submits the parsed typed value;
- cancellation never invokes the accept callback;
- typed message actions preserve their default/cancel roles; and
- `IDialogService` is registered as a singleton and the full desktop service
  provider validates.

The focused platform suite passes 114 tests. The combined desktop and hosted
service validation specifically guards constructor activation for every hosted
service; it catches the startup failure that occurred when
`GlobalHotkeyHostedService` exposed only an internal constructor.

Deterministic WPF and Avalonia renders were captured at matching dimensions
for message and value interactions:

- `artifacts/view-renders/wave3-step16-message-wpf.png`
- `artifacts/view-renders/wave3-step16-message.png`
- `artifacts/view-renders/wave3-step16-value-wpf.png`
- `artifacts/view-renders/wave3-step16-value.png`

The four PNGs were inspected for wrapping, spacing, typography, button layout,
enabled state, clipping, and dark-theme contrast. The WPF renderer now supplies
the same inherited foreground, Roboto font, medium weight, and dark paper
context as the legacy main window and no longer stretches embedded dialog
content across the capture.

The freshly built Avalonia Release executable was also started attached to a
console. The Generic Host reported `Application started` with no activation
exception and shut down cleanly on request. Native owner disabling, title-bar
dismissal, keyboard routing, accessibility announcement, and cancellation
while a dialog is visible still require an interactive dialog smoke test
because the renderer captures static control trees rather than native modal
behavior.

## Documentation consulted

Avalonia 12.1 APIs and patterns used by this step:

- <https://docs.avaloniaui.net/docs/app-development/window-management>
- <https://docs.avaloniaui.net/api/avalonia/controls/window>
- <https://docs.avaloniaui.net/controls/input/buttons/button>
- <https://docs.avaloniaui.net/api/avalonia/controls/button>
- <https://docs.avaloniaui.net/docs/app-development/threading>
- <https://docs.avaloniaui.net/api/avalonia/threading/dispatcher>
- <https://docs.avaloniaui.net/docs/data-binding/compiled-bindings>
- <https://docs.avaloniaui.net/docs/app-development/data-validation>
- <https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform>
- <https://docs.avaloniaui.net/docs/styling/styles>
- <https://github.com/AvaloniaCommunity/Material.Avalonia>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>
- <https://www.nuget.org/packages/Avalonia/12.1.0>

The Avalonia 12 data-validation page was consulted to confirm that the
data-annotations plugin is disabled by default. This slice therefore keeps
validation explicit in the view model and does not enable a second,
overlapping validation pipeline.
