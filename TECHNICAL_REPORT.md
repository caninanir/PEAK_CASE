# Match-2 Pop Game - Complete Technical Analysis Report

**Generated:** 2025  
**Unity Version:** 2019+ (MonoBehaviour lifecycle)  
**Project Path:** `Assets/Scripts/`

---

## Executive Summary

"Match-2 Pop" is a match-2 puzzle game built in Unity using C#. The architecture follows an event-driven, service-oriented pattern with clear separation between gameplay logic, presentation, and data management. The game uses a singleton-based manager system with an EventBus for decoupled communication, object pooling for performance, and coroutine-based animations for visual feedback. Unique features include collection rows for duck collection, dual goal systems (obstacle and cube goals), and goal collection animations.

---

## 1. High-Level Architecture

### 1.1 Architecture Summary

The game uses a **layered, event-driven architecture** with the following structure:
- **Core Layer**: State management (`GameStateController`), event system (`EventBus`), configuration (`ConfigurationManager`)
- **Data Layer**: Level data (`LevelData`), save system (`SaveData`, `SaveManager`), game config (`GameConfig` ScriptableObject)
- **Gameplay Layer**: Grid management (`GridController`), matching (`MatchController`), input (`InputController`), gravity (`GravityController`), items (cubes, rockets, obstacles)
- **Presentation Layer**: UI controllers (`GameplayUIController`, `MenuUIController`), effects (`ParticleEffectManager`, `TransitionController`), animations (`FallAnimator`, `RocketAnimator`, `GoalCollectionAnimationService`)
- **Services Layer**: Pooling (`PoolManager`), audio (`AudioManager`, `MusicManager`), scene transitions (`SceneTransitionManager`)

### 1.2 Major Subsystems

#### **Game/Level Management**
- **`Assets/Scripts/Core/Managers/GameStateController.cs`** — Central state machine; manages `GameState` enum (MainMenu, Playing, GameWon, GameLost, Finished, Paused); tracks current level, moves remaining, processing flags (move processing, gravity running, active animations, particles, projectiles)
- **`Assets/Scripts/Infrastructure/Managers/LevelManager.cs`** — Loads level JSON files from Resources/levels/; maintains dictionary of all levels; provides level data access
- **`Assets/Scripts/Infrastructure/Managers/SaveManager.cs`** — Persists progress via `PlayerPrefs`; tracks completed levels; manages current level progression

#### **Board/Tile System**
- **`Assets/Scripts/Features/Grid/Controllers/GridController.cs`** — Main grid orchestrator; spawns cells and items; manages extended grid (visible + buffer rows + collection rows); handles item positioning and sibling ordering
- **`Assets/Scripts/Features/Grid/Components/GridCell.cs`** — Individual cell container; stores reference to `BaseItem`; provides position data
- **`Assets/Scripts/Features/Grid/Services/GridDataService.cs`** — Data layer for grid; maintains 2D array of `GridCell[,]`; handles coordinate conversion (visible ↔ extended); manages buffer rows and collection rows
- **`Assets/Scripts/Features/Grid/Services/GridLayoutService.cs`** — Calculates cell sizes, positions; handles grid centering and masking

#### **Match-Detection**
- **`Assets/Scripts/Features/Matching/MatchController.cs`** — Orchestrates match processing; listens to `CubeTappedEvent`; handles match validation, rocket creation (5+ cubes), obstacle damage
- **`Assets/Scripts/Features/Matching/MatchDetectorService.cs`** — Flood-fill algorithm to find connected matching cubes; recursive neighbor traversal
- **`Assets/Scripts/Features/Matching/MatchValidator.cs`** — Static validation: `IsValidMatch()` (≥2 cubes), `CanCreateRocket()` (≥5 cubes)

#### **Input**
- **`Assets/Scripts/Features/Input/InputController.cs`** — Singleton input manager; processes mouse/touch in `Update()`; validates input state; publishes `CubeTappedEvent` or `RocketTappedEvent`
- **`Assets/Scripts/Features/Input/InputHandler.cs`** — Raycast handler using `GraphicRaycaster`; finds `BaseItem` under pointer
- **`Assets/Scripts/Features/Input/InputValidator.cs`** — Static validation: checks game state, processing flags, input lock

#### **UI**
- **`Assets/Scripts/UI/Controllers/GameplayUIController.cs`** — Updates moves counter, level display, goal items; listens to `LevelStartedEvent`, `MovesChangedEvent`, `GoalUpdatedEvent`
- **`Assets/Scripts/UI/Controllers/MenuUIController.cs`** — Main menu UI management
- **`Assets/Scripts/UI/Controllers/GoalDisplayController.cs`** — Manages goal item UI layout

#### **Audio**
- **`Assets/Scripts/Infrastructure/Managers/AudioManager.cs`** — Singleton; pools `AudioSource` components (20 default); plays cube break, rocket, obstacle sounds with random pitch variation; special audio for goal cubes
- **`Assets/Scripts/Infrastructure/Managers/MusicManager.cs`** — Background music management

#### **Animation/FX**
- **`Assets/Scripts/Presentation/Animations/FallAnimator.cs`** — Animates item falling using `AnimationCurve`; supports landing bounce, subtle rotation, stretch effects
- **`Assets/Scripts/Presentation/Animations/RocketAnimator.cs`** — Animates rocket creation (cubes gather to center, 0.5s duration)
- **`Assets/Scripts/Presentation/Effects/ParticleEffectManager.cs`** — Spawns particle effects for cube/obstacle destruction; uses pooled `ParticleElement` objects
- **`Assets/Scripts/Presentation/Effects/TransitionController.cs`** — Scene fade transitions (fadeIn: 0.8s, fadeOut: 0.6s)
- **`Assets/Scripts/Features/Goals/GoalCollectionAnimationService.cs`** — Animates goal collection from grid to UI with particle effects

#### **Persistence/Save**
- **`Assets/Scripts/Infrastructure/Data/SaveData.cs`** — Serializable class; stores `currentLevel` (int) and `levelCompleted` (bool[999]); saves to `PlayerPrefs` as JSON
- **`Assets/Scripts/Infrastructure/Managers/SaveManager.cs`** — Wraps `SaveData`; provides `LoadSave()`, `SaveGame()`, `MarkLevelCompleted()`, `AreAllLevelsCompleted()`

#### **Asset Config (ScriptableObjects)**
- **`Assets/Scripts/Infrastructure/Data/GameConfig.cs`** — ScriptableObject; configures cube size, grid spacing, animation durations (fallSpeed: 5f, explosionDelay: 0.2f, rocketCreationDuration: 0.5f), animation curves

---

## 2. Design Patterns & Technical Constructs

### 2.1 Singleton Pattern
**Files:** Multiple managers use singleton pattern
- **Implementation:** `public static Instance { get; private set; }` with `Awake()` check
- **Examples:**
  - `GameStateController.cs:5` — `public static GameStateController Instance`
  - `GridController.cs:9` — `public static GridController Instance`
  - `InputController.cs` — `public static InputController Instance`
  - `PoolManager.cs` — `public static PoolManager Instance`
  - `AudioManager.cs` — `public static AudioManager Instance`
  - `RocketController.cs:7` — `public static RocketController Instance`
  - `ObstacleController.cs:6` — `public static ObstacleController Instance`
- **Pattern:** All use `DontDestroyOnLoad()` to persist across scenes

### 2.2 Event Bus / Observer Pattern
**File:** `Assets/Scripts/Core/Events/EventBus.cs`
- **Implementation:** Static generic event bus using `Dictionary<Type, List<object>>`
- **Methods:**
  - `Subscribe<T>(Action<T> handler)` — Register handler for event type
  - `Unsubscribe<T>(Action<T> handler)` — Remove handler
  - `Publish<T>(T eventData)` — Invoke all handlers for event type
- **Event Types:** Defined in `Assets/Scripts/Core/Events/GameEvents.cs`:
  - `GameStateChangedEvent`, `LevelStartedEvent`, `LevelWonEvent`, `LevelLostEvent`
  - `CubeTappedEvent`, `RocketTappedEvent`, `MatchFoundEvent`, `MatchProcessedEvent`
  - `GravityStartedEvent`, `GravityCompletedEvent`, `GridInitializedEvent`, `GridUpdatedEvent`
  - `ItemSpawnedEvent`, `ItemDestroyedEvent`, `ItemDamagedEvent`
  - `ObstacleDestroyedEvent`, `GoalUpdatedEvent`, `MovesChangedEvent`
  - `GoalCollectionAnimationCompleteEvent`
- **Usage Example:**
```csharp
// Publishing
EventBus.Publish(new CubeTappedEvent { GridX = x, GridY = y, CubeType = type });

// Subscribing
EventBus.Subscribe<CubeTappedEvent>(HandleCubeTapped);
```

### 2.3 Service Locator Pattern
**Files:** Multiple services accessed via static `Instance` properties
- Services are located through singleton instances rather than dependency injection
- Example: `GridController.Instance`, `GameStateController.Instance`, `PoolManager.Instance`

