#if UNITY_5_3_OR_NEWER || UNITY_5_2
#define UNITY
#endif
using System;
using System.Runtime.InteropServices;
using System.Threading;
#if UNITY
using UnityEngine;
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
#else
    private Color[] colors = { Colors.White, Colors.Green, Colors.Yellow, Colors.Red, Colors.Magenta };
#endif

    private Thread thread = null;
    private bool running = false;

    private string profileName = "<press number keys>";

#if UNITY
    private string hudText = "";
    private Rect rect = new Rect(0, 0, Screen.width, Screen.height);
    private GUIStyle style;
#else
    private Label label;
    private string engineVersion;
#endif

    private int screenWidth = 1080;
    private float mouseX = 0;

#if UNITY
    void Start()
#else
    public override void _Ready()
#endif
    {
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
            var x = (int)(elapsed.TotalMilliseconds * pixelsPerMs) % screenWidth;
            var y = GetCursorPosition().Y;
            SetCursorPos(x, y);
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
        mouseX = Input.mousePosition.x;
#else
        // GetViewport().GetMousePosition() updates in real time!
        // Cache it to get comparable test results.
        mouseX = GetViewport().GetMousePosition().X;
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
            if (mode == FullScreenMode.FullScreenWindow)
                mode = FullScreenMode.ExclusiveFullScreen;
            else
                mode = FullScreenMode.FullScreenWindow;
            Screen.fullScreenMode = mode;
#else
            var mode = DisplayServer.WindowGetMode();
            if (mode == DisplayServer.WindowMode.Fullscreen)
                mode = DisplayServer.WindowMode.ExclusiveFullscreen;
            else
                mode = DisplayServer.WindowMode.Fullscreen;
            DisplayServer.WindowSetMode(mode);
#endif
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Alpha1))
        {
            profileName = "VSync; queue 2 frames";
#if UNITY
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 2;
            Application.targetFrameRate = -1;
#else
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
            // QualitySettings.maxQueuedFrames = 2;
            Engine.MaxFps = 0;
#endif
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Alpha2))
        {
            profileName = "VSync; queue 1 frame";
#if UNITY
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 1;
            Application.targetFrameRate = -1;
#else
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
            // QualitySettings.maxQueuedFrames = 1;
            Engine.MaxFps = 0;
#endif
            updateHUDText();
        }
        if (GetKeyDown(KeyCode.Alpha3))
        {
            profileName = "Free; capped at refresh rate";
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

        if (additionalFrameTimeMs > 0)
            Thread.Sleep(additionalFrameTimeMs);

#if UNITY
        screenWidth = Screen.currentResolution.width;
#else
        // This cannot be read in _Draw().
        screenWidth = (int)GetViewport().GetVisibleRect().Size.X;
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
        var height = Screen.currentResolution.height;
#else
        var height = (int)GetViewport().GetVisibleRect().Size.Y;
#endif
        // Each line represents 1 frame worth of movement, starting at 0 frames.
        for (int i = 0; i < colors.Length; i++)
        {
#if UNITY
            var refreshRate = Screen.currentResolution.refreshRate;
#else
            var refreshRate = DisplayServer.ScreenGetRefreshRate();
#endif
            var msPerFrame = 1f / refreshRate * 1000;
            var pixelsPerFrame = pixelsPerMs * msPerFrame;
            var lineX = mouseX + i * pixelsPerFrame;
#if UNITY
            var rect = new Rect(lineX, 0, 1, height);
            GUI.DrawTexture(rect, pixel, ScaleMode.StretchToFill, false, 0, colors[i], 0, 0);
#else
            var top = new Vector2(lineX, 0);
            var bottom = new Vector2(lineX, height);
            DrawLine(top, bottom, colors[i], 1);
#endif
        }
    }

    private void updateHUDText()
    {
#if UNITY
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
            $"Screen.fullScreenMode: {Screen.fullScreenMode}\n" +
#else
            $"VsyncMode: {DisplayServer.WindowGetVsyncMode()}\n" +
            // QualitySettings.maxQueuedFrames?
            $"MaxFps: {Engine.MaxFps}\n" +
            $"Fullscreen: {DisplayServer.WindowGetMode()}\n" +
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
}

#if UNITY
// Nothing to do.
#else
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
