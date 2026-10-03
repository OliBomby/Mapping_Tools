---
name: translate
description: Use when creating translations or localized strings in the Desktop or Application projects.
---

# Translator glossary

Mapping Tools translates interface text only. Dates, numbers, expressions, file
formats and saved identifiers keep their existing syntax. English is the fallback
for missing translations.

## Names that stay unchanged

Keep **every mapping tool name** exactly as written, including descriptive names
such as Hitsound Copier and Timing Helper. Also preserve **Mapping Tools**,
**QuickRun**, **SmartQuickRun**, **QuickUndo**, **BetterSave™**, and external names
such as **osu!**, **osu!lazer**, **Editor Reader**, **MTIPC**, **gosumemory**, **Tosu**
and **GitHub**. Translate their descriptions and surrounding prose.

Never translate usernames, user-authored metadata, file paths, filenames,
extensions, configuration keys, tool IDs, formulas or keyboard shortcuts.
**Songs** is a literal directory name; Dutch can say "Songs-map".

Audio sample-bank identifiers **Auto**, **Normal**, **Soft**, **Drum** and hitsound types
**Normal**, **Whistle**, **Finish**, **Clap** keep their osu! names. MIDI, PCM, IEEE,
Vorbis, SoundFont and file extensions also stay unchanged.

## Mapping vocabulary

Use the corresponding **osu!lazer editor translation** as the first reference,
checking its context. The source is the official
[osu! resources repository](https://github.com/ppy/osu-resources/tree/master/osu.Game.Resources/Localisation).
For example for Dutch, consult [Editor.nl.resx](https://github.com/ppy/osu-resources/blob/master/osu.Game.Resources/Localisation/Editor.nl.resx),
[EditorSetup.nl.resx](https://github.com/ppy/osu-resources/blob/master/osu.Game.Resources/Localisation/EditorSetup.nl.resx)
and [EditorDialogs.nl.resx](https://github.com/ppy/osu-resources/blob/master/osu.Game.Resources/Localisation/EditorDialogs.nl.resx).
For other languages search for the relevant language tag.

# Text localization guide

- Language selection lives in Preferences.
- Core and Infrastructure diagnostics stay language independent; translated explanations are shown at the presentation boundary and the original exceptions remain available as technical details.
- Each project with translatable text has one resource file per language.
- Each mapping tool should have its own prefix for resource keys, e.g. "MapCleaner_Help"
- When adding new strings, make sure to keep the other languages up to date.
- Views use `{loc:Tr Key}` for live text.
- MSBuild generates public `[ProjectName]Strings` classes with strongly typed getters for each entry. Use this auto-generated getter whenever possible to avoid maintaining expensive wrappers.
- Use `ToolChoiceConverter` for displaying localized string values for enum data. 
- Plugins should call `TranslationManager.RegisterResources(static culture => PluginStrings.Culture = culture)` once before using their generated resource class.
- To add a language, add culture-suffixed catalogs beside the English resources, extend supported language normalization in `TranslationManager` and the native language choices in Preferences.
- Resource comments must identify the actual screen or workflow, disambiguate terms, describe each placeholder's value/type/unit, and state applicable names or syntax to preserve. Include relevant context only; a filename or "translate this label" is insufficient. Update comments when behavior changes and copy the same context into translated catalogs for reviewers.