### 2.4 ScriptableObjects
**File:** `Assets/Scripts/Infrastructure/Data/GameConfig.cs`
- **Usage:** Configuration data asset (`[CreateAssetMenu]`)
- **Fields:** Grid settings, animation durations, animation curves
- **Access:** Via `ConfigurationManager.Instance.GameConfig` or `GameStateController.Instance.Config`

### 2.5 State Machine
**File:** `Assets/Scripts/Core/Managers/GameStateController.cs`
- **States:** `GameState` enum (MainMenu, Playing, GameWon, GameLost, Finished, Paused)
- **Transition:** `ChangeGameState(GameState newState)` publishes `GameStateChangedEvent`
- **State checks:** `IsPlaying()`, `IsProcessingMove`, `CheckWinCondition()`, `CheckLoseCondition()`
- **Processing Flags:** Tracks move processing, gravity running, active animations, particles, projectiles

### 2.6 Coroutines
**Extensive use throughout for async operations:**
- **`MatchController.cs:48`** — `ProcessCubeTap()` coroutine handles match processing
- **`GravityController.cs:69`** — `ProcessGravity()` coroutine handles falling items
- **`FallAnimator.cs:51`** — `AnimateFalls()` coroutine animates multiple items falling
- **`RocketAnimator.cs`** — `AnimateRocketCreation()` coroutine (0.5s duration)
- **`SceneTransitionManager.cs`** — `LoadSceneWithTransition()` coroutine handles async scene loading
- **`GoalCollectionAnimationService.cs`** — `AnimateGoalCollection()` coroutine animates goal collection

### 2.7 Update() Loops
**Files with Update() methods:**
- **`InputController.cs`** — `HandleInput()` called every frame; checks input state, processes taps
- **`AudioManager.cs`** — Cleans up finished `AudioSource` components
- **`ParticleEffectManager.cs`** — Updates all active particles (`UpdateParticles()`)

### 2.8 Object Pooling
**Files:** `Assets/Scripts/Core/Services/Pooling/`
- **`GenericPool.cs`** — Generic pool implementation using `Queue<T>`
- **`PoolManager.cs`** — Manages pools for cubes (5 types, 50 each), rockets (20), obstacles (30), particles (100)
- **`IPoolable.cs`** — Interface with `OnSpawn()`, `OnDespawn()`, `OnReturnToPool()`
- **Usage:**
```csharp
BaseItem item = PoolManager.Instance.GetItem(ItemType.RedCube, parent);
PoolManager.Instance.ReturnItem(item);
```

### 2.9 Factory Pattern
**File:** `Assets/Scripts/Features/Items/Obstacles/ObstacleBehaviorFactory.cs`
- Creates obstacle behavior components based on `ItemType`
- Returns `ObstacleBehavior` implementations (BalloonBehavior, DuckBehavior)

### 2.10 Strategy Pattern
**File:** `Assets/Scripts/Features/Items/Obstacles/ObstacleBehavior.cs`
- Abstract base class for obstacle behaviors
- Different behaviors handle damage, sprites, fall capability differently
- BalloonBehavior: Can take damage from adjacent blasts and rockets
- DuckBehavior: Cannot take damage, must fall to collection row

---

## 3. Dependency and Interaction Map

### 3.1 Central Hubs

#### **GameStateController** (`Assets/Scripts/Core/Managers/GameStateController.cs`)
**Dependencies:**
- `SaveManager.Instance` — Get/set current level
- `LevelManager.Instance` — Get level data, validate levels
- `EventBus` — Publishes `GameStateChangedEvent`, `LevelStartedEvent`, `MovesChangedEvent`, `LevelWonEvent`, `LevelLostEvent`
- `RocketProjectileService` — Cleanup on level start
- `ParticleEffectManager.Instance` — Cleanup on level start
- `MusicManager.Instance` — Play end game music
- `AudioManager.Instance` — Play win/lose sounds
- `ObstacleController.Instance` — Check win condition

**Dependents:**
- `InputController` — Checks `CurrentState`, `IsProcessingMove`
- `MatchController` — Checks `IsProcessingMove`, calls `UseMove()`, `SetProcessingMove()`
- `RocketController` — Checks `IsProcessingMove`, calls `UseMove()`, `SetProcessingMove()`
- `SceneTransitionManager` — Calls `StartLevel()`
- `UI Controllers` — Listen to state change events

#### **GridController** (`Assets/Scripts/Features/Grid/Controllers/GridController.cs`)
**Dependencies:**
- `PoolManager.Instance` — Get/return items
- `EventBus` — Publishes `GridInitializedEvent`, `ItemSpawnedEvent`, `GridUpdatedEvent`
- `LevelManager.Instance` — Get level data
- `TransitionController.Instance` — Check fade state

**Dependents:**
- `MatchController` — Get cells, spawn items
- `GravityController` — Access grid data service
- `RocketController` — Get items, check positions
- `InputController` — Get grid container for canvas reference
- `RocketService` — Get items, check positions
- `MatchDetectorService` — Get cells, adjacent cells

#### **EventBus** (`Assets/Scripts/Core/Events/EventBus.cs`)
**Dependencies:** None (static class)

**Dependents:** Almost all systems subscribe/publish:
- `GameStateController` — Publishes state/level events
- `GridController` — Publishes grid/item events
- `MatchController` — Subscribes to `CubeTappedEvent`, `GridUpdatedEvent`; publishes `MatchProcessedEvent`
- `GravityController` — Subscribes to `GravityStartedEvent`, `MatchProcessedEvent`; publishes `GravityCompletedEvent`
- `RocketController` — Subscribes to `RocketTappedEvent`; publishes `RocketExplodedEvent`
- `InputController` — Publishes `CubeTappedEvent`, `RocketTappedEvent`
- `ObstacleController` — Subscribes to `LevelStartedEvent`, `ObstacleDestroyedEvent`, `ItemDestroyedEvent`, `GoalCollectionAnimationCompleteEvent`; publishes `GoalUpdatedEvent`
- `GameplayUIController` — Subscribes to `LevelStartedEvent`, `MovesChangedEvent`, `GoalUpdatedEvent`

### 3.2 Dependency Matrix

| Class | Depends On | Publishes Events | Subscribes To Events |
|-------|------------|------------------|---------------------|
| `GameStateController` | `SaveManager`, `LevelManager`, `ObstacleController` | `GameStateChangedEvent`, `LevelStartedEvent`, `MovesChangedEvent`, `LevelWonEvent`, `LevelLostEvent` | `GravityStartedEvent`, `GravityCompletedEvent` |
| `GridController` | `PoolManager`, `LevelManager` | `GridInitializedEvent`, `ItemSpawnedEvent`, `GridUpdatedEvent` | `LevelStartedEvent`, `GravityCompletedEvent` |
| `MatchController` | `GridController`, `GameStateController` | `MatchProcessedEvent` | `CubeTappedEvent`, `GridUpdatedEvent` |
| `GravityController` | `GridController` | `GravityCompletedEvent` | `GravityStartedEvent`, `MatchProcessedEvent` |
| `RocketController` | `GridController`, `GameStateController` | `RocketExplodedEvent` | `RocketTappedEvent` |
| `InputController` | `GameStateController` | `CubeTappedEvent`, `RocketTappedEvent` | `GameStateChangedEvent` |
| `ObstacleController` | `LevelManager`, `GameStateController` | `GoalUpdatedEvent` | `LevelStartedEvent`, `ObstacleDestroyedEvent`, `ItemDestroyedEvent`, `GoalCollectionAnimationCompleteEvent` |
| `GameplayUIController` | `LevelManager` | None | `LevelStartedEvent`, `MovesChangedEvent`, `GoalUpdatedEvent` |

---

## 4. Runtime Sequences

### 4.1 Game Startup / Application Launch

**Sequence:**
1. **Unity Awake() phase:**
   - `GameStateController.Awake()` — Sets singleton instance, marks DontDestroyOnLoad
   - `LevelManager.Awake()` — Sets singleton, calls `LoadAllLevels()` (scans `Assets/Resources/levels/` for `level_*.json` files)
   - `SaveManager.Awake()` — Sets singleton, calls `LoadSave()` (loads from `PlayerPrefs`)
   - `PoolManager.Awake()` — Sets singleton, initializes pools (cubes, rockets, obstacles, particles)
   - `AudioManager.Awake()` — Sets singleton, initializes audio source pool (20 sources)
   - `ConfigurationManager.Awake()` — Sets singleton, loads `GameConfig` ScriptableObject

2. **Unity Start() phase:**
   - `GameStateController.Start()` — Calls `InitializeGameState()`
     - Gets current level from `SaveManager.Instance.GetCurrentLevel()`
     - Checks if all levels completed → sets state to `Finished`
     - Otherwise validates level → sets state to `MainMenu`
     - Publishes `GameStateChangedEvent`

