# Wave 2, step 14: tool execution and application hosting

Status: implemented, 2026-07-25.

## Scope delivered

The Application layer now owns the execution policy that legacy WPF tools
previously repeated through `BackgroundWorker`, view flags, progress
properties, completion handlers, and message windows:

- `ToolExecutionRequest<T>` gives every invocation a stable concurrency and
  cancellation key, a user-facing name, and a typed asynchronous operation;
- `ToolExecutionContext` supplies cooperative cancellation and validated
  zero-to-one-hundred progress with optional stage text;
- `ToolExecutionOutput<T>` carries a typed value, optional success summary,
  and an explicit editor-reload request;
- `ToolExecutionResult<T>` represents success, cancellation, failure, and
  duplicate-run rejection without using exceptions as frontend state;
- `IToolExecutionService` runs accepted work off the UI thread, prevents
  concurrent invocations with the same key, supports targeted and process-wide
  cancellation, applies the live `AutoReload` setting, and captures reload
  failures in the same terminal result;
- `IUserNotificationService` exposes immutable severity, title, message, and
  optional diagnostic data without choosing a snackbar, dialog, status bar, or
  dispatcher.

Notification delivery is synchronous on the publishing thread. A future
Avalonia subscriber must marshal presentation work to its UI dispatcher.
Subscriber failures are isolated from the already determined tool outcome so
a broken notification surface cannot turn a successful map operation into a
reported tool failure.

## Generic Host composition root

The Avalonia application now creates and starts an `IHost` after the classic
desktop lifetime is available. The host owns the existing desktop singleton
registrations and adds the standard configuration and logging services.
`MainWindow` and `MainViewModel` are resolved from the host rather than a
manually built root `ServiceProvider`.

Two hosted services connect background work to application lifetime:

- `ToolExecutionHostedService` cancels and joins active tool invocations when
  the host stops;
- `PeriodicBackupHostedService` waits for the current settings interval,
  locates and opens the live current beatmap, and asks the step 13 backup
  service to snapshot it only when its content changed. Individual failures
  are logged and do not terminate future backup cycles.

Avalonia's desktop `Exit` event initiates a five-second graceful host stop and
then disposes the host. A startup failure disposes the partially started host
before propagating the error.

## Legacy compatibility

The WPF `SingleRunMappingTool`, its `BackgroundWorker` instances, message
windows, and `RunToolCompletedEventArgs` remain intact for unmigrated
features. Their observed policies informed the new boundary: one active run
per feature, bounded progress, error reporting, optional success prose, and
reload only after successful work.

New Avalonia feature slices must execute their use cases through
`IToolExecutionService`; converting every WPF tool in this infrastructure step
would mix unrelated feature behavior into A6 and remove the working legacy
oracle too early. QuickRun selection, run-finished signaling, and global
hotkeys remain Wave 2 step 15.

## Automated coverage

`Mapping_Tools.Platform.Tests` verifies:

- successful typed output, off-UI-thread execution, ordered progress, success
  notification, and settings-controlled editor reload;
- typed failure and cancellation outcomes;
- duplicate-run rejection without invoking the second delegate;
- targeted, caller, and host-shutdown cancellation;
- joining all active operations before shutdown completes;
- invalid progress rejection and pre-publication notification cancellation;
- notification-subscriber failure isolation;
- singleton composition-root validation and hosted-service registration;
- a real Generic Host start/stop cycle reaching the execution shutdown hook.

The focused platform suite passes 84 tests. Application and Infrastructure
continue to build with the XML-documentation gate enabled. This step changes
application lifetime but no AXAML or visual state, so no render baseline
applies.

## Documentation consulted

Avalonia 12.1 application lifetime and shutdown behavior:

- <https://docs.avaloniaui.net/docs/fundamentals/application-lifetimes>
- <https://docs.avaloniaui.net/api/avalonia/controls/applicationlifetimes/classicdesktopstyleapplicationlifetime>

.NET Generic Host construction, dependency injection, hosted services, and
shutdown:

- <https://learn.microsoft.com/en-gb/dotnet/core/extensions/generic-host>
- <https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/how-to-use-host-builder>
