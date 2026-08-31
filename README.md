# Mapping Tools [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/V7V2HPH5F) [![Github All Releases](https://img.shields.io/github/downloads/OliBomby/Mapping_Tools/total.svg)]()

Mapping Tools is a collection of tools which help you create osu! beatmaps more easily!
[Website](https://mappingtools.github.io/)

The shipped application is the Avalonia desktop frontend. Releases provide
self-contained Windows x86/x64 ZIPs and Inno Setup installers, plus self-contained
ZIPs for Linux x64/arm64 and macOS x64/arm64; macOS archives contain a standard
`Mapping Tools.app` bundle. The
historical Windows updater assets remain available as `release.zip` and
`release_x64.zip`; the canonical assets use deterministic OS/architecture
names such as `mapping-tools-linux-x64.zip`. The installers are Windows-only.
Development launches should use `Mapping_Tools.Desktop/Mapping_Tools.Desktop.csproj`.

The core beatmap tools, project persistence, file dialogs, editing, audio
decoding/export, and MIDI file workflows run on all three desktop platforms.
Linux preview playback requires one of `paplay`, `aplay`, or `ffplay` to be
installed. osu! live-memory/editor integration, Geometry Dashboard overlays,
global hotkeys, and automatic BetterSave remain Windows-only because they use
Win32 and osu! stable process interfaces; those adapters are isolated so they
do not prevent the portable tools from starting.

The legacy WPF/WinForms frontend has been removed after the Avalonia migration.
Existing settings, project JSON, maps, backups, exports, and updater handoff
remain compatible with the shipped application.

<p align="left">
  <img src="https://i.imgur.com/7JqvlNY.png" alt="Mapping Tools logo"/>
  <br/>Logo by <a href="https://osu.ppy.sh/users/1882522">Karoo</a>
</p>

## Tools included
- [Map Cleaner](https://github.com/OliBomby/Map-Cleaner) by [OliBomby](https://github.com/OliBomby) 
- Slider Merger by [OliBomby](https://github.com/OliBomby) 
- Slider Completionator by [OliBomby](https://github.com/OliBomby) 
- Hitsound Studio by [OliBomby](https://github.com/OliBomby) 
- Property Transformer by [OliBomby](https://github.com/OliBomby) 
- Timing Helper by [OliBomby](https://github.com/OliBomby) 
- Hitsound Copier by [OliBomby](https://github.com/OliBomby) 
- Hitsound Preview Helper by [OliBomby](https://github.com/OliBomby) 
- Metadata Manager by [OliBomby](https://github.com/OliBomby)
- Timing Copier by [OliBomby](https://github.com/OliBomby)
- Rhythm Guide by [OliBomby](https://github.com/OliBomby)
- Geometry Dashboard by [OliBomby](https://github.com/OliBomby) | [CrazyRabbitKGe](https://github.com/CrazyRabbitKGe)
- Combo Colour Studio by [OliBomby](https://github.com/OliBomby)
- Sliderator by [OliBomby](https://github.com/OliBomby) | [Karoo](https://github.com/Karoo13) | [JPK314](https://github.com/JPK314)
- Pattern Gallery by [OliBomby](https://github.com/OliBomby)
- Mapset Merger by [OliBomby](https://github.com/OliBomby)
- Tumour Generator 2 by [OliBomby](https://github.com/OliBomby)
- Slider Picturator by [JPK314](https://github.com/JPK314)

## Future implementations
See the [Trello board](https://trello.com/b/iTmmw3eP/mapping-tools).

## Used libraries
- [Avalonia](https://github.com/AvaloniaUI/Avalonia)
- [Material.Avalonia](https://github.com/AvaloniaCommunity/Material.Avalonia)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- [NAudio](https://github.com/naudio/NAudio)
- [NAudio Vorbis](https://github.com/naudio/Vorbis)
- [OsuMemoryDataProvider](https://github.com/Piotrekol)
- [Editor Reader](https://github.com/Karoo13/EditorReader)
- [NonInvasiveKeyboardHook](https://github.com/kfirprods/NonInvasiveKeyboardHook)
- [Overlay.NET](https://github.com/lolp1/Overlay.NET)
- [.NET Ogg Vorbis Encoder](https://github.com/SteveLillis/.NET-Ogg-Vorbis-Encoder)
- [Onova](https://github.com/Tyrrrz/Onova)