3. **Scene Loading:**
   - Main menu scene loads
   - UI controllers initialize and subscribe to events

**Timing:** Synchronous initialization, no delays

### 4.2 Loading and Starting a Level

**Sequence:**
1. **User triggers level start** (e.g., button click)
   - `SceneTransitionManager.LoadLevelScene(levelNumber)` called

2. **Scene transition:**
   - `TransitionController.FadeOut()` — 0.6s fade
   - `SceneManager.LoadSceneAsync("LevelScene")` — Async scene load
   - `TransitionController.FadeIn()` — 0.8s fade
   - `GameStateController.StartLevel(levelNumber)` called

3. **Level initialization:**
   - `GameStateController.StartLevel()`:
     - Cleans up visual effects (`RocketProjectileService.CleanupAllProjectiles()`, `ParticleEffectManager.CleanupAllParticles()`)
     - Gets `LevelData` from `LevelManager.Instance.GetLevelData(levelNumber)`
     - Sets `currentLevel`, `movesRemaining = levelData.move_count`
     - Sets `isProcessingMove = false`
     - Calls `LevelManager.Instance.SetCurrentLevel(levelNumber)`
     - Changes state to `GameState.Playing`
     - Publishes `LevelStartedEvent { LevelNumber }`
     - Publishes `MovesChangedEvent { MovesRemaining }`

4. **Grid initialization** (triggered by `LevelStartedEvent`):
   - `GridController.HandleLevelStarted()`:
     - Calls `InitializeGridForLevel()`

5. **Grid setup:**
   - `GridController.InitializeGrid()`:
     - `ClearGrid()` — Destroys existing cells/items
     - `dataService.Initialize(width, height, bufferRows, collectionRows)` — Creates `GridCell[width, height + bufferRows + collectionRows]`
     - `layoutService.Initialize()` — Calculates cell sizes based on screen bounds
     - `CreateExtendedGrid()` — Instantiates `GridCell` prefabs, positions them
     - `PopulateVisibleGrid(levelData.grid)` — Parses JSON grid array, spawns items
     - `PopulateBufferRows()` — Fills top buffer rows with random cubes
     - `SetupGridMask()` — Adds `RectMask2D` to hide buffer rows and collection rows
     - `SetupGridBackground()` — Initializes background
     - `layoutService.CenterGrid()` — Centers grid on screen
     - Publishes `GridInitializedEvent { GridWidth, GridHeight }`

6. **Goal initialization:**
   - `ObstacleController.HandleLevelStarted()`:
     - Calls `InitializeGoals(levelData)`
     - Calculates obstacle counts from grid (balloons, ducks)
     - Extracts cube goals from `levelData.cube_goals` array
     - Publishes `GoalUpdatedEvent` for each goal type

7. **UI initialization:**
   - `GameplayUIController.HandleLevelStarted()`:
     - Updates level display text
     - Updates moves display
     - Sets up goal items UI (delayed by `WaitForEndOfFrame`)

**Timing:**
- Fade out: 0.6s
- Scene load: Async (varies)
- Fade in: 0.8s
- Grid initialization: Synchronous (fast)
- UI setup: One frame delay

### 4.3 Typical Player Action — Tap Cube That Produces Match

**Sequence:**
1. **Input detection:**
   - `InputController.Update()` — Checks input state via `InputValidator.CanProcessInput()`
   - `Input.GetMouseButtonDown(0)` or `TouchPhase.Began` detected
   - `InputController.ProcessTap(screenPosition)` called

2. **Raycast:**
   - `InputHandler.GetTappedItem(screenPosition)` — Uses `GraphicRaycaster.Raycast()`
   - Returns `BaseItem` (assumed `CubeItem`)

3. **Event publishing:**
   - `InputController.HandleItemTapped(cube)`:
     - Gets grid position via `cube.GetGridPosition()`
     - Publishes `CubeTappedEvent { GridX, GridY, CubeType }`

4. **Match processing:**
   - `MatchController.HandleCubeTapped(CubeTappedEvent)`:
     - Checks `GameStateController.Instance.IsProcessingMove` → returns if true
     - Starts `ProcessCubeTap()` coroutine

5. **Match detection:**
   - `GameStateController.Instance.SetProcessingMove(true)` — Locks input
   - `MatchController.ProcessCubeTap()`:
     - Gets `GridCell` at tapped position: `gridController.GetCell(evt.GridX, evt.GridY)`
     - Calls `matchDetector.FindMatchingGroup(tappedCell)`:
       - Flood-fill algorithm: `FindMatchingNeighbors()` recursively finds connected cubes of same color
       - Returns `List<GridCell>` of matching group

6. **Match validation:**
   - `MatchValidator.IsValidMatch(matchingGroup)` — Checks `group.Count >= 2`
   - If valid:
     - `GameStateController.Instance.UseMove()` — Decrements `movesRemaining`, publishes `MovesChangedEvent`

7. **Rocket creation check:**
   - `MatchValidator.CanCreateRocket(matchingGroup)` — Checks `group.Count >= 5`
   - If true:
     - Randomly selects `ItemType.HorizontalRocket` or `ItemType.VerticalRocket`
     - Starts `RocketController.Instance.AnimateRocketCreation()` coroutine:
       - `RocketAnimator.AnimateRocketCreation()` — Animates cubes gathering to center (0.5s)
       - `AudioManager.Instance.PlayRocketCreationSound()`
       - Waits 0.1s
     - Spawns rocket at tapped cell: `gridController.SpawnItem(rocketType, x, y)`
   - If false:
     - Destroys matching cubes:
       - For each `GridCell` in matching group:
         - Gets `CubeItem`
         - Checks if goal cube: `ObstacleController.Instance.IsGoalCube(cube.itemType)`
         - Spawns particles: `ParticleEffectManager.Instance.SpawnCubeBurst()`
         - Plays sound: `AudioManager.Instance.PlayCubeBreakSound()` (or `PlayGoalCubeBreakSound()` if goal cube)
         - Publishes `ItemDestroyedEvent`
         - Returns to pool: `PoolManager.Instance.ReturnItem(cube)`
         - Removes from cell: `cell.RemoveItem()`

8. **Obstacle damage:**
   - `MatchController.DamageAdjacentObstacles(matchingGroup)`:
     - For each cell in matching group:
       - Gets adjacent cells: `gridController.GetAdjacentCells(x, y)`
       - For each adjacent cell:
         - If `ObstacleItem` and not already damaged:
           - Checks `obstacle.CanTakeDamageFrom(DamageSource.AdjacentBlast)`
           - Calls `obstacle.TakeDamage(1)` — Updates health, sprite
           - If destroyed: Publishes `ObstacleDestroyedEvent`

9. **Event publishing:**
   - Publishes `MatchProcessedEvent { MatchCount, RocketCreated }`
   - Waits 0.2s
   - Waits for all projectiles to complete
   - Waits 0.05f

10. **Gravity trigger:**
    - Waits for gravity to complete via `WaitForGravityToComplete()`

11. **Win/lose check:**
    - `GameStateController.Instance.CheckWinCondition()` — Checks `ObstacleController.Instance.AreAllGoalsCleared()`
    - `GameStateController.Instance.CheckLoseCondition()` — Checks `movesRemaining <= 0 && !win`
    - If win: `GameStateController.Instance.WinLevel()`
    - If lose: `GameStateController.Instance.LoseLevel()`

12. **Unlock input:**
    - `GameStateController.Instance.SetProcessingMove(false)`

**Timing:**
- Input processing: Synchronous (<1ms)
- Match detection: Synchronous (<5ms for typical grid)
- Rocket creation animation: 0.5s + 0.1s = 0.6s
- Cube destruction: Synchronous (instant)
- Wait before gravity: 0.2s + projectile wait + 0.05s
- Total before gravity: ~0.85s (with rocket) or ~0.25s (without rocket)

### 4.4 Creation of Special Tiles (Rockets)

**Location:** `Assets/Scripts/Features/Matching/MatchController.cs:61-91`

**Sequence:**
1. **Detection:**
   - `MatchValidator.CanCreateRocket(matchingGroup)` — Returns `group.Count >= 5`
   - Called in `MatchController.ProcessCubeTap()` after match validation

2. **Animation:**
   - `RocketController.Instance.AnimateRocketCreation(matchingGroup, rocketCell, rocketType)`:
     - `RocketAnimator.AnimateRocketCreation()`:
       - Creates temporary animating cube objects for each matched cube
       - Animates them gathering to target cell (0.5s duration, `SmoothStep` interpolation)
       - Scales cubes during animation (1.0 → 1.2 → 1.0)
       - Destroys animating cubes and original cubes
     - `AudioManager.Instance.PlayRocketCreationSound()`
     - Waits 0.1s

3. **Spawn:**
   - `gridController.SpawnItem(rocketType, rocketCell.x, visibleY)`:
     - Gets item from pool: `PoolManager.Instance.GetItem(rocketType, parent)`
     - Positions item at cell location
     - Calls `item.Initialize(rocketType)` — Sets sprite (horizontal/vertical)
     - Sets cell item: `cell.SetItem(item)`
     - Updates sibling order for rendering
     - Publishes `ItemSpawnedEvent`

