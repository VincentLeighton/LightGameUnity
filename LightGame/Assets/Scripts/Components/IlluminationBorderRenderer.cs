using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders illumination border edges as red lines using pooled LineRenderers.
/// Subscribes to LightBeamSystem.OnIlluminationChanged to update visuals.
/// </summary>
public class IlluminationBorderRenderer : MonoBehaviour
{
    [Tooltip("Reference to the LightBeamSystem")]
    public LightBeamSystem BeamSystem;

    [Tooltip("Border line width in world units (2px default)")]
    public float BorderWidth = 2f / VisualConfig.SpriteResolution;

    [Tooltip("Border color")]
    public Color BorderColor = Color.red;

    private List<LineRenderer> _activeLines = new List<LineRenderer>();
    private List<GameObject> _linePool = new List<GameObject>();

    private void OnEnable()
    {
        if (BeamSystem != null)
            BeamSystem.OnIlluminationChanged += OnIlluminationChanged;
    }

    private void OnDisable()
    {
        if (BeamSystem != null)
            BeamSystem.OnIlluminationChanged -= OnIlluminationChanged;
    }

    private void OnIlluminationChanged(HashSet<Vector2Int> illuminatedTiles)
    {
        UpdateBorderVisuals(illuminatedTiles);
    }

    /// <summary>
    /// Recomputes border edges and updates line visuals.
    /// </summary>
    public void UpdateBorderVisuals(HashSet<Vector2Int> illuminatedTiles)
    {
        if (BeamSystem == null)
        {
            Debug.LogWarning("IlluminationBorderRenderer: No BeamSystem assigned.");
            return;
        }

        var edges = IlluminationBorderLogic.ComputeBorderEdges(illuminatedTiles, BeamSystem.IsWall);

        // Return all active lines to pool
        foreach (var lr in _activeLines)
        {
            if (lr != null)
                lr.gameObject.SetActive(false);
        }
        _activeLines.Clear();

        // Create/reuse LineRenderers for each edge
        for (int i = 0; i < edges.Count; i++)
        {
            var lr = GetOrCreateLine(i);
            var edge = edges[i];

            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(edge.Start.x, edge.Start.y, -0.05f));
            lr.SetPosition(1, new Vector3(edge.End.x, edge.End.y, -0.05f));
            lr.startWidth = BorderWidth;
            lr.endWidth = BorderWidth;
            lr.startColor = BorderColor;
            lr.endColor = BorderColor;
            lr.sortingOrder = VisualConfig.BorderSortOrder;
            lr.gameObject.SetActive(true);

            _activeLines.Add(lr);
        }
    }

    private LineRenderer GetOrCreateLine(int index)
    {
        if (index < _linePool.Count && _linePool[index] != null)
            return _linePool[index].GetComponent<LineRenderer>();

        var go = new GameObject($"BorderLine_{index}");
        go.transform.SetParent(transform);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.sortingOrder = VisualConfig.BorderSortOrder;
        lr.material = new Material(Shader.Find("Sprites/Default"));

        _linePool.Add(go);
        return lr;
    }

    private void OnDestroy()
    {
        foreach (var go in _linePool)
        {
            if (go != null)
                Destroy(go);
        }
        _linePool.Clear();
        _activeLines.Clear();
    }
}
