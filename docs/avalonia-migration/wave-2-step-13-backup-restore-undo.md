# Wave 2, step 13: backup, restore, and undo

Status: implemented, 2026-07-25.

## Scope delivered

`IBeatmapBackupService` now owns backup policy and destructive restore
orchestration independently of dialogs, hotkeys, timers, and either desktop
frontend. It provides:

- ordered multi-file disk backups;
- a session backup that retains both the durable file and matching unsaved
  editor contents;
- periodic snapshots suppressed until serialized contents change;
- metadata-validated restore with an explicit mismatch override;
- QuickUndo using the newest retained backup;
- retention pruning by filesystem creation time;
- optional osu! reload only after a completed restore.

Backup reasons retain the established filename codes:

- automatic tool safety: `yyyy-MM-dd HH-mm-ss___map.osu`;
- explicit user backup: `yyyy-MM-dd HH-mm-ss_UB__map.osu`;
- periodic snapshot: `yyyy-MM-dd HH-mm-ss_PB__map.osu`;
- restore safety: `yyyy-MM-dd HH-mm-ss_RU__map.osu`;
- a live companion uses the historical `_2_` separator.

If the same source is protected more than once during one second, later
snapshots receive a `_C2_`, `_C3_`, and subsequent collision marker rather
than overwriting the first safety copy.

## Mandatory destructive-save safety

`IBeatmapEditingGateway.SaveAsync` now forces an automatic backup before
writing an existing beatmap or storyboard. This invariant is inside the
Application service instead of relying on each feature to remember a separate
static `BackupManager` call.

If the source file is missing, the backup directory is unavailable, copying
fails, or cancellation occurs, the editor is not saved and osu! is not
reloaded. The user's `MakeBackups` preference still controls ordinary
automatic requests, but cannot disable the mandatory pre-save snapshot.

Restores follow the same rule:

1. validate that the backup and destination exist;
2. compare metadata-derived osu! filenames unless the caller explicitly
   allows a mismatch;
3. protect the current destination with an `RU` snapshot;
4. replace the destination;
5. reload osu! only when requested and only after replacement succeeds.

The selected restore source and the new safety copy are protected from
retention pruning for the duration of the operation. Even a configured limit
of zero cannot delete the safety artifact required by the current overwrite.

`BeatmapBackupIncompatibleException` carries both metadata-derived filenames
so Wave 3's typed dialog can ask for an informed override without the
Application layer opening a message box.

## Infrastructure durability

`FileSystemBeatmapBackupStore` writes and copies through unique temporary
siblings. Only a completely flushed temporary file replaces its destination.
Cancellation, a missing source, or an interrupted write therefore leaves an
existing beatmap intact, and temporary files are removed on every failure
path.

Backup enumeration excludes temporary siblings, orders retained files by
creation time to preserve QuickUndo behavior, and uses deterministic filename
ordering for creation-time ties.

The Avalonia composition root registers `IBeatmapBackupStore` and
`IBeatmapBackupService` as desktop-lifetime singletons. The backup service
shares the application settings, text-file persistence, editor reload
adapter, and `TimeProvider` introduced in earlier Wave 2 steps.

## Compatibility and deliberate improvements

The new service retains legacy timestamp and reason-code naming, globally
newest QuickUndo selection, metadata filename validation, retention ordering,
and the ability to force user and safety snapshots when ordinary automatic
backups are disabled.

The following data-safety changes are intentional:

- same-second backups no longer overwrite one another;
- every restore first preserves the destination;
- restore replacement is atomic rather than a direct overwrite;
- pruning cannot remove files still required by the active operation;
- a periodic session writes one current serialized snapshot instead of the
  legacy temporary-file path accidentally producing a duplicate `_2_` copy.

The WPF `BackupManager` and dispatcher timer remain as compatibility code for
unmigrated tools. Migrated features must use `IBeatmapEditingGateway` for
destructive saves and inject `IBeatmapBackupService` for explicit backup,
restore, QuickUndo, and periodic policy. The facade remains available until
the final WPF consumers migrate, preserving the current application as a
behavioral oracle.

## Automated coverage

`Mapping_Tools.Platform.Tests` verifies:

- legacy-compatible reason codes and timestamp naming;
- preference-controlled and forced backups;
- missing-directory and cancellation failures without mutation;
- disk plus unsaved-session companion snapshots;
- per-map periodic content hashing and unchanged-state suppression;
- metadata mismatch rejection before any safety copy or overwrite;
- destination safety before restore and reload-after-replacement ordering;
- newest-file QuickUndo selection;
- retention pruning, including limits smaller than the active protected set;
- unique same-second names;
- mandatory backup-before-save and backup-failure save suppression;
- atomic physical copy/write replacement, temporary-file cleanup,
  enumeration ordering, and targeted deletion;
- desktop singleton registration and container validation.

The focused platform suite passes 72 tests. This step changes no view or
AXAML, so no render baseline applies.

## Follow-up and deferred work

Wave 2 step 14 now schedules periodic change detection as hosted background
work, coordinates cancellation, supplies frontend-neutral notifications, and
owns the Avalonia composition root through the .NET Generic Host. Typed
confirmation dialogs remain a Wave 3 concern, and QuickUndo's global hotkey
was connected in Wave 2 step 15.

Wave 3 step 19 will expose explicit backup, restore, QuickUndo, and backups
folder actions through the Avalonia shell using the existing file picker,
file reveal, and typed-dialog ports.

## Avalonia 12.1 documentation

No Avalonia API was introduced or changed. This step affects Application
contracts, filesystem Infrastructure, editing safety orchestration, tests,
and desktop DI registration only.
