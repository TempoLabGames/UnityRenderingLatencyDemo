#if UNITY_5_3_OR_NEWER || UNITY_5_2
#define UNITY
#endif
// https://wiki.unity3d.com/index.php/FramesPerSecond
#if UNITY
using UnityEngine;
#else
using Godot;
#endif

#if UNITY
public class FPSDisplay : MonoBehaviour
#else
public partial class FPSDisplay : Node2D
#endif
{
    float deltaTime = 0.0f;

#if !UNITY
    private Label label;

    public override void _Ready()
    {
        label = new Label();
        label.Size = GetViewport().GetVisibleRect().Size;
        label.Position = new Vector2(-10, 10);
        label.HorizontalAlignment = HorizontalAlignment.Right;
        AddChild(label);
    }
#endif

#if UNITY
    void Update()
#else
    public override void _Process(double delta)
#endif
    {
#if UNITY
        var delta = Time.unscaledDeltaTime;
#endif
        deltaTime += ((float)delta - deltaTime) * 0.1f;
#if !UNITY
        label.Text = GetText();
#endif
    }

#if UNITY
    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperRight;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = Color.white;
        string text = GetText();
        GUI.Label(rect, text, style);
    }
#endif

    private string GetText()
    {
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        return $"{msec:0.0} ms\n({fps:0.} fps)";
    }
}
