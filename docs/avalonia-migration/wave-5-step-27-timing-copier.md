# Wave 5, Step 27: Timing Copier

Status: implemented as a bounded vertical slice.

The Core engine preserves the legacy timing-copy modes: preserve beat spacing,
resnap objects and timing points, or keep objects fixed. The Application service
opens the source once, processes each pipe-separated target, reports aggregate
progress, and saves through the existing beatmap gateway so the current backup
boundary remains in effect. The Avalonia view model preserves the legacy picker,
resnap-mode, beat-divisor, run, cancellation, completion, and error flows. Legacy
project JSON continues to use the `TimingCopierVm` type alias.

Platform substitutions are limited to the existing migration patterns: WPF
dialogs are represented by `IFilePicker`, background-worker execution is handled
by `ToolExecutionService`, and the WPF header/run/progress controls are replaced
with the shared Avalonia tool controls. The WPF implementation remains unchanged
and runnable.

No current-wave Timing Copier behavior is intentionally deferred. Focused Core,
Application, Infrastructure, and Avalonia Desktop tests pass. The Avalonia
build required access to its user licensing directory outside the workspace.

Avalonia migration references consulted:

- https://docs.avaloniaui.net/docs/data-binding/binding-validation
- https://docs.avaloniaui.net/docs/data-binding/compiled-bindings
- https://docs.avaloniaui.net/docs/data-binding/how-to-create-a-custom-data-binding-converter
- https://docs.avaloniaui.net/docs/data-binding/data-binding-syntax
- https://docs.avaloniaui.net/api/avalonia/data/updatesourcetrigger
- https://docs.avaloniaui.net/docs/xaml/directives
- https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
