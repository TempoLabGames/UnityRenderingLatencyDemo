#if UNITY_5_3_OR_NEWER || UNITY_5_2
#define UNITY
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
#if UNITY
using UnityEngine;
using UnityEngine.Rendering;
#else
using Godot;
using Environment = System.Environment;
#endif

#if UNITY
public class RenderingLatencyDemo : MonoBehaviour
#else
public partial class RenderingLatencyDemo : Node2D
#endif
{
#if UNITY
    public Texture pixel;
#endif

    private float pixelsPerMs = 1;
    private int additionalFrameTimeMs = 0;
#if UNITY
    private Color[] colors = { Color.white, Color.green, Color.yellow, Color.red, Color.magenta };
    private Color idealColor = Color.gray;
    private Color idealBorder = Color.black;
    private List<FullScreenMode> modes = new() { FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.Windowed };
#else
    private Color[] colors = { Colors.White, Colors.Green, Colors.Yellow, Colors.Red, Colors.Magenta };
    private Color idealColor = Colors.Gray;
    private Color idealBorder = Colors.Black;
    private List<DisplayServer.WindowMode> modes = new() { DisplayServer.WindowMode.Fullscreen, DisplayServer.WindowMode.ExclusiveFullscreen, DisplayServer.WindowMode.Windowed };
#endif

    private Thread thread = null;
    private bool running = false;

    private string profileName = "<press number keys>";

#if UNITY
    private string hudText = "";
    private Rect rect = new Rect(0, 0, Screen.width, Screen.height);
    private GUIStyle style;
    private FullScreenMode displayedFullScreenMode;
#else
    private Label label;
    private string engineVersion;
#endif

    private int screenX = 0;
    private int screenWidth = 1080;
    private float mouseX = 0;
    private float mouseY = 0;

    // Ideal frame latency for a given configuration.
    // If minIdeal == maxIdeal, expect stable cursor position with no tearing.
    // If minIdeal != maxIdeal, expect unstable cursor position varying above and below tear lines.
    private int? minIdealF;
    private int? maxIdealF;
    private int? minIdealW;
    private int? maxIdealW;

#if UNITY
    private bool FullScreen => Screen.fullScreen;
#else
    private bool FullScreen => DisplayServer.WindowGetMode() >= DisplayServer.WindowMode.Fullscreen;
#endif

    IntPtr xDisplay;
    uint xRootWindow;

#if UNITY
    void Start()
#else
    public override void _Ready()
#endif
    {
        var platforms = new bool[] { Platform.IsWindows, Platform.IsLinux };
        var matched = platforms.Where(x => x).Count();
        if (matched == 0)
            throw new Exception("Currently only support Windows and Linux");
        if (matched != 1)
            throw new Exception("Ambiguous platform (check Platform class)");

#if UNITY
        style = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 20,
            normal = new GUIStyleState
            {
                textColor = Color.white,
            },
        };
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
#else
        KeyCode.AddActions();
        label = new Label();
        label.Position = new Vector2(10, 10);
        AddChild(label);
        engineVersion = (string)Engine.GetVersionInfo()["string"];
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        RenderingServer.SetDefaultClearColor(Colors.Black);
#endif
        updateHUDText();

        if (Platform.IsLinux)
        {
            xDisplay = XOpenDisplay(null);
            xRootWindow = XRootWindow(xDisplay, 0);
        }

        StartThread();
    }

    private void OnDestroy()
    {
        StopThread();
    }

    void StartThread()
    {
        if (thread != null)
            return;
        running = true;
        thread = new Thread(MoveMouse);
        thread.Start();
    }

    void StopThread()
    {
        if (thread == null)
            return;
        running = false;
        thread.Join();
        thread = null;
    }

    void MoveMouse()
    {
        var start = DateTime.Now;
        while (running)
        {
            var elapsed = DateTime.Now - start;
            var x = screenX + (int)(elapsed.TotalMilliseconds * pixelsPerMs) % screenWidth;
            if (Platform.IsWindows)
            {
                var y = GetCursorPosition().Y;
                SetCursorPos(x, y);
            }
            else
            {
                XQueryPointer(xDisplay, xRootWindow, out var _, out var _, out var _, out var y, out var _, out var _, out var _);
                XWarpPointer(xDisplay, 0, xRootWindow, 0, 0, 0, 0, x, y);
                XFlush(xDisplay);
            }
            Thread.Sleep(1);
        }
    }

