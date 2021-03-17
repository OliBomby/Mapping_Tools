+++
title = "Map Cleaner"
slug = "map-cleaner"
+++

It cleans the current map of useless greenlines and it also lets you do some
other stuff regarding the whole map.

Map cleaner cleans useless greenline stuff by storing all the influences of the
timingpoints and then removing all the timingpoints and then rebuilding all the
timingpoints in a good way. This means the greenlines automatically get
resnapped to the objects that use them.

In this process the map or hitsounds must not change in an unexpected way. If
that happens please let me know, so I can fix it.

Map has sliderbody volume changes: By checking this you tell map cleaner that
any volume changes inside the body of a slider are *intentional* and as such
will not be removed by map cleaner.

The same works for the other general options:

- **Resnap objects**: This will resnap all the hitobjects and resnap sliderends
  during the map cleaning process. The resnapping will be done to the snap
  divisor options.

- **Resnap bookmarks**: This will resnap all bookmarks with the default
  resnapping method. This is a separate option because some maps like to spam
  bookmarks at a high frequency and it would be a shame if normal resnapping of
  hitobjects destroyed your bookmarking masterpiece.

- **Remove muting**: This option removes all volume changes and custom index
  changes on hitsounds with 5% volume. If any timingpoint has 5% volume, then
  the volume change and custom index change on that will be removed.

- **Mute all unclickables**: This will put 5% volume on all hitsound events
  originating from hitobjects that do not involve active player input. Only
  circles, sliderheads and hold note heads will be audible. This can be used to
  get better insight on the rhythm of your map or how it plays.

Resnapping
---

Resnapping works by moving the time to the nearest tick on the timeline and
flooring it to integer. If there is a redline within 10 ms after the time of
the hitobject, it will snap to that redline instead. This is to prevent stuff
from resnapping to a tick 3 ms before the redline.

Resnapping sliderends is done in two different ways. First method works by
taking the duration of the slider and then changing that to the nearest
multiple of a snap divisor. This is similar to using ctrl+shift+S on a slider
in the vanilla editor. The second method is only used if there is a redline
inside the duration of the slider or up to 20 ms after the slider. This will
resnap the end time of the slider to the nearest tick using the previously
described method. Note that this creates sliders with an integer duration and
the exact length deviates with the rounding around the timeline ticks.

All spinner ends and hold note ends are also resnapped using this second
method.

If the map is in the osu! mania gamemode then resnapping will also resnap the
position of the notes to the middle of the columns and to Y = 192.

Timeline
---

Whenever you run Map Cleaner a little timeline will show all the changes the
program made to the timingpoints.

- Red line means a removed timingpoint
- Orange line means a changed timingpoint
- Green line means a timingpoint addition

If a timingpoint was just moved you will see a red line on the previous offset
and a green line on the new offset.

You can doubleclick any of the lines to go to that time in the editor.
