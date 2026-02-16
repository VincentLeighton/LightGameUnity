using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders light beam segments using LineRenderer components.
/// Listens to LightBeamSystem.OnIlluminationChanged to update visuals.
/// </summary>
public class BeamRenderer : MonoBehaviour
{
    [Tooltip("Prefab with a LineRenderer component for drawing beam segments")]
    public GameObject BeamLinePrefab;

    [Tooltip("Reference to the LightBeamSystem")]
    public LightBeamSystem BeamSystem;

    [Tooltip("Beam line width")]
    public float BeamWidth = 0.05f;

    [Tooltip("Beam color")]
    public Color BeamColor = new Color(1f, 0.95f, 0.6f, 1f);

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
        UpdateBeamVisuals();
    }

    /// <summary>
    /// Rebuilds all beam line visuals from the current beam segments.
    /// </summary>
    public void UpdateBeamVisuals()
    {
        if (BeamSystem == null) return;

        var segments = BeamSystem.GetBeamSegments();

        // Return all active lines to pool
        foreach (var lr in _activeLines)
        {
            if (lr != null)
            {
                lr.gameObject.SetActive(false);
            }
        }
        _activeLines.Clear();

        // Create/reuse LineRenderers for each segment
        for (int i = 0; i < segments.Count; i++)
        {
            var lr = GetOrCreateLine(i);
            var seg = segments[i];

            // Convert tile coordinates to world coordinates (center of tile)
            Vector3 start = new Vector3(seg.Start.x + 0.5f, seg.Start.y + 0.5f, -0.1f);
            Vector3 end = new Vector3(seg.End.x + 0.5f, seg.End.y + 0.5f, -0.1f);

            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = BeamWidth;
            lr.endWidth = BeamWidth;
            lr.startColor = BeamColor;
            lr.endColor = BeamColor;
            lr.gameObject.SetActive(true);

            _activeLines.Add(lr);
        }
    }

    private LineRenderer GetOrCreateLine(int index)
    {
        // Reuse from pool if available
        if (index < _linePool.Count && _linePool[index] != null)
        {
            return _linePool[index].GetComponent<LineRenderer>();
        }

        // Create new line
        GameObject go;
        if (BeamLinePrefab != null)
        {
            go = Instantiate(BeamLinePrefab, transform);
        }
        else
        {
            go = new GameObject($"BeamLine_{index}");
            go.transform.SetParent(transform);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.sortingOrder = 5;
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        var lineRenderer = go.GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = go.AddComponent<LineRenderer>();

        _linePool.Add(go);
        return lineRenderer;
    }

    /// <summary>
    /// Clears all beam visuals.
    /// </summary>
    public void ClearBeams()
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
