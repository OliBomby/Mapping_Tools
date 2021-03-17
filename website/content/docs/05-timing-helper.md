+++
title = "Timing Helper"
slug = "timing-helper"
+++

Ever had to time a song with really inconsistent timing? Having to add
countless redlines for every slightly off-beat kick?

Timing Helper is meant to speed up your timing job by placing the redlines for
you. You only have to tell it where exactly all the sounds are.

How does it work?
---

What you do is place 'markers' exactly on the correct timing of sounds. These
markers can be hit objects, bookmarks, greenlines and redlines (configurable in
the GUI).

Timing Helper will then adjust BPM and/or add redlines to make every marker be
snapped.

It will only snap to the snap divisors configured in the GUI

To avoid spamming redlines on every marker you can give it a leniency with its
timing.

The leniency you input is the maximum distance in milliseconds a marker can be
from its timeline tick. The tool will then try to snap every marker within that
leniency by using the fewest redlines.

Timing Helper will round its BPM values to human-like values as if a human
actually typed in the BPMs.

Different roundings include:

- Whole numbers
- Halve numbers
- Tenths
- Hundredths
- Thousandths
- Exact BPM

The tool will choose the most rounded value that still gets the required
precision.

Unchecking 'Include redlines' will make the tool reconstruct all the redlines.
It will still use the redlines to determine the amount of beats in between
every marker, but it will remove all the redlines and then make new ones for
all the markers.

Only use this if you have enough markers to recreate the timing and you want to
optimise the redlines.

By default Timing Helper determines the amount of beats between every marker
automatically based on the amount of beats between the markers on the existing
timing.

Inputting a number of beats in the 'Beats between markers' box will make the
tool make timing in a way so there are the inputted amount of beats in between
every marker.

Only use this if you put a marker every beat or measure to get the overall
timing right in a very varying BPM song or if you just want troll timing.
