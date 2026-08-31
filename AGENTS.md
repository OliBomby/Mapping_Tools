# Mapping Tools

Mapping Tools is an open-source project that provides a set of external tools that enhance or automate various tasks in osu! mapping.
It exists next to an osu! editor and does not aim to replace the editor itself.

## What makes Mapping Tools special?

Here's a brief list of things we can never compromise on:

### Open source and free

Anyone can use and reuse Mapping Tools or components and contribute to it free of charge.

### Cross-platform and cross-client

You can use Mapping Tools on Windows, Linux, and MacOS, 
and interact with any version of osu! be it osu! stable in Wine or osu! Lazer.

### Simple and user friendly

Learning to use a tool should be as simple as randomly clicking buttons.
We also strive to make the tools quick to use, so it requires few clicks to run a tool and get the desired result.
It must be faster than doing the same task manually in the editor.

### Extensible

Adding new tools should be as seemless as possible. We can always add new tools to support new features.
Tools should be relatively independent of each other.

### Support creativity without compromise

Mapping Tools should support all beatmaps, even extremely old maps and crazy aspire maps which break the limits of the game.
We also do not put arbitrary hard limits on the tools, so if there is a value that could conceivably be used in a map, it should be allowed.

## Personal note from the author

I like ambitious ideas, simple systems, and software that feels obvious.
Do not preserve complexity just because it already exists.
Do not introduce machinery because it looks architecturally impressive.
Understand the real constraint, then fight for the smallest model that makes the correct behaviour unsurprising.

Channel both "The Grug Brained Developer" and "yagni". Fight scope creep.
Try to honor the dev's intent in both a minimal and realistic fashion.

## A small glossary

- **osu!** - A rhythm game where players click circles, slide sliders, and spin spinners to the beat of a song. The game has a large community of mappers who create beatmaps for others to play.
- **Map/Beatmap** - A file that contains the timing, hit objects, and other data for a chart in osu!. Beatmaps are created by mappers and can be played by other players.
- **Mapset/Beatmapset** - A collection of beatmaps, songs, backgrounds, storyboards, hitsound samples, and other assets that are grouped together.
- **Mapper** - A person who creates beatmaps for osu!.
- **Mapping** - The process of creating beatmaps for osu!.
- **Mapping Tool** - A software that helps mappers in some aspect of mapping. It can have multiple related functions.
- **Mapping Tools** - This project, a set of mapping tools in one application.

## Coding style

- Insert whitespace between logical blocks of code to improve readability.
- Test project file structure should always match that of the production project. If the file moves, the test file moves with it.
- One file may only contain one public type. Multiple public types must be split up into multiple files.
- Don't introduce aliases in using statements.
- Every public or protected API added to production projects must have meaningful XML documentation.

## Project boundaries

The project adheres to Domain Driven Design.

- Put pure models, value objects, calculations, and domain rules in `Mapping_Tools.Core`.
- Put feature use cases and OS abstraction interfaces in `Mapping_Tools.Application`.
- Put filesystem, osu!, audio, network, and platform adapter implementations in `Mapping_Tools.Infrastructure`.
- Put Avalonia views, CommunityToolkit.Mvvm presentation state, navigation, and UI-only adapters in `Mapping_Tools.Desktop`.

## Original project reference

The original project is available locally at [.reference/Mapping_Tools-Original](.reference/Mapping_Tools-Original/). Consult it for legacy implementation details.
Another Avalonia port of this program was made by NiceAesth. It is available locally at [.reference/NiceAesth-Mapping_Tools](.reference/NiceAesth-Mapping_Tools). Consult it if asked.
