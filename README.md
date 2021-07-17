# Unity Rendering Latency Demo

Visually demonstrates the effects of Unity's rendering latency settings.

This is intended to help developers decide which defaults to use
and which options to expose to users via settings.

Official documentation:
* [QualitySettings.vSyncCount](https://docs.unity3d.com/ScriptReference/QualitySettings-vSyncCount.html)
* [QualitySettings.maxQueuedFrames](https://docs.unity3d.com/ScriptReference/QualitySettings-maxQueuedFrames.html)
* [Application.targetFrameRate](https://docs.unity3d.com/ScriptReference/Application-targetFrameRate.html)

## Quickstart

1. Open the project in Unity
1. Build and run (do not run in editor)

## How It Works

The application moves the mouse at a constant rate,
by default one pixel per millisecond.
It also draws coloured vertical lines spaced one frame apart
starting at the current position of the mouse cursor.

By observing the cursor's position relative to the lines,
you can see exactly how much rendering latency you have
(including above and below tear lines).

The colours correspond to the following frame latency values:

* White: 0 frames
* Green: 1 frame
* Yellow: 2 frames
* Red: 3 frames
* Magenta: 4 frames

Changing the settings using the number keys
allows you to directly compare their effects.
Adding additional frame time with the arrow keys allows you to
simulate slower machines (or more complicated projects) and see
how the application would respond when frames take longer to generate.

## Recommendations

*Disclaimer: These recommendations are based on my own test results
on my own hardware and with my application requirements in mind.
Please consider your own requirements and test on your target platforms.*

### Unity 2020.x and higher

* QualitySettings.vSyncCount = 1 (configurable)
* QualitySettings.maxQueuedFrames = 1-2 (configurable)

Unity's default settings (vSyncCount = 1, maxQueuedFrames = 2)
provide good latency and stability on most platforms.
If you are aggressively pursuing low latency, consider setting
maxQueuedFrames = 1 by default, but be aware that users with
less powerful machines may end up dropping frames.
Users may wish to set vSyncCount = 0 if they are willing to accept tearing.

### Unity 2019.x

* QualitySettings.vSyncCount = 1 (configurable)
* QualitySettings.maxQueuedFrames = 1

Unity 2019.x appears to queue an additional frame,
so you can safely ship with maxQueuedFrames = 1 to offset this.
Users may wish to set vSyncCount = 0 if they are willing to accept tearing.
Consider upgrading to 2020.x or higher to avoid the additional latency.

### Unity 2018.x

Unity 2018.x has high rendering latency when VSync is enabled
irrespective of QualitySettings.maxQueuedFrames,
and moderate rendering latency when VSync is disabled.
If you are concerned about latency, you should upgrade to 2019.x or higher.
