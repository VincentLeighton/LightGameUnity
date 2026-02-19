using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all UI panels: start screen, inventory display, objective count,
/// mode toggle, pause menu, level complete, and game complete screens.
/// Attach to the Canvas GameObject and wire up references in the Inspector.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the GameManager")]
    public GameManager GameMgr;

    [Tooltip("Reference to the InputManager")]
    public InputManager InputMgr;

    [Tooltip("Reference to the LevelManager")]
    public LevelManager LevelMgr;

    [Tooltip("Reference to the LightBeamSystem")]
    public LightBeamSystem BeamSystem;

    [Header("Start Screen")]
    [Tooltip("Panel shown on the start/main-menu screen")]
    public GameObject StartScreenPanel;

    [Tooltip("Button on the start screen that begins the game")]
    public Button StartButton;

    [Header("Inventory Panel")]
    [Tooltip("Panel containing the inventory display")]
    public GameObject InventoryPanel;

    [Tooltip("Text displaying the mirror count")]
    public Text InventoryCountText;

    [Header("Objective Panel")]
    [Tooltip("Panel containing the objective display")]
    public GameObject ObjectivePanel;

    [Tooltip("Text displaying remaining objectives count")]
    public Text ObjectiveCountText;

    [Header("Mode Toggle")]
    [Tooltip("Button to toggle between Movement and MirrorPlacement modes")]
    public Button ModeToggleButton;

    [Tooltip("Text label on the mode toggle button")]
    public Text ModeToggleText;

    [Header("Panels")]
    [Tooltip("Panel shown when the game is paused")]
    public GameObject PauseMenuPanel;

    [Tooltip("Button inside the pause menu to resume")]
    public Button ResumeButton;

    [Tooltip("Panel shown on level completion")]
    public GameObject LevelCompletePanel;

    [Tooltip("Panel shown when all levels are finished")]
    public GameObject GameCompletePanel;

    private Inventory _inventory;

    private void OnEnable()
    {
        if (GameMgr != null)
            GameMgr.OnStateChanged += HandleStateChanged;

        if (BeamSystem != null)
            BeamSystem.OnIlluminationChanged += HandleIlluminationChanged;

        if (LevelMgr != null)
            LevelMgr.OnLevelLoaded += HandleLevelLoaded;

        if (StartButton != null)
            StartButton.onClick.AddListener(OnStartClicked);

        if (ModeToggleButton != null)
            ModeToggleButton.onClick.AddListener(OnModeToggleClicked);

        if (ResumeButton != null)
            ResumeButton.onClick.AddListener(OnResumeClicked);
    }

    private void OnDisable()
    {
        if (GameMgr != null)
            GameMgr.OnStateChanged -= HandleStateChanged;

        if (BeamSystem != null)
            BeamSystem.OnIlluminationChanged -= HandleIlluminationChanged;

        if (LevelMgr != null)
            LevelMgr.OnLevelLoaded -= HandleLevelLoaded;

        UnsubscribeInventory();

        if (StartButton != null)
            StartButton.onClick.RemoveListener(OnStartClicked);

        if (ModeToggleButton != null)
            ModeToggleButton.onClick.RemoveListener(OnModeToggleClicked);

        if (ResumeButton != null)
            ResumeButton.onClick.RemoveListener(OnResumeClicked);
    }

    private void Start()
    {
        // Hide all overlay panels initially
        SetPanelActive(PauseMenuPanel, false);
        SetPanelActive(LevelCompletePanel, false);
        SetPanelActive(GameCompletePanel, false);

        // Set initial visibility based on current game state
        var state = GameMgr != null ? GameMgr.CurrentState : GameState.MainMenu;
        bool isMainMenu = state == GameState.MainMenu;

        SetPanelActive(StartScreenPanel, isMainMenu);
        SetGameplayUIActive(!isMainMenu);

        UpdateModeToggleLabel();
    }

    /// <summary>
    /// Called when a new level is loaded. Subscribes to the level's inventory.
    /// </summary>
    private void HandleLevelLoaded(LevelData levelData)
    {
        // Subscribe to the new inventory
        UnsubscribeInventory();
        if (LevelMgr != null)
        {
            _inventory = LevelMgr.GetInventory();
            if (_inventory != null)
            {
                _inventory.OnMirrorCountChanged += HandleMirrorCountChanged;
                UpdateMirrorCount(_inventory.MirrorCount);
            }
        }

        // Update objective count from current illumination
        if (BeamSystem != null)
            HandleIlluminationChanged(BeamSystem.GetIlluminatedTiles());
    }

    private void UnsubscribeInventory()
    {
        if (_inventory != null)
        {
            _inventory.OnMirrorCountChanged -= HandleMirrorCountChanged;
            _inventory = null;
        }
    }

    /// <summary>
    /// Handles game state transitions — shows/hides panels accordingly.
    /// </summary>
    private void HandleStateChanged(GameState newState)
    {
        bool isMainMenu = newState == GameState.MainMenu;

        // Start screen is only visible during MainMenu
        SetPanelActive(StartScreenPanel, isMainMenu);

        // Gameplay UI is hidden during MainMenu, visible otherwise
        SetGameplayUIActive(!isMainMenu);

        // Overlay panels
        SetPanelActive(PauseMenuPanel, newState == GameState.Paused);
        SetPanelActive(LevelCompletePanel, newState == GameState.LevelComplete);
        SetPanelActive(GameCompletePanel, newState == GameState.GameComplete);
    }

    /// <summary>
    /// Updates the remaining objectives count when illumination changes.
    /// </summary>
    private void HandleIlluminationChanged(HashSet<Vector2Int> illuminatedTiles)
    {
        if (LevelMgr == null || ObjectiveCountText == null)
            return;

        var objectives = LevelMgr.GetPuzzleObjectives();
        if (objectives == null || objectives.Length == 0)
        {
            ObjectiveCountText.text = "0";
            return;
        }

        var positions = new Vector2Int[objectives.Length];
        for (int i = 0; i < objectives.Length; i++)
            positions[i] = objectives[i].TilePosition;

        int remaining = PuzzleChecker.GetRemainingCount(positions, illuminatedTiles);
        ObjectiveCountText.text = remaining.ToString();
    }

    /// <summary>
    /// Updates the mirror count display when inventory changes.
    /// </summary>
    private void HandleMirrorCountChanged(int newCount)
    {
        UpdateMirrorCount(newCount);
    }

    private void UpdateMirrorCount(int count)
    {
        if (InventoryCountText != null)
            InventoryCountText.text = count.ToString();
    }

    /// <summary>
    /// Toggles between Movement and MirrorPlacement modes.
    /// </summary>
    private void OnModeToggleClicked()
    {
        if (InputMgr == null)
            return;

        if (InputMgr.CurrentMode == InteractionMode.Movement)
            InputMgr.SetMode(InteractionMode.MirrorPlacement);
        else
            InputMgr.SetMode(InteractionMode.Movement);

        UpdateModeToggleLabel();
    }

    private void UpdateModeToggleLabel()
    {
        if (ModeToggleText == null || InputMgr == null)
            return;

        ModeToggleText.text = InputMgr.CurrentMode == InteractionMode.Movement
            ? "Move"
            : "Mirror";
    }

    private void OnResumeClicked()
    {
        if (GameMgr != null)
            GameMgr.ResumeGame();
    }

    /// <summary>
    /// Called when the Start Game button is clicked.
    /// </summary>
    private void OnStartClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    /// <summary>
    /// Shows or hides gameplay UI elements (inventory, objectives, mode toggle).
    /// </summary>
    private void SetGameplayUIActive(bool active)
    {
        SetPanelActive(InventoryPanel, active);
        SetPanelActive(ObjectivePanel, active);
        if (ModeToggleButton != null)
            ModeToggleButton.gameObject.SetActive(active);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