**Timing:**
- Animation: 0.5s
- Audio delay: 0.1s
- Total: ~0.6s

### 4.5 Activation of Special Tiles (Rocket Explosion)

**Location:** `Assets/Scripts/Features/Items/Rockets/RocketController.cs:66-117`

**Sequence:**
1. **Input:**
   - Player taps rocket → `InputController` publishes `RocketTappedEvent`

2. **Processing:**
   - `RocketController.HandleRocketTapped()`:
     - Checks `GameStateController.Instance.IsProcessingMove` → returns if true
     - Starts `ProcessRocketExplosion()` coroutine

3. **Lock:**
   - `GameStateController.Instance.SetProcessingMove(true)`
   - `GameStateController.Instance.UseMove()` — Decrements moves

4. **Combo check:**
   - `rocketService.GetAllConnectedRockets(rocketPos)` — Checks 4 directions for other rockets (max 2 connected)
   - If combo found: `ProcessRocketCombo()` coroutine
   - Otherwise: `ProcessSingleRocketExplosion()` coroutine

5. **Single rocket explosion:**
   - `AudioManager.Instance.PlayRocketPopSound()`
   - `DestroyRocket(rocket)` — Destroys GameObject, removes from cell
   - Publishes `RocketExplodedEvent { GridX, GridY, RocketType, IsCombo: false }`
   - Gets direction: `rocketService.GetExplosionDirection(rocketType)` — `Vector2Int.right` (horizontal) or `Vector2Int.up` (vertical)
   - Starts projectile animations:
     - `projectileService.AnimateProjectile(rocketPos, direction)` — Animates projectile in +direction
     - `projectileService.AnimateProjectile(rocketPos, -direction)` — Animates projectile in -direction

6. **Damage application:**
   - Projectiles damage items in their path:
     - For each cell in path:
       - Gets item: `gridController.GetItem(x, y)`
       - If `CubeItem`: Destroys (particles, sound, return to pool, publish `ItemDestroyedEvent`)
       - If `ObstacleItem`: Calls `obstacle.TakeDamage(1)` (if can take rocket damage)
       - If `RocketItem`: Triggers chain reaction (`RocketController.Instance.TriggerChainReaction()`)

7. **Combo explosion:**
   - 2 rockets: Cross-pattern explosion (horizontal + vertical)
   - 3+ rockets: 3x3 area explosion
   - Destroys all rockets
   - `AudioManager.Instance.PlayComboRocketPopSound()`
   - `rocketService.DamageItemsIn3x3Area(center)` or `DamageItemsInCrossPattern(center)` — Damages all items in area
   - Waits 0.1s
   - Spawns combo projectiles (cross or 8 directions)

8. **Gravity trigger:**
   - Waits for projectiles to complete
   - Waits 0.05s
   - Waits for gravity to complete

9. **Win/lose check:**
   - Same as match processing

10. **Unlock:**
    - `GameStateController.Instance.SetProcessingMove(false)`

**Timing:**
- Projectile animation: ~0.5-1.0s (depends on grid size)
- Combo delay: 0.1s
- Total: ~1.0-1.5s

### 4.6 Duck Collection System

**Location:** `Assets/Scripts/Features/Gravity/GravityController.cs:334-364`

**Sequence:**
1. **Duck falls to collection row:**
   - During gravity processing, duck falls through visible grid
   - `GravityService.CalculateFallDistance()` allows ducks to enter collection rows
   - Duck lands in collection row cell

2. **Collection detection:**
   - After gravity completes, `GravityController.CollectDucksInCollectionRow()` called
   - Scans all collection row cells for ducks

3. **Collection processing:**
   - For each duck found:
     - Gets duck item and grid position
     - If `GoalCollectionAnimationService.Instance` exists:
       - Starts `AnimateGoalCollection()` coroutine:
         - Creates animated duplicate of duck
         - Animates from grid position to goal UI display (0.6s duration)
         - Particle effects at collection point
         - Publishes `GoalCollectionAnimationCompleteEvent` when done
     - Otherwise:
       - Publishes `ObstacleDestroyedEvent` immediately
     - Removes duck from cell
     - Returns duck to pool

4. **Goal update:**
   - `ObstacleController.HandleGoalCollectionAnimationComplete()` or `HandleObstacleDestroyed()`:
     - Decrements duck goal count
     - Publishes `GoalUpdatedEvent`
     - Checks win condition

**Timing:**
- Collection animation: 0.6s
- Total: ~0.6s per duck

### 4.7 Level Completion / Game Over

**Win Sequence:**
1. **Win condition check:**
   - `GameStateController.CheckWinCondition()` — Returns `ObstacleController.Instance.AreAllGoalsCleared()`
   - Called after match/rocket processing or duck collection

2. **Win processing:**
   - `GameStateController.WinLevel()`:
     - Checks state is `Playing` → returns if not
     - `MusicManager.Instance.PlayEndGameMusic(true)` — Plays win music
     - `AudioManager.Instance.PlayGameWonSoundDelayed()` — Plays win sound (0.5s delay)
     - `SaveManager.Instance.MarkLevelCompleted(currentLevel)` — Marks level complete, advances current level
     - Checks `SaveManager.Instance.AreAllLevelsCompleted()`:
       - If true: Changes state to `GameState.Finished`
       - Otherwise: Changes state to `GameState.GameWon`
     - Publishes `LevelWonEvent { LevelNumber }`

3. **UI response:**
   - UI controllers listen to `LevelWonEvent` → Show win popup
   - Player can click "Next Level" or "Main Menu"

**Lose Sequence:**
1. **Lose condition check:**
   - `GameStateController.CheckLoseCondition()` — Returns `movesRemaining <= 0 && state == Playing && !win`
   - Called after match processing

2. **Lose processing:**
   - `GameStateController.LoseLevel()`:
     - Checks state is `Playing` → returns if not
     - `MusicManager.Instance.PlayEndGameMusic(false)` — Plays lose music
     - `AudioManager.Instance.PlayGameLostSoundDelayed()` — Plays lose sound (0.5s delay)
     - Changes state to `GameState.GameLost`
     - Publishes `LevelLostEvent { LevelNumber }`

3. **UI response:**
   - UI controllers listen to `LevelLostEvent` → Show lose popup
   - Player can click "Retry" or "Main Menu"

**Next Level:**
- `GameStateController.NextLevel()`:
  - Gets next level: `LevelManager.Instance.GetNextLevelAfter(currentLevel)`
  - If found: Calls `StartLevel(nextLevel)`
  - Otherwise: Returns to main menu

**Return to Main Menu:**
- `GameStateController.ReturnToMainMenu()`:
  - Changes state to `MainMenu`
  - `SceneTransitionManager.Instance.LoadMainScene()` — Loads main scene with fade

**Timing:**
- Win/lose sound delay: 0.5s
- State change: Synchronous
- Scene transition: ~1.4s (fade out 0.6s + load + fade in 0.8s)

---

## 5. Concrete Example Playthrough

**Scenario:** Player taps a cube, creates 5-match, rocket spawns, player taps rocket, rocket explodes, duck falls to collection row, gravity refills, duck collected.

**Timeline (0-20 seconds):**

