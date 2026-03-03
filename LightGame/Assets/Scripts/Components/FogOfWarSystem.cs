using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the fog-of-war overlay that hides unilluminated areas.
/// Uses per-tile SpriteRenderers with alpha fading for transitions.
/// Renders at VisualConfig.FogSortOrder so elements and player render above the fog.
/// Listens to LightBeamSystem.OnIlluminationChanged for updates.
/// </summary>
public class FogOfWarSystem : MonoBehaviour
{
    [Tooltip("Duration of fog fade-in/fade-out transitions in seconds")]
    public float FadeDuration = 0.3f;

    [Tooltip("Material using the FogOfWar shader (assign in Inspector)")]
    public Material FogMaterial;

    /// <summary>Level grid dimensions.</summary>
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }

    private float[,] _currentAlpha;  // current fog alpha per tile (1 = fully fogged)
    private float[,] _targetAlpha;   // target fog alpha per tile
    private SpriteRenderer[,] _fogTiles;
    private HashSet<Vector2Int> _visibleTiles = new HashSet<Vector2Int>();
    private List<GameObject> _fogObjects = new List<GameObject>();
    private static Sprite _fogSprite;

    /// <summary>
    /// Initializes the fog system for a level of the given dimensions.
    /// All tiles start fully fogged.
    /// </summary>
    public void Initialize(int gridWidth, int gridHeight)
    {
        GridWidth = gridWidth;
        GridHeight = gridHeight;

        // Clean up previous fog tiles
        foreach (var go in _fogObjects)
        {
            if (go != null) Destroy(go);
        }
        _fogObjects.Clear();

        _currentAlpha = new float[gridWidth, gridHeight];
        _targetAlpha = new float[gridWidth, gridHeight];
        _fogTiles = new SpriteRenderer[gridWidth, gridHeight];

        // Create shared fog sprite once
        if (_fogSprite == null)
            _fogSprite = SpriteFactory.CreateSquare(VisualConfig.SpriteResolution, Color.black);

        // Create a fog tile for each grid cell
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                var go = new GameObject($"Fog_{x}_{y}");
                go.transform.SetParent(transform);
                go.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                go.transform.localScale = Vector3.one;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _fogSprite;
                sr.sortingOrder = VisualConfig.FogSortOrder;
                sr.color = Color.black;

                _fogTiles[x, y] = sr;
                _fogObjects.Add(go);

                _currentAlpha[x, y] = 1f;
                _targetAlpha[x, y] = 1f;
            }
        }

        _visibleTiles.Clear();
    }

    /// <summary>
    /// Updates the fog based on a new set of illuminated tiles.
    /// Called by LightBeamSystem.OnIlluminationChanged.
    /// </summary>
    public void UpdateIllumination(HashSet<Vector2Int> illuminatedTiles)
    {
        _visibleTiles = illuminatedTiles ?? new HashSet<Vector2Int>();

        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
            {
                _targetAlpha[x, y] = _visibleTiles.Contains(new Vector2Int(x, y)) ? 0f : 1f;
            }
    }

    /// <summary>
    /// Returns true if the given tile is currently visible (fog removed).
    /// </summary>
    public bool IsTileVisible(Vector2Int tile)
    {
        if (tile.x < 0 || tile.x >= GridWidth || tile.y < 0 || tile.y >= GridHeight)
            return false;
        return _currentAlpha[tile.x, tile.y] < 0.5f;
    }

    /// <summary>
    /// Returns the set of tiles currently considered visible (fog alpha below 0.5).
    /// </summary>
    public HashSet<Vector2Int> GetVisibleTiles()
    {
        var visible = new HashSet<Vector2Int>();
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
            {
                if (_currentAlpha[x, y] < 0.5f)
                    visible.Add(new Vector2Int(x, y));
            }
        return visible;
    }

    private void Update()
    {
        if (_currentAlpha == null || _fogTiles == null)
            return;

        float lerpSpeed = FadeDuration > 0f ? Time.deltaTime / FadeDuration : 1f;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                float target = _targetAlpha[x, y];
                float current = _currentAlpha[x, y];

                if (!Mathf.Approximately(current, target))
                {
                    current = Mathf.MoveTowards(current, target, lerpSpeed);
                    _currentAlpha[x, y] = current;

                    var sr = _fogTiles[x, y];
                    if (sr != null)
                        sr.color = new Color(0f, 0f, 0f, current);
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var go in _fogObjects)
        {
            if (go != null) Destroy(go);
        }
        _fogObjects.Clear();
    }
}
