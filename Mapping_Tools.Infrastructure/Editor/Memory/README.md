# osu!stable editor memory

`StableEditorMemoryReader` is a source port of the editor discovery signature and
32-bit CLR layouts recovered from Karoo13's `EditorReader.dll` with
ILSpy 11.1. The original reader is by [Karoo13](https://github.com/Karoo13/EditorReader).
Its copyright and permission notice are preserved in `EditorReader.LICENSE.txt`,
which is also copied into application builds and publishes.

The port implements the state used by `LiveBeatmapSnapshot`: beatmap properties,
hit objects, selected flags, timing points, bookmarks, and editor time. Clipboard,
hover, and compose-tool queries from the original DLL are outside this contract.
Production no longer loads the DLL.

`EditorProcessMemory` supplies read-only memory access through `ReadProcessMemory`
and `VirtualQueryEx` on Windows, or `process_vm_readv` and `/proc/<pid>/maps` on
Linux. The equivalent low-level readers in ProcessMemoryDataFinder are internal,
so this adapter owns its process directly. It has no process-watcher task and can
be disposed after the final active read. Linux access is subject to the same
process-memory permissions as current-beatmap lookup.

The decoder always reads 32-bit target pointers, including addresses above 2 GiB.
It discovers the editor from memory without window titles and scans in overlapping
chunks to avoid allocating an entire heap mapping. It rejects incomplete reads,
checks list counts against array capacities, and checks collection versions and
object contents for changes before returning a snapshot. A failed snapshot is
retried once. These checks detect common concurrent edits; reading an external
process cannot provide an atomic snapshot.

New Linux settings enable live memory reading. Existing saved settings keep their
chosen mode; select memory reading in preferences if it was previously disabled.
The configured Songs path must be the native filesystem path, including when osu!
runs under Wine. Windows path separators in editor metadata are normalized.
The recovered layouts apply to Windows osu!stable under Wine, not native lazer.

Unit fixtures model the recovered CLR layout without a running editor. A temporary
Windows comparison against the original DLL verified every serialized snapshot
field (apart from independently sampled editor time) on an open map with 282
objects, 16 timing points, and 4 selected objects. Linux/Wine requires a separate
live verification run.
