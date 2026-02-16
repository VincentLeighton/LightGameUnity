using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Loads levels from JSON, validates them, and instantiates all game objects.
/// Manages the lifecycle of level objects (create/destroy).
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Tooltip("Parent transform for all level objects")]
    public Transform LevelRoot;

    [Tooltip("Reference to the LightBeamSystem")]
    public LightBeamSystem BeamSystem;

    [Tooltip("Reference to the FogOfWarSystem")]
    public FogOfWarSystem FogSystem;

    [Tooltip("Reference to the PlayerController")]
    public PlayerController Player;

    [Tooltip("Reference to the MirrorInteraction handler")]
    public MirrorInteraction MirrorHandler;

    [Tooltip("Reference to the CameraController")]
    public CameraController CameraCtrl;

    [Tooltip("Optional prefab for mirror objects")]
    public GameObject MirrorPrefab;

    /// <summary>The currently loaded level data.</summary>
    public LevelData CurrentLevel { get; private set; }

    /// <summary>Fired when a level fails validation. Passes the error messages.</summary>
    public event Action<List<string>> OnValidationFailed;

    /// <summary>Fired when a level is successfully loaded.</summary>
    public event Action<LevelData> OnLevelLoaded;

    // Tracking instantiated objects for cleanup
    private List<GameObject> _levelObjects = new List<GameObject>();
    private LightSource[] _lightSources;
    private BeamEmitter[] _beamEmitters;
    private Mirror[] _mirrors;
    private PuzzleObjective[] _puzzleObjectives;
    private Inventory _inventory;

    // Tile lookup
    private string[][] _tileGrid;

    /// <summary>
    /// Loads a level by index from Assets/Levels/level_{index}.json.
    /// Returns true if the level was loaded and validated successfully.
    /// </summary>
    public bool LoadLevel(int levelIndex)
    {
        string path = $"Levels/level_{levelIndex}";
        TextAsset jsonAsset = Resources.Load<TextAsset>(path);

        if (jsonAsset == null)
        {
            // Try loading from Assets/Levels/ directly
            jsonAsset = Resources.Load<TextAsset>($"level_{levelIndex}");
        }

        if (jsonAsset == null)
        {
            Debug.LogWarning($"Level file not found: {path}");
            return false;
        }

        LevelData data;
        try
        {
            data = JsonUtility.FromJson<LevelData>(jsonAsset.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse level JSON: {e.Message}");
            OnValidationFailed?.Invoke(new List<string> { $"JSON parse error: {e.Message}" });
            return false;
        }

        return LoadLevelFromData(data);
    }

    /// <summary>
    /// Loads a level from a LevelData object directly.
    /// Validates the data before instantiating objects.
    /// </summary>
    public bool LoadLevelFromData(LevelData data)
    {
        // Validate
        var result = LevelValidator.Validate(data);
        if (!result.IsValid)
        {
            Debug.LogError($"Level validation failed: {string.Join(", ", result.Errors)}");
            OnValidationFailed?.Invoke(result.Errors);
            return false;
        }

        // Clear previous level
        ClearLevel();

        CurrentLevel = data;

        // Build tile grid for quick lookup
        _tileGrid = new string[data.height][];
        for (int y = 0; y < data.height; y++)
            _tileGrid[y] = data.tiles[y].row;

        Func<Vector2Int, bool> isWall = pos => IsWall(pos);
        Func<Vector2Int, bool> isWalkable = pos => IsWalkable(pos);

        // Set up camera bounds
        if (CameraCtrl != null)
        {
            var bounds = new Bounds(
                new Vector3(data.width * 0.5f, data.height * 0.5f, 0f),
                new Vector3(data.width, data.height, 0f));
            CameraCtrl.LevelBounds = bounds;
        }

        // Instantiate light sources
        _lightSources = new LightSource[data.lightSources.Length];
        for (int i = 0; i < data.lightSources.Length; i++)
        {
            var lsData = data.lightSources[i];
            var go = CreateLevelObject($"LightSource_{i}");
            go.transform.position = TileToWorld(lsData.x, lsData.y);
            var ls = go.AddComponent<LightSource>();
            ls.TilePosition = lsData.ToVector2Int();
            ls.IlluminationRadius = lsData.radius;
            ls.IsWall = isWall;
            _lightSources[i] = ls;
        }

        // Instantiate beam emitters
        _beamEmitters = new BeamEmitter[data.beamEmitters.Length];
        for (int i = 0; i < data.beamEmitters.Length; i++)
        {
            var beData = data.beamEmitters[i];
            var go = CreateLevelObject($"BeamEmitter_{i}");
            go.transform.position = TileToWorld(beData.x, beData.y);
            var be = go.AddComponent<BeamEmitter>();
            be.TilePosition = beData.ToVector2Int();
            be.BeamDirection = beData.GetDirection();
            _beamEmitters[i] = be;
        }

        // Instantiate mirrors
        _mirrors = new Mirror[data.mirrors.Length];
        for (int i = 0; i < data.mirrors.Length; i++)
        {
            var mData = data.mirrors[i];
            GameObject go;
            if (MirrorPrefab != null)
            {
                go = Instantiate(MirrorPrefab, LevelRoot);
                go.name = $"Mirror_{i}";
            }
            else
            {
                go = CreateLevelObject($"Mirror_{i}");
            }
            go.transform.position = TileToWorld(mData.x, mData.y);
            var mirror = go.GetComponent<Mirror>();
            if (mirror == null)
                mirror = go.AddComponent<Mirror>();
            mirror.TilePosition = mData.ToVector2Int();
            mirror.RotationIndex = mData.rotationIndex;
            _mirrors[i] = mirror;
            _levelObjects.Add(go);
        }

        // Instantiate puzzle objectives
        _puzzleObjectives = new PuzzleObjective[data.puzzleObjectives.Length];
        for (int i = 0; i < data.puzzleObjectives.Length; i++)
        {
            var poData = data.puzzleObjectives[i];
            var go = CreateLevelObject($"Objective_{i}");
            go.transform.position = TileToWorld(poData.x, poData.y);
            var po = go.AddComponent<PuzzleObjective>();
            po.TilePosition = poData.ToVector2Int();
            _puzzleObjectives[i] = po;
        }

        // Create inventory
        _inventory = new Inventory();

        // Initialize player
        if (Player != null)
        {
            Player.IsWalkable = isWalkable;
            Player.Initialize(data.playerStart.ToVector2Int());
        }

        // Initialize mirror interaction
        if (MirrorHandler != null)
        {
            MirrorHandler.PlayerInventory = _inventory;
            MirrorHandler.BeamSystem = BeamSystem;
            MirrorHandler.IsWall = isWall;
            MirrorHandler.IsOccupied = pos => IsOccupied(pos);
            MirrorHandler.MirrorPrefab = MirrorPrefab;
            MirrorHandler.MirrorParent = LevelRoot;
            MirrorHandler.Clear();
            foreach (var m in _mirrors)
                MirrorHandler.RegisterMirror(m);
        }

        // Initialize beam system
        if (BeamSystem != null)
        {
            BeamSystem.IsWall = isWall;
            BeamSystem.Initialize(_lightSources, _beamEmitters, _mirrors);
            BeamSystem.RecalculateAllBeams();
        }

        // Initialize fog system
        if (FogSystem != null)
        {
            FogSystem.Initialize(data.width, data.height);
            if (BeamSystem != null)
                FogSystem.UpdateIllumination(BeamSystem.GetIlluminatedTiles());
        }

        // Set camera target
        if (CameraCtrl != null && Player != null)
            CameraCtrl.SetTarget(Player.transform);

        OnLevelLoaded?.Invoke(data);
        return true;
    }

    /// <summary>
    /// Destroys all level objects and resets state.
    /// </summary>
    public void ClearLevel()
    {
        foreach (var go in _levelObjects)
        {
            if (go != null)
                Destroy(go);
        }
        _levelObjects.Clear();

        _lightSources = null;
        _beamEmitters = null;
        _mirrors = null;
        _puzzleObjectives = null;
        _inventory = null;
        _tileGrid = null;
        CurrentLevel = null;

        if (MirrorHandler != null)
            MirrorHandler.Clear();
    }

    /// <summary>
    /// Returns the current inventory instance (created during level load).
    /// </summary>
    public Inventory GetInventory() => _inventory;

    /// <summary>
    /// Returns the puzzle objectives for the current level.
    /// </summary>
    public PuzzleObjective[] GetPuzzleObjectives() => _puzzleObjectives;

    /// <summary>
    /// Checks if a tile position is a wall.
    /// </summary>
    public bool IsWall(Vector2Int pos)
    {
        if (_tileGrid == null) return true;
        if (pos.y < 0 || pos.y >= _tileGrid.Length) return true;
        if (pos.x < 0 || pos.x >= _tileGrid[pos.y].Length) return true;
        return _tileGrid[pos.y][pos.x] == "#";
    }

    /// <summary>
    /// Checks if a tile position is walkable (floor and in bounds).
    /// </summary>
    public bool IsWalkable(Vector2Int pos)
    {
        return !IsWall(pos);
    }

    /// <summary>
    /// Checks if a tile is occupied by a game object (mirror, emitter, light source, objective).
    /// </summary>
    public bool IsOccupied(Vector2Int pos)
    {
        if (_mirrors != null)
            foreach (var m in _mirrors)
                if (m != null && m.TilePosition == pos) return true;

        if (_beamEmitters != null)
            foreach (var be in _beamEmitters)
                if (be != null && be.TilePosition == pos) return true;

        return false;
    }

    private GameObject CreateLevelObject(string name)
    {
        var go = new GameObject(name);
        if (LevelRoot != null)
            go.transform.SetParent(LevelRoot);
        _levelObjects.Add(go);
        return go;
    }

    private static Vector3 TileToWorld(int x, int y)
    {
        return new Vector3(x + 0.5f, y + 0.5f, 0f);
    }
}
