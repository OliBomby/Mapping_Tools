# osu! stable injected IPC and overlay proof of concept

This standalone Windows experiment loads a managed bridge into the running
32-bit osu! stable process. Localhost IPC can reload its active editor or add,
move, inspect, and remove a circle rendered by osu! itself. It does not simulate
input, focus a window, patch methods, or modify osu!'s executable or beatmap
files. It is outside the Mapping Tools solution, on `mtipc-injector` based on
`master`.

## Verified result

On 10 October 2026, the bridge was injected into the already-running process
60492. A `probe` request confirmed that the current screen was the editor and
that the callback ran on osu!'s scheduler thread (managed thread 1). A subsequent
`reload` request returned:

```text
reloaded=true objectManagerReplaced=True
```

The direct reload method returned `true` and replaced the editor's object
manager. The client remained running and responsive. The listener was stopped
after the experiment. Logs are in `bin/bootstrap.log` and `bin/bridge.log`.

The next experiment uses a cyan built-in approach-circle sprite in the editor's
UI sprite manager, which is drawn after the editor modes. IPC controls its
centre and radius in 512x384 beatmap coordinates. Position and radius use osu!'s
own current gamefield transform, refreshed during the sprite's update callback.
The circle ignores clicks and contains no hit object or beatmap data.

Verified in process 60492: a circle at (256, 192), radius 48, appeared in osu!'s
own framebuffer. A `move 384 192` request moved it to the right, confirmed by a
second framebuffer capture. `clear` reported `removed=True` after 1392 sprite
updates, followed by `status` returning `overlay=none`. The client remained
responsive. This overlay test did not reload or save the beatmap.

The final build was also checked with corrected-colour captures at the centre,
after movement, and after removal. A negative radius was rejected without
replacing the existing circle. Stopping the listener with a circle still active
removed it automatically (`removed=True`), deleted the endpoint file, and left
osu! responsive. These captures are local ignored files:
`bin/overlay-center-verified.png`, `bin/overlay-moved-verified.png`, and
`bin/overlay-cleared-verified.png`.

## Build and run

`build.cmd` uses this host's Visual Studio 2022 Build Tools x86 compiler and the
Windows .NET Framework compiler. All outputs stay in the ignored `bin` folder.

```powershell
.\build.cmd
.\bin\Injector.exe <osu-process-id>
.\bin\Client.exe <osu-process-id> probe
.\bin\Client.exe <osu-process-id> circle 256 192 48
.\bin\Client.exe <osu-process-id> status
.\bin\Client.exe <osu-process-id> capture
.\bin\Client.exe <osu-process-id> move 384 192
.\bin\Client.exe <osu-process-id> clear
.\bin\Client.exe <osu-process-id> reload
.\bin\Client.exe <osu-process-id> stop
```

Use the ID of the intended osu! stable process. The injector checks the
executable name and architecture. The bridge rejects assemblies whose module
version ID does not match the inspected build. Probe before reloading.
Reload discards unsaved editor state, as the usual Ctrl+L/Enter operation does.

The build gives each managed assembly a fresh filename and records it in
`bin/bridge-assembly.txt`. The bootstrap reads that filename. This allows a
rebuilt experiment to load into the same process: assemblies in the existing
default AppDomain cannot be unloaded or replaced in place. Stop the old listener
before injecting another build. Reinjecting the same build does not restart its
stopped listener; rebuild or restart osu! first.

The native bootstrap loads the managed bridge through the already-started CLR
v4 hosting API and then unloads itself. The bridge listens on an ephemeral
loopback TCP port, with a random token recorded in `bin/endpoint-<pid>.txt`.
The listener expires after five minutes or stops immediately on `stop`.
It removes its circle and detaches the sprite update callback on shutdown.
A scheduler check every 250 ms also removes the circle when its editor or
sprite manager changes. The managed assemblies remain loaded until osu! exits.
No global rendering hooks are installed.

`status` exposes sprite-list membership, visibility, update count, render
geometry, colour, and texture state. Window captures can remain stale while
osu! is in the background. `capture` calls osu!'s existing read-only framebuffer
helper and saves `bin/framebuffer.png`, correcting the helper's swapped red and
blue channels. It does not run osu!'s normal screenshot action or its upload
path. The helper internally encodes JPEG, so this diagnostic PNG retains those
compression artifacts.

## Inspected build profile

The profile is intentionally specific to the current executable:

```text
MVID: aa9db4d0-6aa6-4bb7-2f5e-8c1ae12ad43a
040013A0: game scheduler singleton
06002F6F: enqueue callback (force scheduling = true)
06002F6D: check scheduler thread
04001396: active screen instance
040022D5: editor singleton
040022D8: editor beatmap manager
04001E71: current beatmap
0400172D: beatmap file name
040022D7: editor object manager
06003655: editor reload(beatmap, confirmation, reloadAudio)
040022FA: editor UI sprite manager (drawn last by 06003611)
06003F09: pSprite(string, Vector2, SkinSource, Origins, Fields)
06000A78: SpriteManager.Add(pDrawable)
06000A7F: SpriteManager.Remove(pDrawable)
06003F13/06003F14: add/remove pSprite.OnUpdate callback
04000754: SpriteManager.SpriteList
0400138C: GameBase.GameField
06003829: GameField.FieldToDisplay(ref Vector2)
0600381E: GameField.Ratio
06001DBC: GameBase.GetScreenShotStream()
```

The reload method was traced from the editor's Ctrl+L branch in method
`06003641`. Passing `false` for confirmation and audio reload performs the
existing reload operation without its confirmation dialog or audio reload.
The active-screen check and the reload call both run on osu!'s game scheduler.

The user-supplied 2016 source at
`D:\Osu! Dingen\osu! 2016 Source Code` guided the rendering investigation:
`osu!\GameModes\Edit\Editor.cs`, `osu!\Graphics\Sprites\pSprite.cs`,
`osu!\Graphics\Sprites\SpriteManager.cs`, `osu!\GameplayElements\HitObjectManager.cs`,
and `osu!\GameField.cs`. The source is an older build; the listed tokens were
checked against the current executable. No supplied source files are copied
into this repository.

The UI manager's gamefield sprite ratio is zero, and the hit-object manager
clears its sprite list every editor update. Therefore, the circle lives in the
UI manager using native screen coordinates converted by `GameField`, rather
than relying on either manager's hit-object scaling. Its sprite is marked
non-disposable so removing it cannot dispose a shared osu! texture.

`Inspect.cs` is a read-only dnlib metadata/IL inspection helper. Compile it
against dnlib 4.5.0 separately if inspecting another build. It supports default
reload-string searches, `types`, `keysummary`, `keys`, a hexadecimal method
token, and `refs:<hexadecimal-token>`. The copied executable and inspection
outputs used during this experiment are local ignored files in `bin`.

This is a custom minimal IPC protocol, not a full MTIPC server. A production
adapter would need reliable method discovery across osu! updates, connection
lifecycle handling, and validation on Wine/Linux/macOS. The successful Windows
reload experiment establishes the direct IPC-to-editor path. The overlay also
needs wider validation across client builds, editor modes, window resizing,
and fullscreen configurations before production integration.
