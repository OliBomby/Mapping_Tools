+++
title = "Hitsound Studio"
slug = "hitsound-studio"
+++

Hitsound Studio is the tool that lets you import data from multiple outside
sources and convert them to osu! standard hitsounds in the form of a
hitsounding difficulty that can you copy to other beatmaps.

It represents hitsounds as a list of layers (hitsound layers). One layer
contains a unique sound, the sampleset and hitsound that accompany that sound
and a list of times that sound has to be played.

Managing hitsounds
---

### Adding layers

When you click the plus button underneath the list there will appear a little
window for adding layers. There are different ways to add layers for each
import type.

- **Simple layer**. You can add a layer which doesn’t import times from any
  outside source if you just want to input the times via the gui.

- **Import stack**. You can import the times from a beatmap. This import type
  will look at one specific coordinate, column or row of a beatmap and get the
  times of all the objects on there for your hitsound layer. That way you can
  use the osu! editor to make the times in your hitsound layers.

- **Import hitsounds**.  You can import the complete hitsounding of a beatmap.
  This import type will look at every sample that plays on every object and
  generate multiple hitsound layers that would recreate the same hitsounds.
  This ignores slidertick and sliderslide hitsounds. When you import hitsounds
  with this mode it will also look for duplicate custom samples in the
  beatmap’s folder to avoid hitsound layers that have the same sound, sampleset
  and hitsound, but a different sound file. However it can’t detect samples
  that are made out of multiple other samples combined.

- **Import MIDI**. You can import MIDI’s for hitsounds. This import type will
  generate a layer for every unique sound the MIDI wants to play. It will also
  automatically fill in the arguments for SoundFonts.

It will automatically fill in the current beatmap for beatmap fields in the
importing window.

### Editing layers

Taking up most of the screen is a list that shows all the hitsound layers. You
can select one or more layers and edit their properties in the panel to the
left.

Double clicking a hitsound layer will make it play it’s sample, so you can
check what it sounds like. If it doesn’t play a sound then it probably couldn’t
find the sample.

In the editing panel you can edit the following properties:

- Name. It has no other use than letting yourself know what each layer is supposed to represent.

- Sample set. It defines which sample set will be used for the sample of the layer.

- Hitsound. It defines which hitsound will be used for the sample of the layer.

- Times. This is the list of times in milliseconds separated by commas which defines when the sample will be played. You can edit this in the tool, but it’s recommended to make all edits in the source of the layer and reloading the layer instead of editing the layer itself.

- Hitsound sample. A path to a file that contains the custom sound the hitsound must make. Supported file types are wave, vorbis and SoundFont. If you input a path to a SoundFont file there will appear extra inputs for generating sounds from a SoundFont. If Hitsound Studio can’t load the sample it will instead treat it like you want it to play the skin’s default sample, so no custom sound.

- Import info. This holds information that define what this layer is compared to the source of the layer, so you can reload the data from the source to the times of the layer. It’s like the layer’s identity for outside sources.

- Reload from source will re-do importing for all the selected layers and replace their times property will new values. This is based of of their import info.

You can use the up/down buttons underneath the list to move the selected layers
up/down and this actually serves an extra purpose.

Hitsound Studio treats the list as a hierarchy so the top layers have the most
priority and the bottom layers have the lowest priority. In case multiple
hitsounds mix at the same time it will take the sampleset of the layer with the
highest priority.

If you press the garbage can button underneath the list it will ask you to
confirm deletion and if you press yes it will remove the selected layers.
 
Using sounds from SoundFont
---

Inputting a path to a SoundFont file (.sf2) in the path for the hitsound sample
will make extra controls appear which allow you to generate the right sound out
of the SoundFont. You cannot use SoundFont for the default sample.

- **Bank**. This is the bank number to the bank to use. Usually bank 0 contains
  all the instruments and bank 128 contains all the percussion.

- **Patch**. This defines which patch to use. Patches are like different
  instruments. Here’s a helpful link that tells you the patch numbers
  corresponding to instruments: https://pjb.com.au/muscript/gm.html.

