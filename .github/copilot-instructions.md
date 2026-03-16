# MineCrate — AI Agent Instructions

> Unity 6 (URP) 2D platformer action game. Players fight waves of enemies using collectible tools.
> Two game modes: **Solo** and **Arena** (2-player local split-keyboard). Tutorial mode available.

## Project Structure

All game content lives under `Assets/_Game/`. Never place files outside this prefix.

```
Assets/_Game/
  Art/          — Sprites, Animations, Backgrounds, VFX
  Audio/        — BGM, SFX, MainMixer.mixer
  Data/         — All ScriptableObject assets (EnemyData, EventChannels, ToolData, MapData, etc.)
  Docs/         — Design docs (ToolBalanceConfig.md, VacuumFlowGuide.md)
  Material/     — Materials
  Prefabs/      — Manager/, Player/, Projectiles/, vfx/, enemy prefabs
  Scenes/       — MainMenu, MapPicker, Game, Game_Map2, Arena, Arena_Map2, Tutorial, Setting, Achievement, trailer
  Scripts/      — All C# code, organized by domain:
    Core/       — GameManager, SaveManager, SceneLoader, SceneNames, BGMManager, SFXManager, MapData, GameModeData
    Core/Events — SO event channel system (EventChannel<T>, VoidEventChannel, IntEventChannel, etc.)
    Player/     — PlayerController, PlayerHealth, PlayerInputHandler, PlayerToolHandler, PlayerData
    Enemies/    — BaseEnemy, WalkerEnemy, FlyerEnemy, EliteEnemy, EnemyData, EnemySpawner, HordeConfig
    Tools/      — BaseTool + 8 concrete tools, ToolData, Toolbox, Configs/
    Combat/     — ToolProjectile
    UI/         — GameHUD, GameOverUI, MainMenuUI, MapPickerUI, SettingsUI, PauseUI, MenuSlideController
    VFX/        — HitTextPopup
    Tutorial/   — TutorialManager, TutorialUI
    Editor/     — Editor-only menu tools (SetupBuildSettings, UpdateToolData, CreateMapSpawnConfigs)
```

## Architecture Essentials

### ScriptableObject Event Channels (core communication pattern)
- Decoupled event system: `EventChannel<T>` base class in `Core/Events/`
- Concrete types: `VoidEventChannel`, `IntEventChannel`, `StringEventChannel`, `FloatEventChannel`, `ToolDataEventChannel`, `EnemyFallChannel`, `DeathDropChannel`
- ~15 channel assets in `Data/EventChannels/` (e.g., `OnGameOver`, `OnScoreChanged`, `OnToolEquipped`)
- Subscribe in `OnEnable`, unsubscribe in `OnDisable`
- Raise with null-safe: `_onScoreChanged?.Raise(value)`

### Singleton Managers
- Pattern: `public static Instance`, null-checked `Awake()` with `DontDestroyOnLoad`, destroy duplicates
- Managers: `GameManager`, `SceneLoader`, `BGMManager`, `SFXManager`, `CameraShake`, `MenuSlideController`

### ScriptableObject Data
- All game data is SO-driven: `ToolData`, `EnemyData`, `PlayerData`, `GameModeData`, `MapData`, `MapSpawnConfig`, `HordeConfig`
- CreateAssetMenu namespace: `"ToolCrate/..."` (e.g., `ToolCrate/Tool Data`, `ToolCrate/Events/Void Event Channel`)

### Tool System
- Inheritance: `BaseTool` → 8 concrete tools (Hammer, Screwdriver, TapeMeasure, NailGun, Blowtorch, Vacuum, Magnet, Chainsaw)
- `PlayerToolHandler` uses `ToolBinding[]` mapping `ToolData` SO → `BaseTool` component
- Tools live on Player prefab as disabled components; enabled on equip

### Enemy System
- Inheritance: `BaseEnemy` → `WalkerEnemy`, `FlyerEnemy`, `EliteEnemy`
- `EnemySpawner` handles wave spawning with lane-based cooldowns and difficulty scaling

### Scene Flow
- MainMenu → MapPicker → Game/Arena/Tutorial
- Cross-scene state via static properties: `GameManager.PendingGameMode`, `GameManager.PendingMap`, `GameManager.IsTutorial`
- Scene name constants in `SceneNames` static class — always use these, never hardcode scene strings

### Save System
- `SaveManager` (static class) + `SaveData` (serializable class)
- JSON via `JsonUtility`, file at `Application.persistentDataPath/save_data.json`
- Tracks: high scores (global + per-map), lifetime stats, input binding overrides, resolution, audio volumes

### Input
- Unity Input System (`ToolCrateInput.inputactions`)
- Two control schemes: `KeyboardLeft` (P1), `KeyboardRight` (P2)
- `PlayerInputHandler` clones the asset per player to avoid shared state

## Coding Conventions

### Naming
- **Private serialized fields:** `_camelCase` with underscore prefix (`_currentState`, `_onScoreChanged`)
- **Public properties:** `PascalCase` (`Instance`, `CurrentState`, `Score`)
- **Animator hashes:** Static readonly with `Hash` suffix (`SpeedHash`, `AttackHash`)
- **Event channel fields:** `_onXxxVerb` pattern (`_onScoreChanged`, `_onGameOver`)
- **One class per file**, filename matches class name

### Serialization
- Always `[SerializeField] private` — never public fields for inspector exposure
- Group with `[Header("...")]` sections
- Use `[Tooltip("...")]` for editor documentation
- Mark obsolete fields: `[System.Obsolete("...")] [HideInInspector]`

### UI Scripts
- Wire button listeners in `Start()` or `Awake()`
- Subscribe to event channels in `OnEnable`, unsubscribe in `OnDisable`
- Use `CanvasGroup` for fade transitions
- Use `TextMeshProUGUI` (TMPro) for all text
- Debug.Log with `[ClassName]` prefix

## Build & Editor Tools

No CI/CD pipeline. Use Unity Editor menu:
- `Tools/Setup Build Settings` — configures Build Settings with all `_Game/Scenes/`
- `Tools/Reset Save Data` — deletes save file + clears PlayerPrefs
- `Tools/Update Tool Unlock Reqs` — batch-updates tool SO unlock scores and weights
- `Tools/Create Map Spawn Configs` — creates/updates `MapSpawnConfig` assets

## Key Packages

| Package | Usage |
|---------|-------|
| Universal RP 17.3.0 | 2D URP renderer |
| Input System 1.18.0 | New Input System (split-keyboard 2P) |
| 2D Animation 13.0.2 | Sprite animations |
| TextMeshPro | All UI text |
| Unity UI (uGUI) 2.0.0 | Canvas-based UI |

## Pitfalls

- **Scene names:** Always reference via `SceneNames` constants, not raw strings
- **Event channel lifecycle:** Always unsubscribe in `OnDisable` to avoid stale listeners
- **Singleton access:** Check `GameManager.Instance != null` before use — it doesn't exist in MainMenu scene
- **Player index:** Arena mode supports 2 players (index 0 and 1). New features must account for multiplayer
- **Tool equip:** Tools are components on the Player prefab. Adding a new tool requires: SO asset, `BaseTool` subclass, `ToolBinding` entry, animator setup
- **Save migration:** `SaveManager.SanitizeData()` handles legacy PlayerPrefs migration. Preserve this when modifying save format
