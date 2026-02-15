---
inclusion: auto
---

# Unity Project: Light Puzzle Game

## Project Layout

- Unity project root: `LightGame/`
- Game scripts: `LightGame/Assets/Scripts/` with assembly `GameScripts.asmdef`
  - `Core/` — Pure data models, enums, static utility classes (no MonoBehaviours)
  - `Components/` — Unity MonoBehaviours (future)
  - `UI/` — UI scripts (future)
- Tests: `LightGame/Assets/Tests/`
  - `EditMode/` — Unit + property tests, assembly `EditModeTests.asmdef`
  - `PlayMode/` — Integration tests (future)
- Level data: `LightGame/Assets/Levels/` (JSON files)
- Plugins: `LightGame/Assets/Plugins/` (FsCheck.dll, FSharp.Core.dll)

## Unity Editor Versions

Two Unity versions are installed:
- `2022.3.16f1` (used by this project)
- `6000.3.3f1`

Path: `C:\Program Files\Unity\Hub\Editor\2022.3.16f1\Editor\Unity.exe`

## Running Tests

- Unity is typically open with the project loaded, so **batch mode `-runTests` will fail** with "Multiple Unity instances cannot open the same project."
- Tests must be run from the Unity Test Runner inside the editor: Window → General → Test Runner → EditMode tab.
- After writing tests, ask the user to run them and report results rather than attempting batch mode execution.
- There is no `dotnet test` support — the `.csproj` files are Unity-generated and not compatible with `dotnet` CLI.

## Assembly Definitions

The `EditModeTests.asmdef` references:
- `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `GameScripts`
- Precompiled: `nunit.framework.dll`, `FsCheck.dll`, `FSharp.Core.dll`
- Define constraint: `UNITY_INCLUDE_TESTS`

New test files placed in `Assets/Tests/EditMode/` are automatically picked up by this assembly. No `.asmdef` changes needed for new test classes.

## Code Conventions

- Pure logic goes in `Scripts/Core/` as static classes or plain C# classes — no MonoBehaviour dependency. This keeps them testable in EditMode without a running scene.
- Data model classes use `[Serializable]` attribute for Unity's `JsonUtility` serialization.
- `TileRow` wrapper class is needed because `JsonUtility` cannot serialize jagged arrays (`string[][]`) directly. The tiles grid is `TileRow[]` where each `TileRow` has a `string[] row`.
- Grid coordinates: `x` = column, `y` = row. `tiles[y].row[x]` to access a tile. Floor = `"_"`, Wall = `"#"`.
- Direction indices 0-7 map to: Right, UpRight, Up, UpLeft, Left, DownLeft, Down, DownRight (see `GridDirections.All`).
- Mirror rotation indices 0-7 map to 0°-315° in 45° steps.