- **Instrument**. Sometimes a patch contains multiple instruments, so you can
  use this to give an index of which part of a patch to use.

- **Key**. This defines the key or pitch of the instrument. For the percussion
  bank this can also define which instrument to use instead of pitch. Hitsound
  Studio can pitch shift if the SoundFont doesn’t contain the exact right key.
  Here’s a helpful link if you don’t that tells you how key number translates
  to other stuff
  https://www.inspiredacoustics.com/en/MIDI_note_numbers_and_center_frequencies.

- **Length**. This defines the length in milliseconds to play a sample. If you
  input -1 here it will instead get the full length of the sound.

- **Velocity**. This is a value from 0 to 128 that defines how loud to generate
  a sound. SoundFont can have different samples for different velocities.
  Hitsound Studio will adjust volume for the velocity value. Inputting -1 will
  act like 128 velocity or 100% volume.

Inputting -1 for bank, patch, instrument or key will make it look through every
entry until it finds the first suitable sample, so that’s your best bet if you
don’t know which bank or patch etc to use.

Also remember you can double click the layer to hear what sound it makes.

Hitsound Studio adds a small fadeout extension to the sounds to make the ending
sound natural. If you input -1 into the length field it will not extend the
length but still keep the small fadeout.

Times are in seconds. It will extend by 0.4 seconds if the length is not
defined as -1 and then do the following fade:

```
If the length of the sample is less than 0.4 seconds:
  Fade start = length * 0.7;
  Fade length = length * 0.2;
else:
  Fade start = length - 0.4;
  Fade length = 0.3;
```

### Base beatmap

The base beatmap is a beatmap that contains all the timing, metadata and volume
changes for the hitsounding difficulty. Basically the hitsounding difficulty
will be exactly this map but with all objects and custom indices replaced. Also
mode will be set to osu! standard, circle size will be set to 4 and the
difficulty will be called “Hitsounds”.
 
### Default sample

You tell Hitsound Studio what sample and sample set to use for undefined
hitnormals.

Default sample is required because in osu! standard an object will always play
a hitnormal sample. For example if you told Hitsound Studio you want a whistle
at 10s with a certain sound without telling it what hitnormal to play at 10s,
then Hitsound Studio doesn’t know what sample to play for the hitnormal at 10s.
The default sample will fill that place. 

If Hitsound Studio can’t load the sample it will instead treat it like you want
it to play the skin’s default sample, so no custom sound.

Exporting hitsounds
---

Press the play button to run the algorithm and export the files to the exports
folder. When it’s done it will open the export folder filled with all the
files.

- One hitsounds beatmap that contains all the data of hitsounding and that you
  can copy from.

- A bunch of wav files that is all the necessary custom samples to properly
  play the hitsounds in the hitsounds beatmap. These wav files are encoded with
  16-bit PCM.

The algorithm takes the hitsound layers, base beatmap and default sample and
converts them to osu! standard hitsounds. It uses advanced algorithms to
generate the optimal custom indices that require the least amount of samples.
It also optimises to use the least amount of greenlines.

Press the question mark button to run the algorithm without exporting any files
and just display the number of custom indices, samples and greenlines it would
have.
 
### Mixing samples

To make osu! standard hitsounds it sometimes has to mix multiple samples
together into one sample.

To prevent clipping of audio it scales volume with the following formula:

```
Volume = 1 / Sqrt(number of samples * average volume)
```

Volume is a scale 0 to 1 and number of sounds is the number of samples that get
mixed into one sample.

The resulting sample will get the sample rate of the highest sample rate sample
out of the samples that make it up.

The resulting sample will be stereo if at least one of the samples that make it
up is stereo.
 
Saving projects
---

You can save and load your configuration by using the project menu in the top
bar and it will also automatically save your current configuration for when you
close Mapping Tools. That way you don't lose your work and it enables you to
work on multiple hitsound projects at the same time.
