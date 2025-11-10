# PEAK MATCH-2 POP GAME
**Unity 6000.0.32f1 | C# | Portrait (9:16) | Mobile-Optimized**

---

## PROJECT OVERVIEW

This project implements a complete match-2 pop puzzle game. Players clear obstacles and collect goal items by matching colored cubes, utilizing rockets for powerful combos, and strategically managing gravity to collect ducks in collection rows.

### Core Game Mechanics

**Match-2 System**
- Minimum 2 adjacent cubes (horizontal/vertical) can be matched and popped
- Groups of 5+ cubes create rockets at the tapped location
- Rocket direction (horizontal/vertical) is randomly determined
- Visual feedback with particle effects and audio cues

**Rocket System**
- Single rocket: Explodes in both directions along its axis
- Rocket combos: 2 adjacent rockets create cross-pattern explosions, 3+ rockets create 3x3 area explosions
- Rockets damage all items in their path including other rockets
- Chain reactions possible through rocket-to-rocket damage

**Obstacle Types**
- **Balloon**: 1 HP, damaged by adjacent blasts and rockets, can fall
- **Duck**: 1 HP, cannot be damaged, must fall to collection rows to be collected

**Goal System**
- **Obstacle Goals**: Collect balloons and ducks by destroying/collecting them
- **Cube Goals**: Collect specific quantities of colored cubes (red, green, blue, yellow, purple)
- Goals tracked in real-time with visual progress indicators
- Goal collection animations provide visual feedback

**Physics & Gravity**
- Items fall vertically only, cannot pass through other items
- Physics-based gravity curves provide natural acceleration
- New random cubes spawn from buffer rows to fill empty spaces
- Collection rows at bottom of grid collect ducks automatically
- Wave-based falling system ensures proper stacking order

---

## ARCHITECTURE OVERVIEW

The game follows a modular singleton-based architecture with clear separation of concerns:

### Event System

**EventBus** - Centralized event communication system
- Generic event bus using Dictionary<Type, List<object>> for type-safe event handling
- Subscribe/Unsubscribe pattern for decoupled system communication
- Publish method invokes all registered handlers for event type
- All events implement IGameEvent interface for type safety
- Enables reactive programming patterns throughout the codebase
- Handlers execute in reverse order (LIFO) for predictable behavior

**Event Types** (defined in GameEvents.cs)
- GameStateChangedEvent, LevelStartedEvent, LevelWonEvent, LevelLostEvent
- CubeTappedEvent, RocketTappedEvent, MatchFoundEvent, MatchProcessedEvent
- GravityStartedEvent, GravityCompletedEvent, GridInitializedEvent, GridUpdatedEvent
- ItemSpawnedEvent, ItemDestroyedEvent, ItemDamagedEvent
- ObstacleDestroyedEvent, GoalUpdatedEvent, MovesChangedEvent
- GoalCollectionAnimationCompleteEvent

### Core Manager Systems

**GameStateController** - Central game state controller
- Manages game states: MainMenu, Playing, GameWon, GameLost, Finished, Paused
- Handles move counting, win/lose conditions, level progression
- Tracks processing flags (move processing, gravity running, active animations, particles, projectiles)
- Publishes events through EventBus for UI updates and system coordination
- Singleton pattern ensures single source of truth for game state
- Coordinates level initialization and cleanup

**GridController** - Grid system coordinator
- Orchestrates all grid-related components through service composition
- GridDataService: Manages grid cell data structure and coordinate conversion
- GridLayoutService: Calculates cell sizes, positions, and grid centering
- Handles item spawning, positioning, and sibling order management
- Manages extended grid with buffer rows and collection rows
- Buffer rows: Spawn new cubes above visible grid
- Collection rows: Collect ducks that fall to bottom
- Publishes grid events for system coordination

**MatchController** - Match processing system
- Listens to cube tap events and processes matches
- MatchDetectorService: Flood-fill algorithm to find connected matching cubes
- MatchValidator: Validates match groups and rocket creation eligibility (2+ for match, 5+ for rocket)
- Handles match destruction, rocket creation, and obstacle damage
- Coordinates with gravity system after match processing