#if UNITY
    void Update()
#else
    public override void _Process(double delta)
#endif
    {
#if UNITY
        // Input.mousePosition is read at the start of the frame and cached.
        var pos = Input.mousePosition;
        mouseX = pos.x;
        mouseY = Screen.height - pos.y;
#else
        // GetGlobalMousePosition() updates in real time!
        // Cache it to get comparable test results.
        var pos = GetGlobalMousePosition();
        mouseX = pos.X;
        mouseY = pos.Y;
#endif
        if (GetKeyDown(KeyCode.UpArrow))
        {
            pixelsPerMs *= 1.1f;
        }
        if (GetKeyDown(KeyCode.DownArrow))
        {
            pixelsPerMs /= 1.1f;
            if (pixelsPerMs < 0)
                pixelsPerMs = 0;
        }
        if (GetKeyDown(KeyCode.RightArrow))
        {
            additionalFrameTimeMs++;
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.LeftArrow))
        {
            additionalFrameTimeMs--;
            if (additionalFrameTimeMs < 0)
                additionalFrameTimeMs = 0;
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Tab))
        {
#if UNITY
            var mode = Screen.fullScreenMode;
#else
            var mode = DisplayServer.WindowGetMode();
#endif
            var i = modes.IndexOf(mode);
            i = (i + 1) % modes.Count;
#if UNITY
            // FullScreenMode.ExclusiveFullScreen is only supported with Direct3D11/12.
            // Note this is *not* the same as WindowMode.ExclusiveFullscreen in Godot
            // (which is actually equivalent to FullScreenMode.FullScreenWindow).
            if (modes[i] == FullScreenMode.ExclusiveFullScreen)
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12)
                    i = (i + 1) % modes.Count;
            Screen.fullScreenMode = modes[i];
#else
            DisplayServer.WindowSetMode(modes[i]);
#endif
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Alpha1))
        {
            profileName = "VSync; queue 2 frames";
            minIdealF = maxIdealF = 2;
            minIdealW = maxIdealW = 3;
#if UNITY
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 2;
            Application.targetFrameRate = -1;
#else
            profileName += " [WARNING: queued frames cannot be set at runtime]";
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
            // QualitySettings.maxQueuedFrames = 2;
            Engine.MaxFps = 0;
#endif
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Alpha2))
        {
            profileName = "VSync; queue 1 frame";
            minIdealF = maxIdealF = 1;
            minIdealW = maxIdealW = 2;
#if UNITY
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 1;
            Application.targetFrameRate = -1;
#else
            profileName += " [WARNING: queued frames cannot be set at runtime]";
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
            // QualitySettings.maxQueuedFrames = 1;
            Engine.MaxFps = 0;
#endif
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Alpha3))
        {
            profileName = "Free; capped at refresh rate";
            // Negative latency can be achieved below tear lines.
            minIdealF = -1;
            maxIdealF = 1;
            // No tearing occurs when windowed.
            minIdealW = 1;
            maxIdealW = 2;
#if UNITY
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 1;
            Application.targetFrameRate = Screen.currentResolution.refreshRate;
#else
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            // QualitySettings.maxQueuedFrames = 1;
            Engine.MaxFps = (int)DisplayServer.ScreenGetRefreshRate();
#endif
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Alpha4))
        {
            profileName = "Free; capped at double refresh rate";
            // Negative latency can be achieved below tear lines.
            minIdealF = -1;
            maxIdealF = 1;
            // No tearing occurs when windowed.
            minIdealW = 1;
            maxIdealW = 2;
#if UNITY
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 1;
            Application.targetFrameRate = Screen.currentResolution.refreshRate * 2;
#else
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            // QualitySettings.maxQueuedFrames = 1;
            Engine.MaxFps = (int)(DisplayServer.ScreenGetRefreshRate() * 2);
#endif
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Alpha5))
        {
            profileName = "Free; uncapped";
            // Negative latency can be achieved below tear lines.
            minIdealF = -1;
            maxIdealF = 0;
            // No tearing occurs when windowed.
            minIdealW = maxIdealW = 1;
#if UNITY
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 1;
            Application.targetFrameRate = -1;
#else
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            // QualitySettings.maxQueuedFrames = 1;
            Engine.MaxFps = 0;
#endif
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Space))
            StartThread();
        if (GetKeyDown(KeyCode.Escape))
            StopThread();