| Time | Action | Method/Event | Script | Sync/Async |
|------|--------|--------------|--------|-----------|
| 0.0s | Game running, level loaded | - | - | - |
| 0.1s | Player taps cube at (3,4) | `InputController.Update()` → `ProcessTap()` | `InputController.cs` | Sync |
| 0.1s | Raycast finds CubeItem | `InputHandler.GetTappedItem()` | `InputHandler.cs` | Sync |
| 0.1s | Event published | `EventBus.Publish(CubeTappedEvent)` | `InputController.cs` | Sync |
| 0.1s | Match processing starts | `MatchController.HandleCubeTapped()` → `ProcessCubeTap()` | `MatchController.cs:37` | Async (Coroutine) |
| 0.1s | Input locked | `GameStateController.SetProcessingMove(true)` | `MatchController.cs:44` | Sync |
| 0.1s | Match detection | `MatchDetectorService.FindMatchingGroup()` | `MatchDetectorService.cs:13` | Sync |
| 0.1s | Found 5 matching cubes | Returns `List<GridCell>` (5 cells) | `MatchDetectorService.cs:20` | Sync |
| 0.1s | Match validated | `MatchValidator.IsValidMatch()` → true | `MatchValidator.cs:5` | Sync |
| 0.1s | Rocket check | `MatchValidator.CanCreateRocket()` → true (≥5) | `MatchValidator.cs:10` | Sync |
| 0.1s | Move used | `GameStateController.UseMove()` | `MatchController.cs:59` | Sync |
| 0.1s | Rocket creation animation | `RocketAnimator.AnimateRocketCreation()` | `RocketAnimator.cs` | Async (Coroutine, 0.5s) |
| 0.6s | Rocket creation sound | `AudioManager.PlayRocketCreationSound()` | `RocketAnimator.cs` | Async (Coroutine) |
| 0.7s | Rocket spawned | `GridController.SpawnItem(HorizontalRocket, 3, 4)` | `MatchController.cs:89` | Sync |
| 0.7s | Event published | `EventBus.Publish(MatchProcessedEvent)` | `MatchController.cs:95` | Sync |
| 0.9s | Wait complete | `yield return new WaitForSeconds(0.2f)` | `MatchController.cs:101` | Async |
| 0.9s | Gravity started | `EventBus.Publish(GravityStartedEvent)` | `MatchController.cs:110` | Sync |
| 0.9s | Gravity processing | `GravityController.HandleGravityStarted()` → `ProcessGravity()` | `GravityController.cs:59` | Async (Coroutine) |
| 0.9s | Fall calculation | `GravityService.CalculateFallDistance()` for each column | `GravityService.cs:17` | Sync |
| 0.9s | Fall animation | `FallAnimator.AnimateFalls()` | `FallAnimator.cs:51` | Async (Coroutine, ~0.3s) |
| 1.2s | Gravity complete | `EventBus.Publish(GravityCompletedEvent)` | `GravityController.cs:77` | Sync |
| 1.2s | Duck collection | `GravityController.CollectDucksInCollectionRow()` | `GravityController.cs:74` | Sync |
| 1.2s | Goal animation | `GoalCollectionAnimationService.AnimateGoalCollection()` | `GoalCollectionAnimationService.cs:97` | Async (Coroutine, 0.6s) |
| 1.8s | Goal updated | `ObstacleController.HandleGoalCollectionAnimationComplete()` | `ObstacleController.cs:142` | Sync |
| 1.8s | Input unlocked | `GameStateController.SetProcessingMove(false)` | `MatchController.cs:125` | Sync |
| 2.0s | Player taps rocket at (3,4) | `InputController.Update()` → `ProcessTap()` | `InputController.cs` | Sync |
| 2.0s | Event published | `EventBus.Publish(RocketTappedEvent)` | `InputController.cs` | Sync |
| 2.0s | Rocket processing | `RocketController.HandleRocketTapped()` → `ProcessRocketExplosion()` | `RocketController.cs:66` | Async (Coroutine) |
| 2.0s | Input locked | `GameStateController.SetProcessingMove(true)` | `RocketController.cs:76` | Sync |
| 2.0s | Move used | `GameStateController.UseMove()` | `RocketController.cs:88` | Sync |
| 2.0s | Rocket destroyed | `DestroyRocket(rocket)` | `RocketController.cs:126` | Sync |
| 2.0s | Sound played | `AudioManager.PlayRocketPopSound()` | `RocketController.cs:124` | Async (Coroutine) |
| 2.0s | Event published | `EventBus.Publish(RocketExplodedEvent)` | `RocketController.cs:128` | Sync |
| 2.0s | Projectiles start | `RocketProjectileService.AnimateProjectile()` (2 directions) | `RocketController.cs:136-137` | Async (Coroutine, ~0.8s) |
| 2.0-2.8s | Projectiles damage items | For each cell in path: destroy cubes, damage obstacles | `RocketService.cs` | Sync (per cell) |
| 2.8s | Projectiles complete | `projectileService.WaitForAllProjectilesToComplete()` | `RocketController.cs:102` | Async |
| 2.85s | Wait complete | `yield return new WaitForSeconds(0.05f)` | `RocketController.cs:103` | Async |
| 2.85s | Gravity started | `EventBus.Publish(GravityStartedEvent)` | `RocketController.cs:105` | Sync |
| 2.85s | Gravity processing | `GravityController.ProcessGravity()` | `GravityController.cs:69` | Async (Coroutine) |
| 3.15s | Gravity complete | `EventBus.Publish(GravityCompletedEvent)` | `GravityController.cs:77` | Sync |
| 3.15s | Duck collection | `GravityController.CollectDucksInCollectionRow()` | `GravityController.cs:74` | Sync |
| 3.15s | Input unlocked | `GameStateController.SetProcessingMove(false)` | `RocketController.cs:116` | Sync |
| 3.2s | Win check | `GameStateController.CheckWinCondition()` | `RocketController.cs:107` | Sync |
| 3.2s | (Assume not won) | - | - | - |
| 20.0s | (Continue gameplay) | - | - | - |

**Notes:**
- Most operations are synchronous except animations (coroutines)
- Total time for match + rocket creation: ~0.9s
- Total time for rocket explosion: ~1.15s
- Gravity typically takes 0.2-0.5s depending on fall distances
- Duck collection animation: 0.6s

---

## 6. Call Stacks and Event Flow for Critical Operations

### 6.1 Match Detection Flow

```
InputController.Update()
  └─ InputController.ProcessTap()
      └─ InputHandler.GetTappedItem()
          └─ GraphicRaycaster.Raycast()
              └─ Returns BaseItem
      └─ InputController.HandleItemTapped()
          └─ EventBus.Publish(CubeTappedEvent)

MatchController.HandleCubeTapped(CubeTappedEvent)
  └─ MatchController.ProcessCubeTap() [Coroutine]
      └─ GameStateController.SetProcessingMove(true)
      └─ GridController.GetCell(x, y)
          └─ GridDataService.GetCell(x, y)
              └─ GridDataService.GetExtendedCell(x, y + bufferRows)
      └─ MatchDetectorService.FindMatchingGroup(cell)
          └─ MatchDetectorService.FindMatchingNeighbors() [Recursive]
              └─ GridController.GetAdjacentCells(x, y)
                  └─ GridDataService.GetAdjacentCells(x, y)
                      └─ Returns List<GridCell>
              └─ Recurses for each adjacent matching cube
      └─ MatchValidator.IsValidMatch(group)
      └─ MatchValidator.CanCreateRocket(group)
      └─ GameStateController.UseMove()
          └─ EventBus.Publish(MovesChangedEvent)
      └─ (If rocket) RocketController.AnimateRocketCreation()
      └─ (If no rocket) Destroy matching cubes
          └─ Check if goal cube: ObstacleController.IsGoalCube()
          └─ Play appropriate sound (goal cube or regular)
          └─ Publish ItemDestroyedEvent
      └─ MatchController.DamageAdjacentObstacles()
      └─ EventBus.Publish(MatchProcessedEvent)
      └─ Wait for projectiles
      └─ WaitForGravityToComplete()
      └─ GameStateController.SetProcessingMove(false)
```

### 6.2 Gravity/Refill Flow

```
EventBus.Publish(GravityStartedEvent)

GravityController.HandleGravityStarted(GravityStartedEvent)
  └─ GravityController.ProcessGravity() [Coroutine]
      └─ GravityController.ProcessAllFalls() [Coroutine]
          └─ Loop: for each column, bottom to top
              └─ GravityService.CalculateFallDistance(x, y)
                  └─ Checks cells below until non-empty
                  └─ Allows ducks to enter collection rows
              └─ GravityService.PrepareFallOperation()
                  └─ Creates FallOperation struct
                  └─ Adds to currentWave list
                  └─ If duck landing in collection row: CollectDuckImmediately()
          └─ FallAnimator.AnimateFalls(currentWave) [Coroutine]
              └─ Animates all items in wave simultaneously
              └─ Uses AnimationCurve for easing
              └─ Updates positions every frame
          └─ Repeat until no more falls (max 20 iterations)
      └─ GravityController.CollectDucksInCollectionRow()
          └─ For each duck in collection rows:
              └─ GoalCollectionAnimationService.AnimateGoalCollection()
                  └─ Animate from grid to UI
                  └─ Publish GoalCollectionAnimationCompleteEvent
              └─ Or publish ObstacleDestroyedEvent directly
      └─ GridController.RepopulateBufferRow()
          └─ For each empty cell in buffer rows:
              └─ GridController.SpawnItemInExtendedGrid(randomCube, x, y)
                  └─ PoolManager.Instance.GetItem(ItemType, parent)
                  └─ BaseItem.Initialize(itemType)
                  └─ GridCell.SetItem(item)
                  └─ EventBus.Publish(ItemSpawnedEvent)
      └─ EventBus.Publish(GridUpdatedEvent)
      └─ EventBus.Publish(GravityCompletedEvent)
```

### 6.3 Special Tile Activation (Rocket) Flow

