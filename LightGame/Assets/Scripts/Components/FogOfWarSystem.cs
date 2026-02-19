using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the fog-of-war overlay that hides unilluminated areas.
/// Uses a render texture (one pixel per tile) with alpha lerping for fade transitions.
/// Renders as a scene-space quad with sorting order from VisualConfig.FogSortOrder
/// so that element and player sprites render above the fog.
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

    private RenderTexture _fogTexture;
    private Texture2D _cpuTexture;
    private float[,] _currentAlpha;  // current fog alpha per tile (1 = fully fogged)
    private float[,] _targetAlpha;   // target fog alpha per tile
    private HashSet<Vector2Int> _visibleTiles = new HashSet<Vector2Int>();
    private GameObject _fogQuad;

    /// <summary>
    /// Initializes the fog system for a level of the given dimensions.
    /// All tiles start fully fogged.
    /// Creates a scene-space quad to render the fog at VisualConfig.FogSortOrder.
    /// </summary>
    public void Initialize(int gridWidth, int gridHeight)
    {
        GridWidth = gridWidth;
        GridHeight = gridHeight;

        _currentAlpha = new float[gridWidth, gridHeight];
        _targetAlpha = new float[gridWidth, gridHeight];

        // Start fully fogged
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                _currentAlpha[x, y] = 1f;
                _targetAlpha[x, y] = 1f;
            }

        // Create render texture and CPU-side texture
        if (_fogTexture != null)
            _fogTexture.Release();

        _fogTexture = new RenderTexture(gridWidth, gridHeight, 0, RenderTextureFormat.ARGB32);
        _fogTexture.filterMode = FilterMode.Point;
        _fogTexture.Create();

        _cpuTexture = new Texture2D(gridWidth, gridHeight, TextureFormat.ARGB32, false);
        _cpuTexture.filterMode = FilterMode.Point;

        // Assign texture to material
        if (FogMaterial != null)
            FogMaterial.SetTexture("_FogTex", _fogTexture);

        // Create scene-space fog quad so fog respects sorting order hierarchy
        CreateFogQuad(gridWidth, gridHeight);

        _visibleTiles.Clear();
        UpdateTexture();
    }

    /// <summary>
    /// Creates a scene-space quad that covers the grid and renders the fog material
    /// at VisualConfig.FogSortOrder, ensuring elements and player render above it.
    /// </summary>
    private void CreateFogQuad(int gridWidth, int gridHeight)
    {
        if (_fogQuad != null)
            Destroy(_fogQuad);

        _fogQuad = new GameObject("FogQuad");
        _fogQuad.transform.SetParent(transform);

        // Position the quad to cover the grid (tiles are at integer positions, centered)
        // Grid goes from (0.5, 0.5) to (width-0.5, height-0.5) in world space
        _fogQuad.transform.position = new Vector3(gridWidth * 0.5f, gridHeight * 0.5f, 0f);
        _fogQuad.transform.localScale = new Vector3(gridWidth, gridHeight, 1f);

        var meshFilter = _fogQuad.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateQuadMesh();

        var meshRenderer = _fogQuad.AddComponent<MeshRenderer>();
        meshRenderer.material = FogMaterial;
        meshRenderer.sortingOrder = VisualConfig.FogSortOrder;
    }

    private static Mesh CreateQuadMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// Updates the fog based on a new set of illuminated tiles.
    /// Called by LightBeamSystem.OnIlluminationChanged.
    /// </summary>
    public void UpdateIllumination(HashSet<Vector2Int> illuminatedTiles)
    {
        _visibleTiles = illuminatedTiles ?? new HashSet<Vector2Int>();

        // Set target alpha: 0 for illuminated tiles, 1 for dark tiles
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
            {
                _targetAlpha[x, y] = _visibleTiles.Contains(new Vector2Int(x, y)) ? 0f : 1f;
            }
    }

    /// <summary>
    /// Returns true if the given tile is currently visible (fog removed).
    /// A tile is considered visible when its current fog alpha is below 0.5.
    /// </summary>
    public bool IsTileVisible(Vector2Int tile)
    {
        if (tile.x < 0 || tile.x >= GridWidth || tile.y < 0 || tile.y >= GridHeight)
            return false;

        return _currentAlpha[tile.x, tile.y] < 0.5f;
    }

    /// <summary>
    /// Returns the set of tiles currently considered visible (fog alpha below 0.5).
    /// Used for testing the fog-illumination invariant.
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
        if (_currentAlpha == null)
            return;

        bool changed = false;
        float lerpSpeed = FadeDuration > 0f ? Time.deltaTime / FadeDuration : 1f;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                float target = _targetAlpha[x, y];
                float current = _currentAlpha[x, y];

                if (!Mathf.Approximately(current, target))
                {
                    _currentAlpha[x, y] = Mathf.MoveTowards(current, target, lerpSpeed);
                    changed = true;
                }
            }
        }

        if (changed)
            UpdateTexture();
    }

    private void UpdateTexture()
    {
        if (_cpuTexture == null || _fogTexture == null)
            return;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                float a = _currentAlpha[x, y];
                _cpuTexture.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        }

        _cpuTexture.Apply();
        Graphics.Blit(_cpuTexture, _fogTexture);
    }

    private void OnDestroy()
    {
        if (_fogQuad != null)
            Destroy(_fogQuad);

        if (_fogTexture != null)
        {
            _fogTexture.Release();
            _fogTexture = null;
        }
    }
}
