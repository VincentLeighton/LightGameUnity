using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that coordinates game state, level transitions, and save/load.
/// Persists across scenes via DontDestroyOnLoad.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Reference to the LevelManager")]
    public LevelManager LevelMgr;

    [Tooltip("Reference to the LightBeamSystem")]
    public LightBeamSystem BeamSystem;

    [Tooltip("Delay in seconds before transitioning to the next level")]
    public float LevelCompleteDelay = 2f;

    [Tooltip("Total number of levels available")]
    public int TotalLevels = 3;

    /// <summary>Current game state.</summary>
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    /// <summary>Fired when the game state changes.</summary>
    public event Action<GameState> OnStateChanged;

    private SaveData _saveData;
    private Coroutine _levelCompleteCoroutine;

    private const string SaveKey = "LightPuzzleSaveData";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _saveData = new SaveData();
        LoadProgress();
    }

    private void Update()
    {
        // Handle Android back button (mapped to Escape)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.Playing)
                PauseGame();
            else if (CurrentState == GameState.Paused)
                ResumeGame();
        }
    }

    /// <summary>
    /// Loads a level by index. Sets state to Playing on success.
    /// </summary>
    public void LoadLevel(int levelIndex)
    {
        if (_levelCompleteCoroutine != null)
        {
            StopCoroutine(_levelCompleteCoroutine);
            _levelCompleteCoroutine = null;
        }

        if (LevelMgr == null)
        {
            Debug.LogError("GameManager: LevelManager reference is not set.");
            return;
        }

        bool success = LevelMgr.LoadLevel(levelIndex);
        if (success)
        {
            _saveData.currentLevelIndex = levelIndex;
            SetState(GameState.Playing);

            // Subscribe to illumination changes for puzzle completion check
            if (BeamSystem != null)
                BeamSystem.OnIlluminationChanged += CheckPuzzleCompletion;
        }
    }

    /// <summary>
    /// Called when all puzzle objectives are illuminated.
    /// Triggers level-complete flow with a delay before next level.
    /// </summary>
    public void OnLevelComplete()
    {
        if (CurrentState != GameState.Playing)
            return;

        SetState(GameState.LevelComplete);

        // Mark level as completed
        int levelIndex = _saveData.currentLevelIndex;
        if (!_saveData.completedLevels.Contains(levelIndex))
            _saveData.completedLevels.Add(levelIndex);

        SaveProgress();

        // Unsubscribe from illumination changes
        if (BeamSystem != null)
            BeamSystem.OnIlluminationChanged -= CheckPuzzleCompletion;

        _levelCompleteCoroutine = StartCoroutine(LevelCompleteSequence());
    }

    private IEnumerator LevelCompleteSequence()
    {
        yield return new WaitForSeconds(LevelCompleteDelay);

        int nextLevel = _saveData.currentLevelIndex + 1;
        if (nextLevel >= TotalLevels)
        {
            SetState(GameState.GameComplete);
        }
        else
        {
            LoadLevel(nextLevel);
        }

        _levelCompleteCoroutine = null;
    }

    /// <summary>
    /// Pauses the game by setting Time.timeScale to 0.
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState != GameState.Playing)
            return;

        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    /// <summary>
    /// Resumes the game by restoring Time.timeScale to 1.
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused)
            return;

        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    /// <summary>
    /// Saves current progress to PlayerPrefs as JSON.
    /// </summary>
    public void SaveProgress()
    {
        try
        {
            string json = JsonUtility.ToJson(_saveData);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save progress: {e.Message}");
        }
    }

    /// <summary>
    /// Loads progress from PlayerPrefs. Starts fresh if data is corrupted.
    /// </summary>
    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
            return;

        try
        {
            string json = PlayerPrefs.GetString(SaveKey);
            var loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded != null)
            {
                _saveData = loaded;
                if (_saveData.completedLevels == null)
                    _saveData.completedLevels = new List<int>();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load save data, starting fresh: {e.Message}");
            _saveData = new SaveData();
        }
    }

    /// <summary>
    /// Returns the current save data (for UI or other systems).
    /// </summary>
    public SaveData GetSaveData() => _saveData;

    private void CheckPuzzleCompletion(System.Collections.Generic.HashSet<Vector2Int> illuminatedTiles)
    {
        if (CurrentState != GameState.Playing || LevelMgr == null)
            return;

        var objectives = LevelMgr.GetPuzzleObjectives();
        if (objectives == null || objectives.Length == 0)
            return;

        // Update each objective's illumination state
        foreach (var obj in objectives)
        {
            if (obj != null)
                obj.UpdateIlluminationState(illuminatedTiles.Contains(obj.TilePosition));
        }

        // Check if all objectives are illuminated
        var positions = new Vector2Int[objectives.Length];
        for (int i = 0; i < objectives.Length; i++)
            positions[i] = objectives[i].TilePosition;

        if (PuzzleChecker.IsLevelComplete(positions, illuminatedTiles))
            OnLevelComplete();
    }

    private void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// Called by Unity when the application is paused/resumed (e.g., sent to background).
    /// Saves progress when going to background.
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Going to background — save progress
            SaveProgress();
        }
    }
}