```
InputController.Update()
  └─ InputController.ProcessTap()
      └─ InputHandler.GetTappedItem()
      └─ InputController.HandleItemTapped()
          └─ (If RocketItem) EventBus.Publish(RocketTappedEvent)

RocketController.HandleRocketTapped(RocketTappedEvent)
  └─ RocketController.ProcessRocketExplosion() [Coroutine]
      └─ GameStateController.SetProcessingMove(true)
      └─ GameStateController.UseMove()
      └─ RocketService.GetAllConnectedRockets(position)
          └─ Checks 4 directions for RocketItem (max 2 connected)
      └─ (If combo) RocketController.ProcessRocketCombo()
          └─ DestroyRocket() for all rockets
          └─ (If 2 rockets) RocketService.DamageItemsInCrossPattern(center)
          └─ (If 3+ rockets) RocketService.DamageItemsIn3x3Area(center)
              └─ For each cell in area:
                  └─ RocketService.DamageItem(item)
                      └─ (If CubeItem) HandleCubeDamage()
                      └─ (If ObstacleItem) HandleObstacleDamage()
                      └─ (If RocketItem) TriggerChainReaction()
          └─ RocketProjectileService.SpawnCrossProjectiles() or SpawnComboProjectiles()
      └─ (If single) RocketController.ProcessSingleRocketExplosion()
          └─ DestroyRocket(rocket)
          └─ RocketService.GetExplosionDirection(rocketType)
          └─ RocketProjectileService.AnimateProjectile() [2x Coroutine]
              └─ Animates projectile along path
              └─ Damages items in path
      └─ RocketProjectileService.WaitForAllProjectilesToComplete()
      └─ WaitForGravityToComplete()
      └─ GameStateController.SetProcessingMove(false)
```

### 6.4 Level Complete Flow

```
MatchController.ProcessCubeTap() [or RocketController.ProcessRocketExplosion()]
  └─ GameStateController.CheckWinCondition()
      └─ ObstacleController.Instance.AreAllGoalsCleared()
          └─ Checks obstaclesRemaining dictionary
          └─ Checks cubesRemaining dictionary
              └─ Returns true if all values == 0

GameStateController.WinLevel()
  └─ MusicManager.Instance.PlayEndGameMusic(true)
  └─ AudioManager.Instance.PlayGameWonSoundDelayed() [Coroutine, 0.5s delay]
  └─ SaveManager.Instance.MarkLevelCompleted(currentLevel)
      └─ SaveData.MarkLevelCompleted(level)
      └─ SaveData.Save()
          └─ JsonUtility.ToJson(this)
          └─ PlayerPrefs.SetString("SaveData", json)
          └─ PlayerPrefs.Save()
      └─ Advances currentLevel if needed
  └─ SaveManager.Instance.AreAllLevelsCompleted()
      └─ Checks all levels in LevelManager
  └─ GameStateController.ChangeGameState(finished ? Finished : GameWon)
      └─ EventBus.Publish(GameStateChangedEvent)
  └─ EventBus.Publish(LevelWonEvent)

GameplayUIController (or other UI) listens to LevelWonEvent
  └─ Shows win popup
```

### 6.5 Duck Collection Flow

```
GravityController.ProcessGravity()
  └─ GravityController.CollectDucksInCollectionRow()
      └─ For each duck in collection rows:
          └─ GoalCollectionAnimationService.AnimateGoalCollection()
              └─ Creates animated duplicate
              └─ Animates from grid to UI (0.6s)
              └─ Particle effects
              └─ EventBus.Publish(GoalCollectionAnimationCompleteEvent)
          └─ Or EventBus.Publish(ObstacleDestroyedEvent)

ObstacleController.HandleGoalCollectionAnimationComplete(GoalCollectionAnimationCompleteEvent)
  └─ ObstacleController.DecrementObstacleGoal(duckType)
      └─ Decrements obstaclesRemaining[duckType]
      └─ EventBus.Publish(GoalUpdatedEvent)
      └─ ObstacleController.AreAllGoalsCleared()
          └─ If true: GameStateController.WinLevel()
```

---

## 7. Data Ownership & Persistence

### 7.1 Level Data

**Storage:** JSON files in `Assets/Resources/levels/level_*.json`

**Format:**
```json
{
  "level_number": 1,
  "grid_width": 8,
  "grid_height": 8,
  "move_count": 20,
  "grid": ["r", "g", "b", "y", "p", "ba", "du", ...],
  "cube_goals": [
    { "cube_type": "r", "count": 10 },
    { "cube_type": "g", "count": 5 }
  ]
}
```

**Loading:**
- **`LevelManager.LoadAllLevels()`** — Scans directory at startup, loads all JSON files
- **`LevelManager.LoadLevelFromJSON(levelNumber)`** — Uses `Resources.Load<TextAsset>()`, parses with `JsonUtility.FromJson<LevelData>()`
- **Caching:** All levels stored in `Dictionary<int, LevelData>` in memory

**File:** `Assets/Scripts/Infrastructure/Data/LevelData.cs`

### 7.2 Player Progress

**Storage:** `PlayerPrefs` key `"SaveData"` (JSON string)

**Format:**
```json
{
  "currentLevel": 5,
  "levelCompleted": [true, true, true, true, false, false, ...]
}
```

**Class:** `Assets/Scripts/Infrastructure/Data/SaveData.cs`

**Methods:**
- **`SaveData.Save()`** — `JsonUtility.ToJson(this)` → `PlayerPrefs.SetString("SaveData", json)` → `PlayerPrefs.Save()`
- **`SaveData.Load()`** — `PlayerPrefs.GetString("SaveData")` → `JsonUtility.FromJson<SaveData>(json)`
- **`SaveManager.SaveGame()`** — Wrapper that calls `currentSave.Save()`
- **`SaveManager.MarkLevelCompleted(level)`** — Updates array, calls `SaveGame()`

**Timing:** Synchronous (blocking I/O, but fast on modern devices)

### 7.3 Game Configuration

**Storage:** ScriptableObject asset (`GameConfig.asset`)

**File:** `Assets/Scripts/Infrastructure/Data/GameConfig.cs`

**Fields:**
- Grid settings (cubeSize, gridSpacing)
- Animation settings (fallSpeed, explosionDelay, rocketCreationDuration, cubeDestroyDuration)
- Animation curves (fallCurve, explosionCurve, scaleCurve)

**Access:** `ConfigurationManager.Instance.GameConfig` or `GameStateController.Instance.Config`

**Editing:** Via Unity Inspector on ScriptableObject asset

---

## 8. Performance & Concurrency Considerations

### 8.1 Hot Paths

#### **Per-Frame Update() Loops:**
1. **`InputController.Update()`**
   - Called every frame
   - Checks input state, processes taps
   - **Optimization:** Early return if input locked or invalid state

2. **`AudioManager.Update()`**
   - Cleans up finished `AudioSource` components
   - Iterates `activeAudioSources` list
   - **Optimization:** Only checks if list has items

3. **`ParticleEffectManager.Update()`**
   - Updates all active particles
   - Iterates `activeParticles` list, removes expired
   - **Optimization:** Uses reverse iteration for safe removal

#### **Heavy Loops:**
1. **Match Detection** — `MatchDetectorService.FindMatchingGroup()`
   - Flood-fill algorithm: O(n) where n = connected matching cubes
   - Recursive calls: `FindMatchingNeighbors()`
   - **Bottleneck:** Deep recursion on large matching groups
   - **Fix:** Consider iterative BFS instead of recursion

2. **Gravity Calculation** — `GravityController.ProcessAllFalls()`
   - Nested loops: `for x in width, for y in totalHeight`
   - Called multiple times per gravity cycle (max 20 iterations)
   - **Bottleneck:** O(width × totalHeight × iterations)
   - **Optimization:** Already batches operations (`maxOperationsPerFrame = 30`)

3. **Grid Initialization** — `GridController.InitializeGrid()`
   - Creates all cells: `width × (height + bufferRows + collectionRows)`
   - Spawns all items
   - **Bottleneck:** Instantiation of many GameObjects
   - **Optimization:** Uses object pooling for items, but cells are instantiated fresh

### 8.2 Potential Race Conditions

#### **1. Multiple Coroutines Modifying Grid State**
**Location:** `MatchController.ProcessCubeTap()` and `GravityController.ProcessGravity()` running simultaneously

**Issue:** If gravity starts before match processing completes, items might be moved while being destroyed.

**Current Protection:** `GameStateController.IsProcessingMove` flag prevents input, and gravity waits for match processing via `WaitForGravityToComplete()`.

**Fix:** Already handled via coroutine waiting.

#### **2. Event Bus Handler Execution Order**
**Location:** `EventBus.Publish()` iterates handlers in reverse order (`for (int i = handlers.Count - 1; i >= 0; i--)`)

**Issue:** Handler execution order is not guaranteed if multiple handlers subscribe to same event.

**Fix:** Use priority system or explicit ordering if needed.

#### **3. Pool Return During Animation**
**Location:** Items returned to pool while animations still running

**Issue:** `PoolManager.ReturnItem()` might be called while `FallAnimator` is still animating the item.

**Current Protection:** Items are disabled (`SetActive(false)`) but transform might still be animated.

**Fix:** Ensure animations complete before returning to pool, or check `gameObject.activeInHierarchy` in animator.

### 8.3 Performance Bottlenecks

