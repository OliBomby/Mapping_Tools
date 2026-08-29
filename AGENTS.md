# Mapping Tools

Mapping Tools is an open-source project that provides a set of external tools that enhance or automate various tasks in osu! mapping.
It exists next to an osu! editor and does not aim to replace the editor itself.

## What makes Mapping Tools special?

Here's a brief list of things we can never compromise on:

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

## XML documentation standard

Every public or protected API added to production projects
must have meaningful XML documentation. This includes types, constructors,
methods, properties, fields, events, delegates, operators, enum types and enum
members. Document parameters, type parameters, return values, exceptions, and
important platform or cancellation behavior where applicable. Prefer a
specific summary of the contract or behavior over restating the identifier.
Never generate documentation mechanically from a symbol name or signature.
Read the implementation and relevant call sites before writing each comment,
and make the documentation add information that the identifier does not:
units, ranges, invariants, ordering, ownership, mutation, side effects,
fallbacks, format compatibility, cancellation, or failure behavior.
Use `<inheritdoc/>` only when an inherited or implemented contract already
describes the member accurately.

All test projects are exempt. `Directory.Build.targets` generates documentation files
and treats CS1591 as an error for every other project. Do not suppress CS1591;
build every affected non-test project and resolve the diagnostic before
completing a migration.

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

## Layer boundaries

The project adheres to Domain Driven Design.

- Any code that is used solely for the frontend belongs in the Desktop project. It may not exist in the Application or Core projects.

## Avalonia migration standard

The legacy WPF implementation is the normative specification for every migrated view. Read the WPF XAML, code-behind, view model, converters, and custom controls before editing the Avalonia version.

## Original project reference

The original project is available locally at [.reference/Mapping_Tools-Original](.reference/Mapping_Tools-Original/). Consult it for legacy implementation details.
Another Avalonia port of this program was made by NiceAesth. It is available locally at [.reference/NiceAesth-Mapping_Tools](.reference/NiceAesth-Mapping_Tools). Consult it if asked.

- Keep the AXAML structurally identical to the WPF XAML. Only replace WPF-only controls or properties, move non-visual behavior out of code-behind, modernize view models, and substitute the approved shared tool controls.
- Do not paraphrase copy, remove tooltips, change spacing, invent validation limits, add commands, or redesign interactions during a migration. Treat product improvements as separate work requiring explicit approval.
- Inventory every legacy binding, converter, event handler, command, validation rule, tooltip, context-menu item, dialog, and completion/error branch. Preserve all behavior that belongs to the current migration wave.
- Consult `docs/avalonia-migration/feature-dependency-graph.md` before treating absent behavior as a violation. Behavior explicitly assigned to a later wave is deferred scope, not part of the current view migration.
- Preserve shell-owned behavior such as feature scrolling. Add view-owned scrolling only when the WPF view itself owns a specialized inner scroller.
- Keep a custom control's styles in its control AXAML or a co-located control-owned style file. Keep view-only styles in the view and shell-only styles in the shell.
- Put application-wide Material compatibility overrides in focused dictionaries under `Mapping_Tools.Desktop/Resources/Styles`. `App.axaml` composes those dictionaries and global resources; it must not become the owner of unrelated component styles.
- Record any unavoidable platform substitution in the migration notes. An unapproved difference blocks completion.
- Verify migrations with a minimal WPF-to-Avalonia source diff, focused behavior tests, and builds of both affected frontends. Do not use the WPF/Avalonia PNG renderer as migration acceptance evidence.
