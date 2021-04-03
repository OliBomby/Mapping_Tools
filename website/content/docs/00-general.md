+++
title = "General"
slug = "general"
+++

Mapping Tools is meant to combine all the mapping tools you need into one
program.

It ensures reliable quality by using shared algorithms, automatic updating and
backups.

Mapping tools stores all it’s config files in a map inside Documents called
Mapping-Tools.

It stores the general configs and configs of individual tools separately.

The backups and custom Hitsound Studio saves also get saved there by default.

Current map is a global variable which is used by most of the tools. It’s a
path to the .osu file of a beatmap. This map will be the map that most tools do
their work on.

You can set the current map by clicking on File and then use either Open
beatmap or Open current beatmap. 

- Open beatmap will open a file browsing window that defaults to your osu!
  songs folder.

- Open current beatmap will read the memory of your osu! client to get the path
  to the beatmap that is currently selected in the osu! client. This will not
  work if osu! is closed, there are multiple osu! clients open or if the path
  to your osu! folder is configured incorrectly.

Generate backup will make a copy of the current map and store it in the backups
folder.

The backups folder is inside the folder where mapping tools stores all of it’s
configuration.

You can use Options > Open backups folder to easily open the backups folder.

Inside the Tools menu item you can find all the available mapping tools.

In the About menu item you can find some info about the contributors to mapping
tools and the version of mapping tools. There is also a link to the Github.

Get started
---

It shows info letting you know how to use a tool from mapping tools.

It shows a list of recent maps that were selected as current map paired with
the time they were last selected.

If you double click on one of the items in the list, it will select that map as
the current map.

Preferences 
---

You can find the preferences in the Options menu item. You can:

- Toggle dark mode
- Set the path to your osu! folder
- Set the path to your songs folder
- Set the path to your backups folder
- Toggle making automatic backups

Text boxes
---

All text boxes that want you to input some kind of number have a special way of
parsing the text. You can input the number in the form of an equation. For
instance “1 + 1/4” will be interpreted as 1.25. Both dots and commas will be
interpreted as decimal separators.

If the text cannot be interpreted then it will go to a default value. Most of
the time this default value will have the program completely ignore that
feature, so there will be no adverse and unexpected results.

All text boxes that want you to input a path have a folder button next to it
that you can use to summon a file browsing dialog.

All text boxes that want you to input a path to a beatmap also have a download
looking button next to it that when you click it will get the current map from
your osu! client and put the path in the text box just like how the get current
map works. 

Backups
---

Every time you run a tool that changes a beatmap a backup will automatically be
made and put in the backups folder. You can disable this in the preferences.