1. **Grid Cell Instantiation** — Creates all cells on level start (could be 100+ GameObjects)
   - **Suggestion:** Pool `GridCell` objects or use object pooling

2. **Match Detection Recursion** — Deep recursion on large groups
   - **Suggestion:** Use iterative BFS with `Queue<GridCell>`

3. **Particle Updates** — Updates all particles every frame
   - **Suggestion:** Use object pooling (already implemented) and limit max particles

4. **Audio Source Pool** — Creates new sources if pool exhausted
   - **Suggestion:** Increase pool size or limit concurrent sounds

---

## 9. Testing & Instrumentation Suggestions

### 9.1 Unit Test Targets

#### **Pure Logic Classes (No Unity Dependencies):**
1. **`MatchValidator.cs`** — Static validation methods
   - Test `IsValidMatch()` with various group sizes
   - Test `CanCreateRocket()` with 4, 5, 6+ cubes

2. **`MatchDetectorService.cs`** — Match detection algorithm
   - Mock `GridController`, test flood-fill on known grids
   - Test edge cases: single cube, large group, disconnected groups

3. **`GravityService.cs`** — Fall distance calculation
   - Mock `GridController`, test fall distances for various scenarios
   - Test with obstacles, empty columns, full columns, collection rows

4. **`SaveData.cs`** — Save/load logic
   - Test JSON serialization/deserialization
   - Test level completion tracking
   - Test edge cases: invalid level numbers, corrupted data

#### **Integration Test Targets:**
1. **Match Processing Flow** — Test full flow from input → match → destruction → gravity
2. **Rocket Creation** — Test match → rocket creation → spawn
3. **Rocket Explosion** — Test rocket tap → explosion → damage → gravity
4. **Duck Collection** — Test duck fall → collection row → goal update
5. **Goal Collection Animation** — Test animation from grid to UI
6. **Level Progression** — Test level complete → save → next level load

### 9.2 Logging Suggestions

#### **Match Detection:**
```csharp
// In MatchDetectorService.FindMatchingGroup()
Debug.Log($"[MatchDetection] Starting at ({cell.x}, {cell.y}), color: {targetType}");
Debug.Log($"[MatchDetection] Found {group.Count} matching cubes");
```

#### **Gravity Processing:**
```csharp
// In GravityController.ProcessAllFalls()
Debug.Log($"[Gravity] Iteration {fallIterations}, {currentWave.Count} items falling");
```

#### **Duck Collection:**
```csharp
// In GravityController.CollectDucksInCollectionRow()
Debug.Log($"[DuckCollection] Collected {duckCount} ducks from collection rows");
```

#### **Event Publishing:**
```csharp
// In EventBus.Publish() (add logging flag)
if (enableEventLogging)
    Debug.Log($"[EventBus] Published {eventType.Name}, {handlers.Count} handlers");
```

#### **Performance Profiling:**
```csharp
// Wrap heavy operations
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... operation ...
Debug.Log($"[Performance] {operationName} took {stopwatch.ElapsedMilliseconds}ms");
```

### 9.3 Analytics Hooks

**Suggested locations:**
1. **Level Start** — `GameStateController.StartLevel()`
   - Track: level number, moves available, goal counts (obstacles and cubes)

2. **Match Made** — `MatchController.ProcessCubeTap()`
   - Track: match size, rocket created, time since level start, goal cubes matched

3. **Rocket Used** — `RocketController.ProcessRocketExplosion()`
   - Track: rocket type, combo (yes/no), items destroyed

4. **Duck Collected** — `GravityController.CollectDucksInCollectionRow()`
   - Track: ducks collected, time since level start

5. **Level Complete** — `GameStateController.WinLevel()` / `LoseLevel()`
   - Track: level number, moves remaining, time taken, win/lose

6. **Goal Updated** — `ObstacleController.DecrementObstacleGoal()` / `DecrementCubeGoal()`
   - Track: goal type, remaining count, level number

**Sample Implementation:**
```csharp
public static class Analytics
{
    public static void TrackEvent(string eventName, Dictionary<string, object> parameters)
    {
        // Integrate with analytics SDK (e.g., Unity Analytics, Firebase)
        Debug.Log($"[Analytics] {eventName}: {JsonUtility.ToJson(parameters)}");
    }
}

// Usage:
Analytics.TrackEvent("match_made", new Dictionary<string, object>
{
    { "level", currentLevel },
    { "match_size", matchingGroup.Count },
    { "rocket_created", shouldCreateRocket },
    { "goal_cubes_matched", goalCubesMatched }
});
```

---

## 10. Improvement Opportunities and Risks

### 10.1 Prioritized Improvements

#### **1. Add Input Lock During Gravity (HIGH PRIORITY)**
**Location:** Already handled via `WaitForGravityToComplete()` coroutine waiting

**Status:** Already implemented

#### **2. Extract Interfaces for Testing (MEDIUM PRIORITY)**
**Location:** Multiple managers

**Issue:** Hard to unit test due to static singleton dependencies.

**Fix:** Create interfaces:
```csharp
public interface IGridController
{
    GridCell GetCell(int x, int y);
    BaseItem GetItem(int x, int y);
    void SpawnItem(ItemType type, int x, int y);
}

// Inject via constructor or property
public class MatchDetectorService
{
    private IGridController gridController;
    public MatchDetectorService(IGridController grid) { this.gridController = grid; }
}
```

**Risk:** Medium — Requires refactoring many classes

#### **3. Replace Recursive Match Detection with Iterative BFS (MEDIUM PRIORITY)**
**Location:** `MatchDetectorService.cs:26`

**Issue:** Stack overflow risk on very large matching groups.

**Fix:** Use iterative BFS with `Queue<GridCell>`

**Risk:** Low — Algorithmic improvement, no behavior change

#### **4. Add Null Checks in Event Handlers (MEDIUM PRIORITY)**
**Location:** All event handlers

**Issue:** Handlers assume instances exist (e.g., `GridController.Instance`).

**Fix:** Add null checks:
```csharp
private void HandleLevelStarted(LevelStartedEvent evt)
{
    if (GridController.Instance == null) return;
    // ... rest of code
}
```

**Risk:** Low — Defensive programming

#### **5. Pool GridCell Objects (LOW PRIORITY)**
**Location:** `GridController.cs`

**Issue:** Creates new `GridCell` objects on every level load.

**Fix:** Create `GridCellPool`, reuse cells across levels.

**Risk:** Medium — Requires careful cleanup

#### **6. Reduce Coupling Between MatchController and RocketController (MEDIUM PRIORITY)**
**Location:** `MatchController.cs:68`

**Issue:** Direct dependency on `RocketController.Instance`.

**Fix:** Use event: `EventBus.Publish(new RocketCreationRequestEvent { ... })`

**Risk:** Medium — Requires event system extension

#### **7. Add Validation for Grid Bounds (LOW PRIORITY)**
**Location:** `GridDataService.cs`, `GridController.cs`

**Issue:** Some methods don't validate bounds before array access.

**Fix:** Add bounds checks in `GetExtendedCell()`, `GetCell()`, etc.

**Risk:** Low — Defensive programming

#### **8. Extract Constants for Magic Numbers (LOW PRIORITY)**
**Location:** Multiple files

**Issue:** Magic numbers scattered (e.g., `0.2f`, `0.5f`, `20`).

**Fix:** Create `GameplayConstants` class:
```csharp
public static class GameplayConstants
{
    public const float MATCH_PROCESSING_DELAY = 0.2f;
    public const float ROCKET_CREATION_DURATION = 0.5f;
    public const int MAX_GRAVITY_ITERATIONS = 20;
    public const int ROCKET_MATCH_THRESHOLD = 5;
}
```

**Risk:** Low — Code organization

#### **9. Add Error Handling for JSON Parsing (MEDIUM PRIORITY)**
**Location:** `LevelManager.cs:75`, `SaveData.cs`

**Issue:** JSON parsing can fail silently or crash.

**Fix:** Add try-catch with logging:
```csharp
try
{
    levelData = JsonUtility.FromJson<LevelData>(jsonFile.text);
}
catch (Exception e)
{
    Debug.LogError($"Failed to parse level {levelNumber}: {e.Message}");
    return null;
}
```

**Risk:** Low — Error handling

#### **10. Add Unit Tests for Core Logic (HIGH PRIORITY)**
**Location:** Create `Tests/` folder

**Issue:** No unit tests found in codebase.

**Fix:** Create test assembly, add tests for `MatchValidator`, `MatchDetectorService`, `SaveData`, `GravityService`.

**Risk:** Low — New code addition

---

## 11. Mapping of Gameplay → Code Responsibilities

