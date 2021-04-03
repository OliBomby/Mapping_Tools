+++
title = "Property Transformer"
slug = "property-transformer"
+++

Multiple and add to properties of all the timingpoints, hitobjects, bookmarks
and storyboarded samples of the current map.

The new value is the old value times the multiplier plus the offset. The
multiplier is the left textbox and the offset is the right textbox. The
multiplier gets done first.

Resulting values get rounded if they have to be integer.

If the multiplier can’t be parsed then it will default to 1.

If the offset can’t be parsed then it will default to 0.

Clipping properties
---

Clip properties will make the properties not go outside of bounds. Properties
that have 1x multiplier and 0 offset will not get clipped. The clipping works
with the following boundaries:

- Timingpoints offset: None
- Timingpoints BPM: 15 - 10000
- Timingpoints SV: 0.1 - 10
- Timingpoints custom index: 0 - 100
- Timingpoints volume: 5 - 100
- Hitobject time: None
- Bookmark time: None 
- SB sample time: None 
 
Filters
---

Filters let you direct the things that get targeted by Property Transformer. 

The first text box is for matching. All parameters that match that value are
able to get transformed. This filter will only be active if the enable filters
checkbox is checked and the textbox can be parsed to a number.

The next filter is a time range. To get targeted by Property Transformer, the
object that holds the parameter must have a time greater than or equal to the
first value and less than or equal to the second value. The times specified are
in milliseconds.

If one of the textboxes can’t be parsed it will default to negative infinity
for the minimum time and positive infinity for the maximum time.

The filter is only active if one of the two textboxes can be parsed and the
enable filters checkbox is checked.
