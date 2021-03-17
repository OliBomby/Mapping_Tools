+++
title = "Hitsound Preview Helper"
slug = "hitsound-preview-helper"
+++

Hitsound Preview Helper helps by placing hitsounds on all the objects of the
current map based on the positions of the objects. That way you can hear the
hitsounds play while you hitsound without having to assign them manually and
later import them to Hitsound Studio.

This tool is meant to help a very specific hitsounding workflow. If you
hitsound by placing circles on different parts on the screen and treat each
position as a different layer of hitsounds. For example using a mania map and
have each column represent a different sound.

You define a list of positions and their hitsounds (hitsound zones) using the
tool's GUI.

For each object the tool finds the nearest hitsound zone and applies the
hitsounds from that hitsound zone to the object.

Inputting -1 for a coordinate of the hitsound zone will make it act like that
coordinate could be any value. For example -1 in the X coordinate will make it
effectively a horizontal line and each object will calculate the distance from
it using only the Y coordinate.
 
Saving projects
---

You can save and load your configuration of hitsound zones and it will also
automatically save your current configuration for when you close Mapping Tools.
That way you don't lose your possibly large configurations and it enables you
to work on multiple hitsound projects at the same time.