#if UNITY
        // This value takes at least one frame to update.
        if (Screen.fullScreenMode != displayedFullScreenMode)
            updateHUDText();
#endif

        if (additionalFrameTimeMs > 0)
            Thread.Sleep(additionalFrameTimeMs);

#if UNITY
        screenWidth = Screen.width;
#if UNITY_2021_2_OR_NEWER
        screenX = Screen.mainWindowPosition.x;
#else
        // Not supported.
        screenX = 0;
#endif
#else
        // This cannot be read off the main thread.
        var window = GetWindow();
        screenWidth = (int)window.Size.X;
        screenX = (int)window.Position.X;
        QueueRedraw();
#endif
    }

#if UNITY
    private void OnGUI()
#else
    public override void _Draw()
#endif
    {
#if UNITY
        GUI.Label(rect, hudText, style);
        var height = Screen.height;
#else
        var height = (int)GetViewport().GetVisibleRect().Size.Y;
#endif
        // Each line represents 1 frame worth of movement, starting at 0 frames.
        for (int i = -1; i < colors.Length; i++)
        {
#if UNITY
            var refreshRate = Screen.currentResolution.refreshRate;
#else
            var refreshRate = DisplayServer.ScreenGetRefreshRate();
#endif
            var msPerFrame = 1f / refreshRate * 1000;
            var pixelsPerFrame = pixelsPerMs * msPerFrame;
            var lineX = mouseX + i * pixelsPerFrame;
            if (i != -1)
                DrawVerticalLine(lineX, 0, height, colors[i]);
            int? minIdeal, maxIdeal;
            if (FullScreen)
            {
                minIdeal = minIdealF;
                maxIdeal = maxIdealF;
            }
            else
            {
                minIdeal = minIdealW;
                maxIdeal = maxIdealW;
            }
            if (i >= minIdeal && i <= maxIdeal)
            {
                DrawVerticalLine(lineX, mouseY - 10, 40, idealColor);
                DrawVerticalLine(lineX, mouseY - 10, 2, idealBorder);
                DrawVerticalLine(lineX, mouseY - 10 + 40, 2, idealBorder);
            }
        }
    }

    private void DrawVerticalLine(float x, float y, float length, Color color)
    {
#if UNITY
        var rect = new Rect(x, y, 1, length);
        GUI.DrawTexture(rect, pixel, ScaleMode.StretchToFill, false, 0, color, 0, 0);
#else
        var top = new Vector2(x, y);
        var bottom = top + new Vector2(0, length);
        DrawLine(top, bottom, color, 1);
#endif
    }

    private void updateHUDText()
    {
#if UNITY
        displayedFullScreenMode = Screen.fullScreenMode;

        hudText =
#else
        label.Text =
#endif
            $"Environment.OSVersion: {Environment.OSVersion}\n" +
#if UNITY
            $"Application.unityVersion: {Application.unityVersion}\n" +
#else
            $"Engine.GetVersionInfo: {engineVersion}\n" +
#endif
            "\n" +
            $"Profile: {profileName}\n" +
#if UNITY
            $"QualitySettings.vSyncCount: {QualitySettings.vSyncCount}\n" +
            $"QualitySettings.maxQueuedFrames: {QualitySettings.maxQueuedFrames}\n" +
            $"Application.targetFrameRate: {Application.targetFrameRate}\n" +
            $"Screen.fullScreenMode: {displayedFullScreenMode}\n" +
#else
            $"VsyncMode: {DisplayServer.WindowGetVsyncMode()}\n" +
            // QualitySettings.maxQueuedFrames?
            $"MaxFps: {Engine.MaxFps}\n" +
            $"WindowMode: {DisplayServer.WindowGetMode()}\n" +
#endif
            "\n" +
            $"Additional frame time (ms): {additionalFrameTimeMs}\n" +
            "\n" +
            "Up/Down: Change mouse pixels per ms\n" +
            "Left/Right: Change additional frame time\n" +
            "Tab: Change fullscreen type\n" +
            "Space: Start moving mouse\n" +
            "Esc: Stop moving mouse\n" +
            "";
    }