| Gameplay Concept | Code Classes/Methods Responsible |
|-----------------|----------------------------------|
| **Player Input** | `InputController.cs:HandleInput()` → `InputHandler.GetTappedItem()` → Publishes `CubeTappedEvent` or `RocketTappedEvent` |
| **Tile Spawn** | `GridController.cs:SpawnItem()` → `PoolManager.Instance.GetItem()` → `BaseItem.Initialize()` → `GridCell.SetItem()` |
| **Match Detection** | `MatchController.cs:HandleCubeTapped()` → `MatchDetectorService.FindMatchingGroup()` → `MatchValidator.IsValidMatch()` |
| **Match Processing** | `MatchController.cs:ProcessCubeTap()` → Destroys cubes → `DamageAdjacentObstacles()` → Publishes `MatchProcessedEvent` |
| **Rocket Creation** | `MatchController.cs:ProcessCubeTap()` → `MatchValidator.CanCreateRocket()` (≥5) → `RocketAnimator.AnimateRocketCreation()` → `GridController.SpawnItem()` |
| **Rocket Activation** | `RocketController.cs:HandleRocketTapped()` → `ProcessRocketExplosion()` → `RocketProjectileService.AnimateProjectile()` → `RocketService.DamageItemsIn3x3Area()` or `DamageItemsInCrossPattern()` |
| **Gravity** | `GravityController.cs:ProcessGravity()` → `GravityService.CalculateFallDistance()` → `FallAnimator.AnimateFalls()` → `GridController.RepopulateBufferRow()` |
| **Duck Collection** | `GravityController.cs:CollectDucksInCollectionRow()` → `GoalCollectionAnimationService.AnimateGoalCollection()` → `ObstacleController.DecrementObstacleGoal()` |
| **Obstacle Damage** | `MatchController.DamageAdjacentObstacles()` / `RocketService.DamageItem()` → `ObstacleItem.TakeDamage()` → `ObstacleBehavior.TakeDamage()` → Publishes `ObstacleDestroyedEvent` |
| **Obstacle Destruction** | `ObstacleItem.TakeDamage()` → `ObstacleBehavior.IsDestroyed` → `ObstacleController.HandleObstacleDestroyed()` → Publishes `GoalUpdatedEvent` |
| **Cube Goal Collection** | `MatchController.PlayCubePopEffects()` → Checks `ObstacleController.IsGoalCube()` → Publishes `ItemDestroyedEvent` → `ObstacleController.HandleItemDestroyed()` → `GoalCollectionAnimationService.AnimateGoalCollection()` → `DecrementCubeGoal()` |
| **Level Win** | `GameStateController.CheckWinCondition()` → `ObstacleController.AreAllGoalsCleared()` → `GameStateController.WinLevel()` → `SaveManager.MarkLevelCompleted()` |
| **Level Lose** | `GameStateController.CheckLoseCondition()` → `GameStateController.LoseLevel()` → Publishes `LevelLostEvent` |
| **Level Load** | `SceneTransitionManager.LoadLevelScene()` → `GameStateController.StartLevel()` → `GridController.InitializeGrid()` → `ObstacleController.InitializeGoals()` |
| **Save Progress** | `SaveManager.MarkLevelCompleted()` → `SaveData.MarkLevelCompleted()` → `SaveData.Save()` → `PlayerPrefs.SetString()` |
| **Audio (Cube Break)** | `MatchController.ProcessCubeTap()` → `AudioManager.PlayCubeBreakSound()` or `PlayGoalCubeBreakSound()` → `AudioSource.Play()` |
| **Audio (Rocket)** | `RocketController.ProcessRocketExplosion()` → `AudioManager.PlayRocketPopSound()` / `PlayComboRocketPopSound()` |
| **Particles** | `MatchController.ProcessCubeTap()` → `ParticleEffectManager.SpawnCubeBurst()` → `PoolManager.GetParticle()` → `ParticleElement.Setup()` |
| **Scene Transition** | `SceneTransitionManager.LoadMainScene()` / `LoadLevelScene()` → `TransitionController.FadeOut()` → `SceneManager.LoadSceneAsync()` → `TransitionController.FadeIn()` |
| **UI Updates (Moves)** | `GameStateController.UseMove()` → Publishes `MovesChangedEvent` → `GameplayUIController.HandleMovesChanged()` → Updates `movesText` |
| **UI Updates (Goals)** | `ObstacleController.HandleObstacleDestroyed()` / `HandleItemDestroyed()` → Publishes `GoalUpdatedEvent` → `GameplayUIController.HandleGoalUpdated()` → Updates `GoalItem` count |
| **Object Pooling** | `PoolManager.GetItem()` → `GenericPool<T>.Get()` → `IPoolable.OnSpawn()` → Returns pooled object |
| **Object Return** | `PoolManager.ReturnItem()` → `IPoolable.OnReturnToPool()` → `GenericPool<T>.Return()` → Object disabled and queued |

---

## 12. Top 10 Files for New Developers

**Priority Order:**

1. **`Assets/Scripts/Core/Managers/GameStateController.cs`** — Central state machine, orchestrates game flow
2. **`Assets/Scripts/Core/Events/EventBus.cs`** — Event system used throughout codebase
3. **`Assets/Scripts/Core/Events/GameEvents.cs`** — All event type definitions
4. **`Assets/Scripts/Features/Grid/Controllers/GridController.cs`** — Grid management, item spawning
5. **`Assets/Scripts/Features/Matching/MatchController.cs`** — Match processing logic
6. **`Assets/Scripts/Features/Input/InputController.cs`** — Input handling
7. **`Assets/Scripts/Features/Gravity/GravityController.cs`** — Gravity and refill logic, duck collection
8. **`Assets/Scripts/Features/Items/Rockets/RocketController.cs`** — Rocket explosion logic
9. **`Assets/Scripts/Infrastructure/Data/LevelData.cs`** — Level data structure
10. **`Assets/Scripts/Shared/Enums/GameEnums.cs`** — All enums (ItemType, GameState, etc.)

---

## Appendix A: File Structure Reference

### Core Systems
- `Core/Managers/GameStateController.cs` — State machine
- `Core/Events/EventBus.cs` — Event system
- `Core/Events/GameEvents.cs` — Event definitions
- `Core/Managers/ConfigurationManager.cs` — Config access

### Managers
- `Infrastructure/Managers/LevelManager.cs` — Level loading
- `Infrastructure/Managers/SaveManager.cs` — Save system
- `Infrastructure/Managers/SceneTransitionManager.cs` — Scene transitions

### Gameplay
- `Features/Grid/Controllers/GridController.cs` — Grid management
- `Features/Grid/Services/GridDataService.cs` — Grid data
- `Features/Grid/Services/GridLayoutService.cs` — Layout calculations
- `Features/Matching/MatchController.cs` — Match processing
- `Features/Matching/MatchDetectorService.cs` — Match detection
- `Features/Matching/MatchValidator.cs` — Match validation
- `Features/Input/InputController.cs` — Input handling
- `Features/Gravity/GravityController.cs` — Gravity system
- `Features/Items/Rockets/RocketController.cs` — Rocket logic
- `Features/Items/Obstacles/ObstacleController.cs` — Goal tracking
- `Features/Goals/GoalCollectionAnimationService.cs` — Goal collection animations

### Data
- `Infrastructure/Data/LevelData.cs` — Level structure
- `Infrastructure/Data/SaveData.cs` — Save structure
- `Infrastructure/Data/GameConfig.cs` — Config ScriptableObject

### Items
- `Features/Items/Base/BaseItem.cs` — Base item class
- `Features/Items/Cubes/CubeItem.cs` — Cube implementation
- `Features/Items/Rockets/RocketItem.cs` — Rocket implementation
- `Features/Items/Obstacles/ObstacleItem.cs` — Obstacle implementation

### Services
- `Core/Services/Pooling/PoolManager.cs` — Object pooling
- `Core/Services/Pooling/GenericPool.cs` — Generic pool implementation
- `Infrastructure/Managers/AudioManager.cs` — Audio system
- `Presentation/Effects/ParticleEffectManager.cs` — Particle effects

---

## Appendix B: Key Constants and Timing Values

| Constant | Value | Location |
|----------|-------|----------|
| Rocket creation animation duration | 0.5s | `RocketAnimator.cs` |
| Match processing delay | 0.2s | `MatchController.cs:101` |
| Fade in duration | 0.8s | `TransitionController.cs` |
| Fade out duration | 0.6s | `TransitionController.cs` |
| Fall time per cell | Variable | `GravityService.cs` |
| Max gravity iterations | 20 | `GravityController.cs:101` |
| Max operations per frame (gravity) | 30 | `GravityController.cs:10` |
| Buffer rows | 1 | `GridController.cs:16` |
| Collection rows | 1 | `GridController.cs:17` |
| Cube pool size | 50 | `PoolManager.cs` |
| Rocket pool size | 20 | `PoolManager.cs` |
| Obstacle pool size | 30 | `PoolManager.cs` |
| Particle pool size | 100 | `PoolManager.cs` |
| Audio source pool size | 20 | `AudioManager.cs` |
| Rocket match threshold | 5 | `MatchValidator.cs:12` |
| Goal collection animation duration | 0.6s | `GoalCollectionAnimationService.cs:11` |

---

**End of Technical Report**

