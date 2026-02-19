/// <summary>
/// Pure logic for determining which UI panels should be visible for each game state.
/// Keeps panel visibility rules testable without Unity runtime.
/// </summary>
public static class UIVisibilityLogic
{
    /// <summary>
    /// Returns the expected visibility state for all UI panels given a GameState.
    /// </summary>
    public static PanelVisibility GetPanelVisibility(GameState state)
    {
        return new PanelVisibility
        {
            StartScreen = state == GameState.MainMenu,
            InventoryPanel = state != GameState.MainMenu,
            ObjectivePanel = state != GameState.MainMenu,
            ModeToggleButton = state != GameState.MainMenu,
            PauseMenu = state == GameState.Paused,
            LevelComplete = state == GameState.LevelComplete,
            GameComplete = state == GameState.GameComplete
        };
    }

    /// <summary>
    /// Data class holding the expected visibility of each UI panel.
    /// </summary>
    public struct PanelVisibility
    {
        public bool StartScreen;
        public bool InventoryPanel;
        public bool ObjectivePanel;
        public bool ModeToggleButton;
        public bool PauseMenu;
        public bool LevelComplete;
        public bool GameComplete;

        public override string ToString() =>
            $"Start={StartScreen} Inv={InventoryPanel} Obj={ObjectivePanel} " +
            $"Mode={ModeToggleButton} Pause={PauseMenu} LvlComplete={LevelComplete} " +
            $"GameComplete={GameComplete}";
    }
}
