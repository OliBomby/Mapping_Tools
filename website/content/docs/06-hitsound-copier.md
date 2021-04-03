+++
title = "Hitsound Copier"
slug = "hitsound-copier"
+++

Copies hitsounds from A to B.

There are 2 modes. First mode is overwrite everything. This will basically
first remove the hitsounds from the map you’re copying to and then copy the
hitsounds.

Second mode is copying only the defined hitsounds. A defined hitsound is when
there is something there in the map you’re copying from. This mode will copy
over all the hitsounds from the map you’re copying from. Anything in the map
you’re copying to that has not been defined in the map you’re copying from will
not change. For instance muted sliderends will remain there.

Temporal leniency is the maximum amount milliseconds two hitsounds can be away
from each other in the two maps to still copy over the hitsounds.

The temporal distance between the two hitsounds has to be less than or equal to
the temporal leniency.

Sliderbody hitsounds are greenlines in the body of a slider that change custom
index or volume during the slider.
 
Copying storyboarded samples
---

To copy storyboarded samples simply check the checkbox that says "Copy
storyboarded samples".

It will add the storyboarded samples from the map you are copying from to the
map you are copying to.

If the map you are copying to already contains an exact copy of a storyboarded
sample in the map you are copying from then that storyboarded sample will not
be added.

If the overwrite everything copying mode is selected then it will first remove
all the storyboarded samples of the map you are copying to and then copy.

Checking the box of "Ignore hitsound satisfied samples" will make the program
try to not copy over storyboarded samples for samples that already get played
by the hitsounds.

Temporal leniency also applies here.

If a hitsound plays sample X at time Y then the storyboarded sample of X at
time Y +/- temporal leniency will not get copied over.

The algorithm also looks for files that have different names/locations but have
the same sound, so you don't have to worry about your storyboarded samples
using different files.

However the algorithm can't detect if there are samples made out of multiple
sounds mixed together. It will treat those as unique sounds.

Muting Sliderends
---

You can enable sliderend muting in Hitsound Copier by checking the mute
sliderends checkbox. Despite the name this muting also applies to spinnerends.

Only sliderends that are not defined will be eligible to get muted. If you want
to ignore that filter you can just copy from an empty map with the overwrite
only defined hitsounds option.

Sliderends from repeating sliders won’t get muted.

You can configure the minimum snap that the sliderend must have to be eligible
to get muted.

You can also configure the minimum length the slider must have to be eligible
to get sliderend muting. The time you input is in number of beats. If the
textbox for the minimum length can’t be parsed then it will default to 0.

You can configure how the program mutes sliderends. Under muted config the
textbox is to configure what customindex gets given to muted sliderends. If the
textbox can’t be parsed then it will not change the custom index on the
sliderend. You can also tell the program what sampleset the muted sliderend
must be.

In addition to the stuff you configured, all muted sliderends will have 5%
volume, auto for additions and no hitsounds.


