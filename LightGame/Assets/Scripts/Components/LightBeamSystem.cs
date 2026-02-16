using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates all illumination: collects light source radii and beam tracer results.
/// Exposes the union of all illuminated tiles and beam segments for rendering.
/// Fires OnIlluminationChanged whenever the illuminated set changes.
/// </summary>
public class LightBeamSystem : MonoBehaviour
{
    [Tooltip("Maximum number of reflections before a beam terminates")]
    public int MaxReflections = 20;

    /// <summary>Fired when the set of illuminated tiles changes. Passes the new set.</summary>
    public event Action<HashSet<Vector2Int>> OnIlluminationChanged;

    /// <summary>
    /// Callback to check if a tile is a wall.
    /// Must be set by LevelManager after level load.
    /// </summary>
    public Func<Vector2Int, bool> IsWall { get; set; }

    private LightSource[] _lightSources;
    private BeamEmitter[] _beamEmitters;
    private Mirror[] _mirrors;

    private HashSet<Vector2Int> _illuminatedTiles = new HashSet<Vector2Int>();
    private List<BeamSegment> _beamSegments = new List<BeamSegment>();

    /// <summary>
    /// Initializes the system with references to all light-related objects in the level.
    /// Call after level load.
    /// </summary>
    public void Initialize(LightSource[] lightSources, BeamEmitter[] beamEmitters, Mirror[] mirrors)
    {
        _lightSources = lightSources ?? Array.Empty<LightSource>();
        _beamEmitters = beamEmitters ?? Array.Empty<BeamEmitter>();
        _mirrors = mirrors ?? Array.Empty<Mirror>();
    }

    /// <summary>
    /// Returns the current set of all illuminated tiles (light source radii + beam paths).
    /// </summary>
    public HashSet<Vector2Int> GetIlluminatedTiles()
    {
        return _illuminatedTiles;
    }

    /// <summary>
    /// Returns the current beam segments for rendering.
    /// </summary>
    public List<BeamSegment> GetBeamSegments()
    {
        return _beamSegments;
    }

    /// <summary>
    /// Recalculates all illumination from scratch: light source radii, beam emitter activation,
    /// beam tracing through mirrors. Call when any mirror, light source, or emitter changes.
    /// </summary>
    public void RecalculateAllBeams()
    {
        if (IsWall == null)
            return;

        _illuminatedTiles.Clear();
        _beamSegments.Clear();

        // Step 1: Collect illumination from all light source radii
        var lightSourceIlluminated = new HashSet<Vector2Int>();
        foreach (var ls in _lightSources)
        {
            var tiles = ls.GetIlluminatedTiles();
            lightSourceIlluminated.UnionWith(tiles);
        }

        // Add light source radius tiles to the total illuminated set
        _illuminatedTiles.UnionWith(lightSourceIlluminated);

        // Step 2: Update beam emitter activation states
        foreach (var emitter in _beamEmitters)
        {
            emitter.UpdateActiveState(lightSourceIlluminated.Contains(emitter.TilePosition));
        }

        // Step 3: Build beam tracer inputs
        var emitterInputs = new BeamTracer.BeamEmitterInput[_beamEmitters.Length];
        for (int i = 0; i < _beamEmitters.Length; i++)
        {
            emitterInputs[i] = new BeamTracer.BeamEmitterInput
            {
                Position = _beamEmitters[i].TilePosition,
                Direction = _beamEmitters[i].BeamDirection
            };
        }

        var mirrorInputs = new BeamTracer.MirrorInput[_mirrors.Length];
        for (int i = 0; i < _mirrors.Length; i++)
        {
            mirrorInputs[i] = new BeamTracer.MirrorInput
            {
                Position = _mirrors[i].TilePosition,
                RotationIndex = _mirrors[i].RotationIndex
            };
        }

        // Step 4: Trace beams
        var traceResult = BeamTracer.TraceBeams(
            emitterInputs,
            mirrorInputs,
            IsWall,
            lightSourceIlluminated,
            MaxReflections);

        // Step 5: Merge beam illumination into total set
        _illuminatedTiles.UnionWith(traceResult.IlluminatedTiles);
        _beamSegments.AddRange(traceResult.Segments);

        // Step 6: Notify listeners
        OnIlluminationChanged?.Invoke(_illuminatedTiles);
    }
}
