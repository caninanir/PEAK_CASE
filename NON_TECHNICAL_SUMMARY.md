# Match-2 Pop Game - High-Level Flow Summary (Non-Technical)

**For:** Non-developer stakeholders  
**Purpose:** Understand game flow without code details

---

## Game Overview

"Match-2 Pop" is a match-2 puzzle game where players tap groups of matching colored cubes to clear them, collect goal items, and destroy obstacles. The game features special power-ups (rockets) that can be created by matching 5+ cubes, and these rockets can clear entire rows or columns when activated. Unique mechanics include collection rows where ducks must fall to be collected, and dual goal systems tracking both obstacle collection and cube collection.

---

## Game Flow (High Level)

### 1. Starting the Game

When the game launches:
- The game loads all available levels from files
- It checks what level the player last reached
- It shows the main menu where players can select levels

### 2. Starting a Level

When a player selects a level:
- The screen fades out
- The level scene loads (showing the game board)
- The game reads the level configuration (board size, number of moves, obstacle layout, cube goals)
- The board is created with cubes and obstacles placed according to the level design
- The game sets up the UI showing moves remaining and goals (which obstacles to collect and which cubes to collect)
- The screen fades in and gameplay begins

### 3. Player Makes a Move

When a player taps a cube:
- The game detects which cube was tapped
- It finds all connected cubes of the same color (matching group)
- If 2+ cubes match:
  - The game uses one move
  - If 5+ cubes match, a rocket is created (cubes animate together, then a rocket appears)
  - Otherwise, the matching cubes are destroyed (with particles and sound effects)
  - Special audio plays if a goal cube is destroyed
  - Any obstacles adjacent to the match take damage (balloons can be damaged, ducks cannot)
  - The game checks if all goals are cleared (win condition)
  - The game checks if moves are exhausted (lose condition)
- Gravity causes remaining cubes and obstacles to fall down
- Ducks that fall to the bottom collection rows are automatically collected
- New cubes spawn from the top to fill empty spaces
- The game unlocks input for the next move

### 4. Rocket Activation

When a player taps a rocket:
- The rocket explodes, clearing an entire row (horizontal rocket) or column (vertical rocket)
- If two rockets are adjacent, they create a cross-pattern explosion (horizontal + vertical)
- If three or more rockets are adjacent, they create a 3x3 area explosion
- All cubes and obstacles in the explosion path are destroyed/damaged
- If another rocket is hit by the explosion, it triggers a chain reaction
- Gravity refills the board
- Win/lose conditions are checked

### 5. Obstacle Collection

**Balloons:**
- Can be destroyed by being adjacent to a cube match (takes 1 damage)
- Can be destroyed by being hit by a rocket explosion (takes 1 damage)
- When destroyed, the goal counter decreases
- Visual feedback shows the balloon popping

**Ducks:**
- Cannot be damaged by matches or rockets
- Must fall to the collection rows at the bottom of the grid
- When a duck reaches a collection row, it is automatically collected
- An animated visual effect shows the duck flying to the goal display
- The goal counter decreases when collected

### 6. Goal System

The game tracks two types of goals:

**Obstacle Goals:**
- Collect balloons by destroying them
- Collect ducks by letting them fall to collection rows
- Goal counter shows how many of each obstacle type remain

**Cube Goals:**
- Collect specific quantities of colored cubes (red, green, blue, yellow, purple)
- Goal cubes make special sounds when destroyed
- Goal counter shows how many of each cube color remain

**Goal Collection Feedback:**
- When goals are collected, animated visual effects show items flying from the grid to the goal display
- Particle effects enhance the collection feedback
- The goal counter updates in real-time

### 7. Level Completion

**Winning:**
- When all goals (obstacles and cubes) are collected, the game:
  - Plays victory music and sound
  - Saves progress (marks level as completed)
  - Shows win screen
  - Player can proceed to next level or return to menu

**Losing:**
- When moves run out and goals remain, the game:
  - Plays defeat music and sound
  - Shows lose screen
  - Player can retry the level or return to menu

### 8. Progress Saving

The game automatically saves:
- Current level progress
- Which levels have been completed
- This data persists between game sessions

---

## Key Game Systems

### Board Management
- The game board is a grid of cells
- Each cell can contain a cube, rocket, obstacle, or be empty
- The board has a "buffer zone" above the visible area where new cubes spawn
- The board has a "collection zone" below the visible area where ducks are collected
- When cubes and obstacles fall, they animate smoothly to their new positions

### Matching System
- Players tap a cube to find matching groups
- Matching means connected cubes of the same color
- Groups of 2-4 cubes are destroyed normally
- Groups of 5+ cubes create a rocket

### Special Features
- **Rockets:** Clear entire rows or columns
- **Combos:** Two adjacent rockets create a cross-pattern explosion, three or more create a 3x3 explosion
- **Chain Reactions:** Rockets can trigger other rockets
- **Collection Rows:** Ducks must fall to collection rows to be collected
- **Goal Collection Animations:** Visual feedback when goals are collected
- **Visual Effects:** Particles, animations, and sounds provide feedback

### User Interface
- Top bar shows: current level number, moves remaining, goal items (obstacles and cubes to collect)
- Goal items show how many of each type remain
- Goal counters update in real-time as items are collected
- Popups appear for win/lose states

---

## Technical Highlights (Simplified)

- **Performance:** Uses object pooling to reuse game objects (cubes, particles) instead of creating/destroying them constantly
- **Smooth Animations:** All movements (falling, explosions, goal collection) are animated smoothly using curves
- **Event System:** Game systems communicate through events, keeping code organized
- **Modular Design:** Different systems (matching, gravity, rockets, goals) are separate and can be modified independently
- **Collection Row System:** Unique mechanic where ducks must fall to collection rows, cannot be damaged directly

---

## Player Experience Flow

1. **Main Menu** → Player selects level
2. **Level Load** → Board appears, goals shown (obstacles and cubes)
3. **Gameplay Loop:**
   - Player taps cubes
   - Matches are found and processed
   - Rockets can be created and activated
   - Ducks fall to collection rows and are collected
   - Goal cubes are tracked and collected
   - Board refills with gravity
   - Repeat until win or lose
4. **Result Screen** → Win or lose popup
5. **Next Action** → Continue to next level, retry, or return to menu

---

## Unique Game Mechanics

### Collection Rows
- Special rows at the bottom of the grid
- Ducks fall through the visible grid into collection rows
- When ducks reach collection rows, they are automatically collected
- Visual animations show ducks flying to the goal display

### Dual Goal System
- **Obstacle Goals:** Track balloons and ducks that need to be collected
- **Cube Goals:** Track specific quantities of colored cubes that need to be collected
- Both goal types must be completed to win the level
- Real-time goal tracking with visual feedback

### Goal Collection Animations
- When goals are collected, animated visual effects show items flying from the grid to the goal display
- Particle effects enhance the collection feedback
- Provides clear visual feedback for goal progress

---

**Note:** This summary describes the high-level flow. For detailed technical implementation, see the full Technical Report.

