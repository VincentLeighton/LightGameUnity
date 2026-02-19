using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;

// Feature: light-puzzle-game, Property 16: UI panel visibility per game state
// Validates: Requirements 12.1, 12.3

[TestFixture]
public class UIVisibilityTests
{
    private static readonly GameState[] AllStates = (GameState[])Enum.GetValues(typeof(GameState));

    // Property 16: StartScreenPanel is visible only during MainMenu state
    [Test]
    public void StartScreenVisibleOnlyDuringMainMenu()
    {
        var gen = Gen.Elements(AllStates);
        var arb = Arb.From(gen);

        Prop.ForAll(arb, state =>
        {
            var visibility = UIVisibilityLogic.GetPanelVisibility(state);

            Assert.AreEqual(state == GameState.MainMenu, visibility.StartScreen,
                $"StartScreen should be {(state == GameState.MainMenu ? "visible" : "hidden")} for state {state}");
        }).QuickCheckThrowOnFailure();
    }

    // Property 16: Gameplay UI hidden during MainMenu, visible otherwise
    [Test]
    public void GameplayUIHiddenDuringMainMenu()
    {
        var gen = Gen.Elements(AllStates);
        var arb = Arb.From(gen);

        Prop.ForAll(arb, state =>
        {
            var visibility = UIVisibilityLogic.GetPanelVisibility(state);
            bool expectGameplayVisible = state != GameState.MainMenu;

            Assert.AreEqual(expectGameplayVisible, visibility.InventoryPanel,
                $"InventoryPanel visibility wrong for state {state}");
            Assert.AreEqual(expectGameplayVisible, visibility.ObjectivePanel,
                $"ObjectivePanel visibility wrong for state {state}");
            Assert.AreEqual(expectGameplayVisible, visibility.ModeToggleButton,
                $"ModeToggleButton visibility wrong for state {state}");
        }).QuickCheckThrowOnFailure();
    }

    // Property 16: Each state has exactly one valid panel configuration
    [Test]
    public void OverlayPanelsCorrectPerState()
    {
        var gen = Gen.Elements(AllStates);
        var arb = Arb.From(gen);

        Prop.ForAll(arb, state =>
        {
            var v = UIVisibilityLogic.GetPanelVisibility(state);

            Assert.AreEqual(state == GameState.Paused, v.PauseMenu,
                $"PauseMenu visibility wrong for state {state}");
            Assert.AreEqual(state == GameState.LevelComplete, v.LevelComplete,
                $"LevelComplete visibility wrong for state {state}");
            Assert.AreEqual(state == GameState.GameComplete, v.GameComplete,
                $"GameComplete visibility wrong for state {state}");
        }).QuickCheckThrowOnFailure();
    }
}
