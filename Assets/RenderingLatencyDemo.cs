using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class RenderingLatencyDemo : MonoBehaviour
{
    public Texture pixel;

    private float pixelsPerMs = 1;
    private int additionalFrameTimeMs = 0;
    private Color[] colors = { Color.white, Color.green, Color.yellow, Color.red, Color.magenta };

    private Thread thread = null;
    private bool running = false;

    private string profileName = "<press number keys>";
    private string hudText = "";

    private Rect rect = new Rect(0, 0, Screen.width, Screen.height);
    private GUIStyle style;

    void Start()
    {
        style = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 20,
            normal = new GUIStyleState
            {
                textColor = Color.white,
            },
        };
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
            var x = (int)(elapsed.TotalMilliseconds * pixelsPerMs) % Screen.currentResolution.width;
            var y = GetCursorPosition().Y;
            SetCursorPos(x, y);
            Thread.Sleep(1);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            pixelsPerMs *= 1.1f;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            pixelsPerMs /= 1.1f;
            if (pixelsPerMs < 0)
                pixelsPerMs = 0;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            additionalFrameTimeMs++;
            updateHUDText();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            additionalFrameTimeMs--;
            if (additionalFrameTimeMs < 0)
                additionalFrameTimeMs = 0;
            updateHUDText();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            profileName = "VSync; queue 2 frames";
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 2;
            Application.targetFrameRate = -1;
            updateHUDText();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            profileName = "VSync; queue 1 frame";
            QualitySettings.vSyncCount = 1;
            QualitySettings.maxQueuedFrames = 1;
            Application.targetFrameRate = -1;
            updateHUDText();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            profileName = "Free; capped at refresh rate";
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 1;
            Application.targetFrameRate = Screen.currentResolution.refreshRate;
            updateHUDText();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            profileName = "Free; capped at double refresh rate";
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 1;
            Application.targetFrameRate = Screen.currentResolution.refreshRate * 2;
            updateHUDText();
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            profileName = "Free; uncapped";
            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 1;
            Application.targetFrameRate = -1;
            updateHUDText();
        }
        if (Input.GetKeyDown(KeyCode.Space))
            StartThread();
        if (Input.GetKeyDown(KeyCode.Escape))
            StopThread();

        if (additionalFrameTimeMs > 0)
            Thread.Sleep(additionalFrameTimeMs);
    }

    private void OnGUI()
    {
        GUI.Label(rect, hudText, style);
        var height = Screen.currentResolution.height;
        // Each line represents 1 frame worth of movement, starting at 0 frames.
        for (int i = 0; i < colors.Length; i++)
        {
            var msPerFrame = 1f / Screen.currentResolution.refreshRate * 1000;
            var pixelsPerFrame = pixelsPerMs * msPerFrame;
            var rect = new Rect(Input.mousePosition.x + i * pixelsPerFrame, 0, 1, height);
            GUI.DrawTexture(rect, pixel, ScaleMode.StretchToFill, false, 0, colors[i], 0, 0);
        }
    }

    private void updateHUDText()
    {
        hudText =
            $"Environment.OSVersion: {Environment.OSVersion}\n" +
            $"Application.unityVersion: {Application.unityVersion}\n" +
            "\n" +
            $"Profile: {profileName}\n" +
            $"QualitySettings.vSyncCount: {QualitySettings.vSyncCount}\n" +
            $"QualitySettings.maxQueuedFrames: {QualitySettings.maxQueuedFrames}\n" +
            $"Application.targetFrameRate: {Application.targetFrameRate}\n" +
            "\n" +
            $"Additional frame time (ms): {additionalFrameTimeMs}\n" +
            "\n" +
            "Up/Down: Change additional frame time\n" +
            "Left/Right: Change mouse pixels per ms\n" +
            "Space: Start moving mouse\n" +
            "Esc: Stop moving mouse\n" +
            "";
    }

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