**GravityController** - Physics-based gravity system
- GravityService: Calculates fall distances and prepares fall operations
- FallAnimator: Animates item falling with custom gravity curves
- Multi-wave falling system ensures proper stacking order
- Batched animation processing for performance optimization
- Collects ducks in collection rows after gravity completes
- Spawns new cubes from buffer rows after gravity completes

**RocketController** - Rocket explosion system
- RocketService: Handles explosion direction, combo detection, and damage application
- RocketAnimator: Animates rocket creation from matched cubes
- RocketProjectileService: Animates projectile effects along explosion paths
- Processes single rockets, combos (cross and 3x3), and chain reactions
- Coordinates damage application and gravity triggers

**ObstacleController** - Goal tracking system
- Tracks remaining obstacle counts per type (balloons, ducks)
- Tracks remaining cube goal counts per color
- Updates UI goal displays in real-time through events
- Triggers win condition when all goals cleared
- Manages obstacle-specific damage rules through behavior system (ObstacleBehaviorFactory)
- Obstacle behaviors: BalloonBehavior, DuckBehavior handle damage logic and sprites

**InputController** - Touch input processing
- InputHandler: UI-based raycasting for precise touch detection
- InputValidator: Validates input state and game conditions
- Processes mouse and touch input, publishes tap events
- State-aware input prevents interaction during animations

**LevelManager** - Level loading and progression
- Loads level data from JSON files in Resources/levels/
- Manages level progression and completion states
- Provides level data access to other systems
- Handles level validation and error recovery

**SaveManager** - Data persistence
- Uses Unity PlayerPrefs for cross-platform save data
- Tracks level progression and completion states
- Provides editor tools for level manipulation
- Handles save data corruption and migration

**SceneTransitionManager** - Scene loading and transitions
- Manages scene transitions with fade effects
- Coordinates async scene loading with visual feedback
- Handles level scene initialization timing
- Provides smooth transitions between menu and gameplay scenes

---

## TECHNICAL IMPLEMENTATION

### Grid System Architecture

**GridCell System**
- Manages individual cell state with x,y coordinates
- Tracks current item occupancy and relationships
- Provides world positioning and placement validation
- Handles item setting/removal with proper cleanup

**Extended Grid Design**
- Buffer rows above visible grid for smooth item spawning
- Collection rows below visible grid for duck collection
- Extended grid coordinates vs visible grid coordinates separation
- Prevents visual "popping" of new items at grid top
- Allows for pre-calculated falling animations

**Item Hierarchy**
- BaseItem: Abstract base class for all grid items
- CubeItem: Colored cubes with matching logic (red, green, blue, yellow, purple)
- RocketItem: Directional rockets with explosion mechanics
- ObstacleItem: Multi-health obstacles with damage states (balloons, ducks)
- Handles grid positioning, pooling cleanup, UI positioning
- Provides virtual methods for item-specific behaviors

### Match Detection Algorithm

**MatchDetectorService**
- FindMatchingGroup: Flood-fill algorithm to find connected matching cubes
- Uses HashSet for visited tracking to prevent infinite loops
- Returns group of connected cells with same cube color
- Minimum 2 cubes required for valid match (MatchValidator.IsValidMatch)
- Recursive neighbor traversal for complete group detection
- Integrated with GridController for cell access and adjacency queries

**MatchValidator**
- Static validation methods for match groups
- IsValidMatch: Checks if group has 2+ cubes
- CanCreateRocket: Checks if group has 5+ cubes for rocket creation
- Provides clear separation of validation logic

### Physics-Based Gravity System

