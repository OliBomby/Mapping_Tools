# Wave 9, step 41: audio services

## Scope

This step implements I4 audio services from the feature dependency graph. It establishes a reusable audio boundary for decoding, generation, effects, playback, MIDI, SoundFont rendering, and WAV/Ogg export. Hitsound Studio layer import/reload/edit/preview/dialog/schema/export work remains step 42 and is intentionally not migrated here.

## Boundary and ownership

- `Mapping_Tools.Core` owns copied floating-point clip data, audio formats, the osu! volume curve, effect descriptions and processing rules, and neutral MIDI models.
- `Mapping_Tools.Application` owns audio ports and orchestration services. Contracts contain no NAudio, Vorbis, MIDI-library, playback-device, or UI types.
- `Mapping_Tools.Infrastructure` owns NAudio/NVorbis/OggVorbisEncoder-backed decoding, SoundFont rendering, playback, MIDI, WAV/Ogg encoding, and deterministic disposal of external resources.
- The WPF project remains runnable. `LegacyAudioPreviewAdapter` is an opt-in bridge for legacy preview callers; the existing Hitsound Studio code-behind remains untouched for step 42.

## Parity and unavoidable substitutions

The legacy audio semantics are retained, including the volume-to-amplitude curve, sample panning/pitch transforms, SoundFont bank/patch/instrument/key/velocity selection, looping and terminal fades, soft limiting, MIDI channel conventions, and Ogg sample-rate handling. The concrete playback implementation uses WASAPI and the file decoder uses Media Foundation for MP3, matching the existing Windows-oriented behavior; these are Infrastructure platform substitutions and are not present in Core/Application/Desktop contracts.

There is no SoundFont fixture in the repository's wave-0 fixtures. The renderer's missing-source failure is covered, while successful SoundFont rendering requires a representative `.sf2` fixture in a later validation pass.

## Verification

- Core, Application, Infrastructure, and Desktop production projects compile.
- Infrastructure tests: 62 passed, including real Ogg decode, WAV/Ogg round trips, generated effects, MIDI round trip, cancellation, SoundFont transform ownership, non-zero-origin tempo conversion, and SoundFont failure handling.
- Application tests: 132 passed.
- Desktop tests: 202 passed.
- Architecture tests: 3 passed; Core/Application forbidden-library checks pass.
- Legacy WPF frontend build: passed with existing repository warnings.
- Full Core test run: 146 passed in the current worktree; unrelated fixture and line-ending changes were left outside this step's scope.
