# TowerUniteOCRAuto
Tower Unite automatic gambler that utilizes OCR to stop you from getting kicked.

## About

TowerUniteOCRAuto is a program that will allow you to gamble automatically while you watch YouTube or do homework. This program, unlike using a simple macro, allows you to not have Tower Unite be the focused window (it can be minimized/you can click on other things in your desktop and work on your pc while this runs), while also using OCR to allow you to not get kicked off.

Utilizes the Tesseract.Net.SDK by patagames.com (current: 1.9.2) and the Utilities, User32, Gdi32, and KeyboardHook classes (modified slightly) for lower level hooks.

## Current known bugs

Some characters in OCR are recognized as other characters. This is from Tesseract's training files so I'll have to get them figured out.

Z can look like a 2 | Also outputs "D2"

Sometimes, some letters don't get pressed (they are, but aren't recognized | Unconfirmed if my last update fixed it)

If you do get kicked, use lastSuccessfulBypassImageTaken.png to figure out what the actual key to press was, and then the last console entry on it to see what key was pressed.

## Current Todo

* Finish todo

* Grab more errors from OCR (Z is read as 2)

* Better cross-thread handling (make cancelable?)

* If user gets kicked off, notify user that they did get kicked off and then pause the program so they don't jump in place