**GravityController Implementation**
- Multi-wave falling system for realistic physics
- GravityService calculates all possible falls before animation
- Groups items by fall waves (items can't fall through others)
- FallAnimator animates each wave simultaneously for performance
- Collects ducks in collection rows after all falls complete
- Spawns new cubes from buffer rows after gravity completes

**Physics Features**
- Custom AnimationCurve for realistic gravity acceleration
- Landing bounce effects for visual polish (configurable)
- Batched animations with maxOperationsPerFrame limit (30 default)
- RectTransform caching in GravityService to avoid expensive GetComponent calls
- Variable fall duration based on distance
- Subtle rotation effects during falling (optional)
- Column-based sound effects for landing items

**Collection Row System**
- Ducks fall through visible grid into collection rows
- Collection rows automatically collect ducks after gravity completes
- GoalCollectionAnimationService animates duck collection with visual feedback
- Ducks cannot be damaged, only collected via falling

### Rocket System Implementation

**RocketController Mechanics**
- RocketService checks for adjacent rockets before explosion (combo detection)
- Processes single rocket or combo explosion based on adjacency
- RocketProjectileService animates projectiles in both directions along axis
- RocketService damages all items in explosion path including other rockets
- Triggers gravity and updates hints after explosion
- Handles chain reactions through rocket-to-rocket damage

**Rocket Creation**
- RocketAnimator animates cubes gathering to center (0.5s duration)
- Smooth interpolation with scale effects during creation
- Audio feedback with pitch variation
- Spawns rocket at tapped location after animation

**Combo System**
- Detects adjacent rockets before explosion triggers
- 2 rockets: Creates cross-pattern explosion (horizontal + vertical)
- 3+ rockets: Creates expanding 3x3 explosion patterns
- Multiple rockets create larger area of effect
- Chain reactions possible through rocket-to-rocket damage
- Combo projectiles spawn in multiple directions for visual impact

### Goal System Architecture

**ObstacleController Goal Tracking**
- Tracks obstacle goals (balloons, ducks) from level grid
- Tracks cube goals (colored cubes) from level data
- Updates goal counts in real-time through events
- Triggers win condition when all goals cleared
- GoalCollectionAnimationService provides visual feedback for collected goals

**Goal Collection Animation**
- Animated visual feedback when goals are collected
- Items animate from grid position to goal UI display
- Particle effects enhance collection feedback
- Separate handling for ducks (collection row) vs other items

### UI System Architecture

**Event-Driven UI Updates**
- GameplayUIController subscribes to EventBus events for reactive updates
- Handles MovesChangedEvent: Updates move counter display
- Handles LevelStartedEvent: Initializes goal displays with current level data
- Handles GoalUpdatedEvent: Updates goal item counts in real-time
- GoalDisplayController manages goal item layout and updates
- Uses pooled UI elements for goal items to reduce garbage collection

**UI Controllers**
- GameplayUIController: Main gameplay UI (moves, level, goals)
- MenuUIController: Main menu UI management
- PopupController: Manages popup display and transitions
- BasePopup: Abstract class with fade/scale transition animations
- LosePopup: Game over scenarios with retry and main menu options
- Win scenarios use celebration system instead of traditional popups
- Proper event cleanup and animation coroutine management

### Performance Optimizations

**Object Pooling System**
- GenericPool<T> implementation for any Component type implementing IPoolable
- PoolManager: Centralized pooling system for all game objects
- Pre-allocates objects to eliminate instantiation during gameplay
- Type-specific pools: Cubes (50 per color), Rockets (20), Obstacles (30), Particles (100)
- Automatic pool expansion when needed
- Proper cleanup and return-to-pool lifecycle management

**Performance Features**
- RectTransform caching in GravityService for expensive component access
- Batched animation processing to minimize per-frame overhead (maxOperationsPerFrame)
- Efficient sibling index management for proper rendering order
- Pooled particle systems for all visual effects
- Coroutine-based animations for frame-independent performance
- AudioSource pooling (20 sources) prevents audio cutoff

**Memory Management**
- Proper event subscription cleanup in all managers
- Pool clearing on scene transitions to prevent memory leaks
- Cache invalidation for destroyed objects
- Automatic AudioSource pooling to prevent audio source exhaustion

---

## LEVEL SYSTEM

### Level Data Format
```json
{
  "level_number": 1,
  "grid_width": 6,
  "grid_height": 8,
  "move_count": 20,
  "grid": ["r", "g", "b", "y", "p", "ba", "du", ...],
  "cube_goals": [
    { "cube_type": "r", "count": 10 },
    { "cube_type": "g", "count": 5 }
  ]
}
```

**Item Codes**
- `r`, `g`, `b`, `y`, `p`: Red, Green, Blue, Yellow, Purple cubes
- `rand`: Random cube color
- `vro`, `hro`: Vertical/Horizontal rockets
- `ba`: Balloon obstacle (1 HP, can be damaged)
- `du`: Duck obstacle (1 HP, must fall to collection row)

### Level Progression
- Multiple predefined levels with increasing difficulty
- JSON files loaded from Resources/levels/ directory
- Automatic goal extraction from level grid data (obstacles) and cube_goals array
- Progress saved locally with PlayerPrefs
- Level data cached in memory for fast access

---

## AUDIO SYSTEM

**AudioManager** - Advanced sound management system
- **AudioSource Pooling**: 20 pre-allocated AudioSources to prevent audio cutoff during intense gameplay
- **Dynamic Pool Expansion**: Creates additional AudioSources when pool is exhausted
- **Cube Break Variations**: Randomized cube destruction sounds to prevent repetitive audio
- **Goal Cube Audio**: Special audio feedback for goal cubes when destroyed
- **Obstacle-Specific Audio**:
  - Balloon: Pop sound on destruction
  - Duck: Collection sound when falling to collection row
- **Rocket Audio System**:
  - Creation sound with slight pitch variation
  - Single rocket explosion with randomized pitch
  - Combo rocket explosion with increased volume and pitch variation
- **Pitch Randomization**: All sounds use random pitch (±5-10%) to prevent monotony
- **Volume Balancing**: Separate volume controls for SFX, UI, and master volume
- **Audio Priority System**: Uses Unity's AudioSource priority (0-256) for audio mixing
- **Performance Optimization**: AudioSource pool recycling, automatic cleanup of finished sounds
- **Game State Audio**: Victory and defeat sounds with delayed playback for dramatic effect

**MusicManager** - Adaptive music system
- **State-Based Music**: Different tracks for menu, gameplay, victory, and defeat states
- **Smooth Crossfading**: Fade-in/fade-out transitions between music states
- **End Game Music**: Special victory and defeat music scheduling
- **Volume Control**: Independent music volume control separate from SFX

---

## VISUAL EFFECTS

**ParticleEffectManager** - GPU-efficient particle system
- Pooled particle elements for performance
- Type-specific effects for cubes, rockets, obstacles
- Balloon pop effects with burst particles
- Celebration system for level completion
- Optimized for mobile rendering

**CelebrationManager** - Win state effects
- Multi-layered particle effects
- Screen-space fireworks and confetti
- Coordinated with audio for impact

**GoalCollectionAnimationService** - Goal collection feedback
- Animated visual feedback when goals are collected
- Items animate from grid position to goal UI display
- Particle effects enhance collection feedback
- Smooth interpolation with rotation and scale effects

---

## INPUT SYSTEM

**InputController** - Touch input processing
- **InputHandler**: UI-based raycasting using Unity's GraphicRaycaster for precise touch detection
- **InputValidator**: Validates input state, game conditions, and processing flags
- **Mobile Touch Handling**: Proper touch input support for mobile devices (mouse fallback)
- **Item Delegation**: Routes touch events to appropriate item tap handlers (cubes vs rockets)
- **Event Publishing**: Publishes CubeTappedEvent or RocketTappedEvent through EventBus
- **State-Aware Input**: Prevents input during animations and game state transitions
- **Input Locking**: Manages input lock state during move processing
- **Canvas Reference Management**: Automatically refreshes canvas references on scene changes

---

## CODE ORGANIZATION

```
Assets/Scripts/
├── Core/               # Core systems (GameStateController, EventBus, ConfigurationManager)
│   ├── Events/         # Event system (EventBus, GameEvents)
│   ├── Managers/       # Core managers (GameStateController, ConfigurationManager)
│   ├── Services/       # Core services (Pooling)
│   └── Utilities/      # Utility classes (ComponentCache)
├── Features/            # Gameplay systems
│   ├── Goals/          # Goal tracking and collection animations
│   ├── Gravity/        # Gravity system (GravityController, GravityService)
│   ├── Grid/           # Grid system (GridController, GridDataService, GridLayoutService)
│   ├── Input/          # Input handling (InputController, InputHandler, InputValidator)
│   ├── Items/          # Item-specific logic (Cubes, Rockets, Obstacles)
│   └── Matching/       # Matching system (MatchController, MatchDetectorService, MatchValidator)
├── Infrastructure/     # Infrastructure systems
│   ├── Camera/         # Camera control (AspectRatio)
│   ├── Data/           # Data structures (LevelData, SaveData, GameConfig)
│   └── Managers/       # Infrastructure managers (LevelManager, SaveManager, AudioManager, MusicManager, SceneTransitionManager)
├── Presentation/       # Presentation layer
│   ├── Animations/     # Animation systems (FallAnimator, RocketAnimator)
│   └── Effects/        # Visual effects (ParticleEffectManager, TransitionController, CelebrationController)
├── Shared/             # Shared code
│   └── Enums/          # Game enumerations (GameEnums)
├── UI/                 # UI components and controllers
│   ├── Components/     # UI components (BasePopup, GoalItem, LevelButton)
│   └── Controllers/     # UI controllers (GameplayUIController, MenuUIController, PopupController)
└── Editor/             # Unity editor tools (LevelEditorWindow, SaveEditorWindow)
```

---

## EDITOR TOOLS

**Level Editor Window** - Complete visual level editor
- **Visual Grid Editor**: Click-to-paint interface for designing levels in Unity editor
- **Real-Time Preview**: See exactly how levels will appear in-game while editing
- **Tool Palette**: Full selection of item types (cubes, rockets, obstacles) with color coding
- **Grid Resizing**: Dynamic grid width/height adjustment with automatic data conversion
- **Goal Configuration**: Set cube goals for each level
- **Level Management**:
  - Load/Save levels from JSON files
  - Create new levels with customizable parameters
  - Duplicate existing levels for quick iteration
  - Insert levels at specific positions in level sequence
- **Unsaved Changes Tracking**: Visual indicator for modified levels with save prompts
- **Level Validation**: Automatic validation of level data and goal configuration
- **Move Count Configuration**: Adjustable move limits per level
- **File Operations**: Export levels to Resources folder for runtime loading

**Save Editor Window** - Progress management tools
- **Current Save Inspection**: View current player progress and level completion
- **Level Manipulation**:
  - Jump to any level for testing
  - Mark levels as completed/incomplete
  - Reset individual level progress
- **Progress Control**:
  - Reset entire game progress
  - Complete all levels for testing purposes
  - Validate save data integrity

**Debug Features**
- **Visual Grid Debugging**: Gizmo-based grid cell visualization in Scene view
- **Pool Statistics**: Real-time monitoring of object pool usage and performance
- **Performance Monitoring**: Frame rate and memory usage tracking during gameplay
- **Audio Debug**: AudioSource pool utilization and sound effect testing
- **Level Data Validation**: Automatic checking for malformed level files

---

## MOBILE OPTIMIZATIONS

**Portrait Orientation Lock**
- 9:16 aspect ratio support with responsive UI
- Safe area handling for notched devices
- Touch input optimized for thumb interaction

**Performance Considerations**
- Object pooling eliminates garbage collection spikes
- Efficient UI element recycling
- Optimized texture formats and sprite atlasing
- Frame-rate independent animations using coroutines

**Battery Efficiency**
- Minimal particle counts for mobile GPUs
- Efficient audio mixing to reduce CPU load
- Optimized animation curves for smooth interpolation

---

## TECHNICAL HIGHLIGHTS

1. **Robust Architecture**: Clean separation of concerns with singleton controllers, service layer, and event-driven communication through EventBus

2. **Advanced Grid System**: Extended grid design with buffer zones and collection rows for smooth item spawning and duck collection, coordinate conversion between visible and extended grids

3. **Service-Oriented Design**: Clear separation between controllers (orchestration) and services (logic), enabling testability and maintainability

4. **Physics-Based Falling**: Custom gravity curves with realistic acceleration, landing bounce effects, and batched animation processing

5. **Comprehensive Pooling**: Centralized PoolManager with GenericPool<T> for items, particles, and UI elements, eliminating garbage collection spikes

6. **Event-Driven Communication**: EventBus system with typed events (IGameEvent) enables decoupled system communication

7. **Flexible Goal System**: Supports both obstacle goals (balloons, ducks) and cube goals (colored cubes) with real-time tracking and visual feedback

8. **Collection Row Mechanics**: Unique duck collection system where ducks must fall to collection rows, cannot be damaged directly

9. **Polish & Effects**: Celebration system, goal collection animations, visual hints with animations, smooth scene transitions, and comprehensive audio system

10. **Editor Integration**: Unity editor tools for level editing and progress management

11. **Mobile-First Design**: Touch input with state validation, portrait orientation, and performance optimized for mobile devices

---

**This implementation demonstrates production-ready code quality with attention to performance, maintainability, and user experience. The modular architecture supports easy expansion and modification while maintaining clean, testable code throughout.**

