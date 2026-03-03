using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug UI element that displays the player's current grid position.
/// Anchored to the bottom-left corner of the screen.
/// </summary>
public class DebugPositionDisplay : MonoBehaviour
{
    [Tooltip("Reference to the PlayerController")]
    public PlayerController Player;

    private Text _text;

    private void Start()
    {
        // Find or create a Canvas
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        if (canvas == null) return;

        // Create the text GameObject as a child of the canvas
        var go = new GameObject("DebugPositionText");
        go.transform.SetParent(canvas.transform, false);

        _text = go.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = 18;
        _text.color = Color.white;
        _text.alignment = TextAnchor.LowerLeft;

        // Anchor to bottom-left
        var rt = _text.rectTransform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(10, 10);
        rt.sizeDelta = new Vector2(200, 30);
    }

    private void Update()
    {
        if (_text == null || Player == null) return;
        var pos = Player.CurrentTilePosition;
        _text.text = $"Grid: ({pos.x}, {pos.y})";
    }

    private void OnDestroy()
    {
        if (_text != null)
            Destroy(_text.gameObject);
    }
}