#if UNITY
    private bool GetKeyDown(KeyCode key)
    {
        return Input.GetKeyDown(key);
    }
#else
    private bool GetKeyDown(Key key)
    {
        return Input.IsActionJustPressed(key.ToString());
    }
#endif

    // https://stackoverflow.com/questions/1316681/getting-mouse-position-in-c-sharp/5577528#5577528

    /// <summary>
    /// Struct representing a point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// Retrieves the cursor's position, in screen coordinates.
    /// </summary>
    /// <see>See MSDN documentation for further information.</see>
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    public static POINT GetCursorPosition()
    {
        POINT lpPoint;
        GetCursorPos(out lpPoint);
        // NOTE: If you need error handling
        // bool success = GetCursorPos(out lpPoint);
        // if (!success)

        return lpPoint;
    }

    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("libX11")]
    static extern IntPtr XOpenDisplay(string display_name);

    [DllImport("libX11")]
    static extern uint XRootWindow(IntPtr display, int screen_number);

    [DllImport("libX11")]
    static extern bool XQueryPointer(IntPtr display, uint w, out uint root_return, out uint child_return, out int root_x_return, out int root_y_return, out int win_x_return, out int win_y_return, out uint mask_return);

    [DllImport("libX11")]
    static extern int XWarpPointer(IntPtr display, uint src_w, uint dst_w, int src_x, int src_y, uint src_width, uint src_height, int dest_x, int dest_y);

    [DllImport("libX11")]
    static extern int XFlush(IntPtr display);
}

#if UNITY
class Platform
{
    public static bool IsWindows => SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows;
    public static bool IsLinux => SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux;
}
#else
class Platform
{
    public static bool IsWindows => OS.GetName() == "Windows" || OS.GetName() == "UWP";
    public static bool IsLinux => OS.GetName() == "Linux";
}

class KeyCode
{
    public const Key UpArrow = Key.Up;
    public const Key DownArrow = Key.Down;
    public const Key LeftArrow = Key.Left;
    public const Key RightArrow = Key.Right;
    public const Key Alpha1 = Key.Key1;
    public const Key Alpha2 = Key.Key2;
    public const Key Alpha3 = Key.Key3;
    public const Key Alpha4 = Key.Key4;
    public const Key Alpha5 = Key.Key5;
    public const Key Tab = Key.Tab;
    public const Key Space = Key.Space;
    public const Key Escape = Key.Escape;

    private static readonly Key[] keys = {
        UpArrow,
        DownArrow,
        LeftArrow,
        RightArrow,
        Alpha1,
        Alpha2,
        Alpha3,
        Alpha4,
        Alpha5,
        Tab,
        Space,
        Escape,
    };

    public static void AddActions()
    {
        foreach (var key in keys)
        {
            InputMap.AddAction(key.ToString());
            InputMap.ActionAddEvent(key.ToString(), new InputEventKey { Keycode = key });
        }
    }
}
#endif
