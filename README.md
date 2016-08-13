mono-curses
===========

This provides both a low-level API, as well as a simple console
UI toolkit called `gui.cs`.

The goal of this library was to bind curses.  There is a
low-level binding in binding.cs and a few chunks in handles.cs
that provide a basic abstraction.

The focus of this work though has been on a simple GUI toolkit
for writing desktop applications, inspired on the 15 year old
work that I did for the Midnight Commander (you can tell I
like those colors). 

The work in `gui' does not take advantage of curses "WINDOWS"
or the Panel library as am not familiar with them, instead we
create our own abstraction here. 

License
=======

This is an ncurses binding licensed under the terms of the MIT X11
license.

Features
========

Detects window changes, invokes event for widgets to relayout if
the user wishes to.

Hotkeys (Alt-letter) are handled by buttons and a handful others.

Dialog boxes automatically get centered (even with window size change
scenarios).

Entry widget has emacs keybindings.

ListView widget uses Model/View setup.

Color and black and white support (first parameter to Application.Init)

TODO
====

* Rename x,y,w,h into something better, expose rects?
* Merge Widget and Container?
* Add scrollbar and thumb to listviews
* Add text view widget
* Add scrollable control
* Checkbox/Radio button are missing
* Date/Time widget
* Process widget
* Command line parsing to demo (to active B&W support).
* Implement Layout managers, which?
* Write a manual/tutorial


