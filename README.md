# Important note about this repo

Don't use it! Or rather - it's a big mess, will take a long time to open up and is full of junk. This was the original repo for Polyhydra and has become a bit unwieldy as it containts every experiment and alternative UI I ever tried.

I moved the core code to https://github.com/IxxyXR/Polyhydra-upm which also contains some fairly concise examples. It's probably best to start there if you want to play with the procedural generation functionality and you're comfortable with a bit of light C#

# Polyhydra

![Screenshot](https://pro2-bar-s3-cdn-cf1.myportfolio.com/1e3b6316-da77-4fd2-a111-e12070c11b10/2977d391-d8a0-4759-8f3b-fe112b8957b8_rwc_0x22x975x549x975.png?h=f2ff1682c51247d1bc76e926872686e2)

A toolkit for the procedural generation of geometric forms in Unity. The above image is from a VR piece I made using it called "Gossamer" that is currently exhibited in the Museum of Other Realities: https://andybak.net/gossamer

* YouTube playlist: https://youtube.com/playlist?list=PL94EgLgEIJyJQh_nB-CvSKbXjNU0ojNqC
* Gallery: https://andybak.net/polyhydra


# Getting Started

1. Download or clone the repo.
2. Open it via Unity Hub in an appropriate Unity version (same or slightly newer should be fine)
3. Open any of the example scenes and have a play


# Try it on the web

This is one possible UI that I started working on: http://www.polyhydra.org.uk/media/fastui/ Only a prototype but a quick way to get an intro into one possibly application of this toolkit. Keyboard controls listed onscreen Experimental and a bit buggy. Save often...

NOTE! - keyboard controls only in this particular web version. (It's keyboard only because it was a variation on a UI I created originally using MIDI devices which was a really nice tactile way to create - but difficult to share with people who didn't own the same MIDI controller as you - hence this version. There is a more conventional mouse+keyboard UI here: https://andybak.itch.io/polyhydra - but it's more clunky and less immediate )

# Features

Basic workflow is:

1. Start by generating a base 3d shape
2. Apply a Conway operator or similar modifier to some or all of the faces based on some rules
3. Repeat
4. Gasp in wonder at the beauty of your creation.

* All (but one) Uniform Polyhedra using the Wythoff construction
* Johnson(-esque) polyhedra - Prisms, pyramids, cupolae, rotundae 
* Most Conway operations are implemented and are parameterized and chainable. Can be applied to a subset of faces based on simple filters or complex rules
* Regular tilings of the plane with various deformations 
* De Bruijn multigrids
* A port of the Isohedral tilings from tactile.js


# Credits

As far as possible I'd like to licence this under the MIT licence or similar but the code has a complex heritage. 

Obviously the original work by Willem Wythoff and John Conway. And also countless other mathematicians who have formed a base for, contributed to and extended the work in this area. A special shout out to George Hart who is often co-credited with Conway due to the large amount of work he did exploring and extending Conway's original operators. 

The actual Wythoff code was based on https://github.com/kaonasi (which in turn is based on the work of Zvi Har’El: http://www.math.technion.ac.il/S/rl/kaleido/ ).

(Zvi Har'El has sadly passed away. I've tried to contact all potential copyright holders to see if it's OK to make use of their work as a basis for this but I've had no luck in getting a response. Please get in touch if you're an interested party.

Conway operator code was based on work by Will Pearson @mcneel which can be found here: https://github.com/pearswj/buckminster

Again - I tried to get in touch and didn't get a response. I'm not sure what the intended licence of that code is. It seems to be a standard copyright attribution but I wonder if this is an oversight more than the real intention of the author.

Multigrids is ported from work by Wolthera van Hövell tot Westerflier for https://github.com/kde/krita - they generously agreed that my version could be MIT licenced.

Portions of grids.cs is from Antiprism and is MIT but should be attributed to Adrian Rossiter and Roger Kaufman: https://github.com/antiprism/antiprism/blob/master/COPYING

Isohedral tilings are from tactile.js https://github.com/isohedral/tactile-js Thanks to Craig Kaplan @TriggerLoop

My original inspiration was 3DS Max's Hedra plugin which kept me entertained for quite a while nearly 2 decades ago. I think credit for that is due to Tom Hudson :-)

![Screenshot](https://github.com/Ixxy-Open-Source/wythoff-polyhedra/blob/master/0.png)

