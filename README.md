# Unity Rendering Latency Demo

Visually demonstrates the effects of Unity's rendering latency settings.

**Update:** Now also usable in Godot! (C#/.NET required)

This is intended to help developers decide which defaults to use
and which options to expose to users via settings.

Official documentation:
* [QualitySettings.vSyncCount](https://docs.unity3d.com/ScriptReference/QualitySettings-vSyncCount.html)
* [QualitySettings.maxQueuedFrames](https://docs.unity3d.com/ScriptReference/QualitySettings-maxQueuedFrames.html)
* [Application.targetFrameRate](https://docs.unity3d.com/ScriptReference/Application-targetFrameRate.html)

**Note:** This currently only runs on Windows and Linux as it uses
OS-specific functions to manipulate the mouse position.

## Quickstart

1. Open the project in Unity
1. Build and run (do not run in editor)
1. Press the number keys to test preset configurations

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

The grey bands indicate optimal performance with the given settings.
If your mouse is to the right of the grey bands, the rendering implementation
is adding latency beyond the theoretical minimum.

## Recommendations

*Disclaimer: These recommendations are based on my own test results
on my own hardware and with my application requirements in mind.
Please consider your own requirements and test on your target platforms.*

### Unity 2020.2.x and higher

* QualitySettings.vSyncCount = 1 (configurable)
* QualitySettings.maxQueuedFrames = 1-2 (configurable)

Unity's default settings (vSyncCount = 1, maxQueuedFrames = 2)
provide good latency and stability on most platforms.
If you are aggressively pursuing low latency, consider setting
maxQueuedFrames = 1 by default, but be aware that users with
less powerful machines may end up dropping frames.
Users may wish to set vSyncCount = 0 if they are willing to accept tearing.

See also: https://blog.unity.com/technology/fixing-time-deltatime-in-unity-2020-2-for-smoother-gameplay-what-did-it-take

> Warning: Setting QualitySettings.maxQueuedFrames to 1 will
> essentially disable pipelining in the engine, which will make it
> much harder to hit your target frame rate. Keep in mind that
> if you do end up running at a lower frame rate, your input latency
> will likely be worse than if you kept QualitySettings.maxQueuedFrames
> at 2. For instance, if it causes you to drop to 72 frames per second,
> your input latency will be 2 * 1⁄72 = 27.8 ms, which is
> worse than the previous latency of 20.82 ms.
> If you want to make use of this setting, we suggest you add it
> as an option to your game settings menu so gamers with fast hardware
> can reduce QualitySettings.maxQueuedFrames, while gamers with
> slower hardware can keep the default setting.

### Unity 2019.x to 2020.1.x

**If possible, upgrade to 2020.2.x or higher.**

If unable to upgrade:

* QualitySettings.vSyncCount = 1 (configurable)
* QualitySettings.maxQueuedFrames = 1

Unity 2019.x to 2020.1.x appears to queue an additional frame,
so you can safely ship with maxQueuedFrames = 1 to offset this.
Users may wish to set vSyncCount = 0 if they are willing to accept tearing.

### Unity 2018.x

**If possible, upgrade to 2020.2.x or higher.**

Unity 2018.x has high rendering latency when VSync is enabled
irrespective of QualitySettings.maxQueuedFrames,
and moderate rendering latency when VSync is disabled.
Do not build latency-sensitive games with this version of Unity.
