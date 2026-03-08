# Unity Retree Sample Spec

## User spec (do not edit)

The sample is a simple space invader 2D game, with the following classes:

```
class GameController : RetreeUpdater
    Game game

    // Listens for changes to game
    override void Start()

    // Update game UI for active vs. inactive
    protected void OnGameNodeChanged()

    // anything else needed to fulfill below requirements

class GameUIController : MonoBehaviour
    // On Canvas. Manages start/end screen UI, score, and health text.
    void ShowStartScreen()
    void HideStartScreen()
    void UpdateScore(int score)
    void UpdateHealth(int health)

class Game : RetreeNode
    public RetreeList<Enemy> enemies = new()
    public Player player = new()
    public RetreeList<LaserProjectile> projectiles = new();
    public bool gameActive = false

    // adds new enemy to list and returns it
    protected void SpawnEnemy()

    // spawns new enemy every 10 seconds (5 max)
    // GameController listens to player health changes, when zero set gameActive to false
    public void Start()

    // Set gameActive = false
    // Reset player
    // Clear enemies and projectile
    public void End()

    public void SpawnProjectile(Ship shooter)

class Health : RetreeNode
    public int health
    [RetreeIgnore]
    public int startHealth

    // startHealth set to both health and startHealth on init
    public Health(int startHealth)

class LaserProjectile : RetreeNode
    [RetreeIgnore]
    public int damage = 10

    [RetreeIgnore]
    public readonly float xPos

    public float yPos

    [RetreeIgnore]
    public readonly float yDirection

    public LaserProjectile(float xPos, float startYPos, float yDirection)

    // Starts moving up / down according to yDirection, once per tick
    public void Start()
    protected void MoveUp()

    // Subtract damage from health
    // Get parent and remove item from list
    public void OnCollision(IHasHealth hit)

class Ship : RetreeNode
    public Health health
    public float xPos
    public float yPos

    Ship(int startHealth, float xPos, float yPos)

    // Gets parent node and SpawnProjectile in opposite direction as yPos
    public void Shoot()

class Player : Ship
    public int score = 0
    public Player() : base (100, 0, BottomEdgeOfScreen)

class Enemy : Ship
    public Enemy() : base (20, RandomXPosOnScreen, TopEdgeOfScreen)
    
    // Shoots every X seconds
    // Listens for OnHealthNodeChange
    public void Start()

    // If health below zero, get parent node and delete self from list
    protected void OnHealthChange()

// Create MonoBehaviour scripts with PlayerController, EnemyController, and ProjectileController that subscribe to their nodes on lazy init
// Rather than use `Update`, listen to the variables via "OnNodeUpdated" and update positions accordingly
// Show a red quad for enemies and blue quad for players
// Player presses space key to shoot projectile up
// Player can move left and right with arrow key
// When ProjectileController collides with PlayerController or EnemyController, call projectile.OnCollision
// In GameController, listen for deleted enemies and projectiles and delete their respective gameObjects
// When GameController detects deleted enemy, also increment player score by 1 if game is active
```

## Full spec

### 1. Project setup

- **Unity version:** 6000.0.40f1
- **Project location:** `samples/SpaceInvaders/`
- **Package reference:** `com.ryanbliss.retreecore` via local `file:` path in `Packages/manifest.json`
- **Render pipeline:** Built-in 2D. Orthographic camera sized so world coordinates span roughly ±8 horizontal, ±5 vertical.
- **Scene:** `Assets/Scenes/SpaceInvaders.unity` — single scene, set as default in Build Settings.

### 2. World constants

```
Screen bounds (world units, orthographic camera size = 5):
  Left   = -8
  Right  =  8
  Top    =  4.5
  Bottom = -4.5
  Player Y = -4 (near bottom)
  Enemy spawn Y = 3.5 (near top)
```

All positions and movement use world-space floats. Constants are defined in a static `GameConstants` class.

---

### 3. Retree node classes (pure C#, no Unity dependencies)

All node classes live in `Assets/Scripts/Nodes/`. They use the `RetreeCore` namespace only — no `UnityEngine` references.

#### 3.1 `Health : RetreeNode`

```
Fields:
  public int health             — current HP, observed by Retree
  [RetreeIgnore] public int startHealth  — max HP, not observed

Constructor(int startHealth):
  Sets both this.health and this.startHealth to startHealth.

Methods:
  public void TakeDamage(int amount):
    health = Math.Max(0, health - amount)

  public void Reset():
    health = startHealth

  public bool IsAlive => health > 0   (property, not observed)
```

#### 3.2 `LaserProjectile : RetreeNode`

```
Fields:
  [RetreeIgnore] public int damage = 10
  [RetreeIgnore] public readonly float xPos      — set in constructor, never changes
  public float yPos                               — observed, drives visual position
  [RetreeIgnore] public readonly float yDirection — +1 (up) or -1 (down)

Constructor(float xPos, float startYPos, float yDirection):
  Stores all three values.

Methods:
  public void Move(float speed):
    yPos += yDirection * speed
    Projectile does not self-remove; GameController handles off-screen cleanup.

  public void OnCollision(Ship hit):
    hit.health.TakeDamage(damage)
    var parent = Retree.Parent(this);
    if (parent is RetreeList<LaserProjectile> list)
        list.Remove(this);
```

#### 3.3 `Ship : RetreeNode`

```
Fields:
  public Health health           — child RetreeNode, observed for tree changes
  public float xPos              — observed, drives horizontal visual position
  public float yPos              — observed, drives vertical visual position

Constructor(int startHealth, float xPos, float yPos):
  this.health = new Health(startHealth)
  this.xPos = xPos
  this.yPos = yPos

Methods:
  public void Shoot():
    Walk up Retree.Parent chain to find the Game node.
    Call game.SpawnProjectile(this).
    Projectile direction is opposite of ship's vertical half:
      Player (bottom) fires up (+1), Enemy (top) fires down (-1).
    Direction = yPos < 0 ? 1f : -1f
```

#### 3.4 `Player : Ship`

```
Fields:
  public int score = 0           — observed

Constructor():
  base(100, 0f, GameConstants.PlayerY)

Methods:
  public void MoveLeft(float speed):
    xPos = Math.Max(GameConstants.LeftBound, xPos - speed)

  public void MoveRight(float speed):
    xPos = Math.Min(GameConstants.RightBound, xPos + speed)

  public void Reset():
    health.Reset()
    xPos = 0f
    score = 0
```

#### 3.5 `Enemy : Ship`

```
Fields:
  (inherits health, xPos, yPos from Ship)

Constructor():
  base(20, RandomXInBounds(), GameConstants.EnemySpawnY)

  RandomXInBounds():
    Random float between GameConstants.LeftBound + 1 and GameConstants.RightBound - 1.

Methods:
  (relies on GameController for movement and shooting timers)
```

#### 3.6 `Game : RetreeNode`

```
Fields:
  public RetreeList<Enemy> enemies = new()
  public Player player = new()
  public RetreeList<LaserProjectile> projectiles = new()
  public bool gameActive = false
  [RetreeIgnore] public float formationDirection = 1f   — current horizontal drift (+1 right, -1 left)

Methods:
  public Enemy SpawnEnemy():
    var enemy = new Enemy()
    enemies.Add(enemy)
    return enemy

  public void SpawnProjectile(Ship shooter):
    float direction = shooter.yPos < 0 ? 1f : -1f
    var proj = new LaserProjectile(
        shooter.xPos,
        shooter.yPos,
        direction
    )
    projectiles.Add(proj)

  public void StartGame():
    gameActive = true

  public void End():
    gameActive = false
    player.Reset()
    enemies.Clear()
    projectiles.Clear()
    formationDirection = 1f
```

---

### 4. MonoBehaviour controllers (Unity layer)

All controllers live in `Assets/Scripts/Controllers/`. They bridge Retree nodes to Unity GameObjects.

#### 4.1 `GameController : RetreeUpdater`

**Responsibilities:**
- Owns the `Game` node instance.
- Delegates UI updates to `GameUIController`.
- Spawns an enemy every 10 seconds (max 5 alive at once).
- Moves the enemy formation side-to-side: when any enemy hits a screen edge, all reverse direction and step down by 0.5 units.
- Moves all projectiles each frame.
- Removes off-screen projectiles (yPos > Top + 1 or yPos < Bottom - 1).
- Detects enemy deaths (enemy removed from `enemies` list) → increments `player.score` if game is active, destroys the enemy's GameObject.
- Detects projectile removal → destroys the projectile's GameObject.
- Detects player health reaching 0 → calls `game.End()`.
- Handles enemy random shooting (each enemy shoots at a random interval between 2–4 seconds).
- Listens to space key to start/restart game.

**Listener:**
- `game.RegisterOnTreeChanged(OnGameTreeChanged)` — single listener for all changes: `gameActive` toggle, health changes, enemy/projectile list mutations, score updates.

**UI:** Delegates to `GameUIController` (see §4.5) for start screen, score, and health display.

**Formation movement logic (in `Update`):**
```
if game is not active, skip.
float formationSpeed = 2f per second.
Move all enemies horizontally: enemy.xPos += formationDirection * formationSpeed * Time.deltaTime.
If any enemy.xPos hits LeftBound or RightBound:
  Reverse formationDirection.
  Step all enemies down by 0.5 units: enemy.yPos -= 0.5f for each enemy.
```

**Projectile movement (in `Update`):**
```
float projectileSpeed = 8f per second.
foreach projectile in game.projectiles:
  projectile.Move(projectileSpeed * Time.deltaTime)
```

**Enemy shooting (in `Update`):**
```
Track a Dictionary<Enemy, float> of next-shoot timers.
Each frame, decrement timers by Time.deltaTime.
When timer <= 0 for an enemy, call enemy.Shoot() and reset timer to Random.Range(2f, 4f).
```

**Tracking GameObjects:**
```
Dictionary<RetreeBase, GameObject> _nodeToGameObject
```
Maps each Enemy, Player, and LaserProjectile node to its visual GameObject. Used for cleanup when nodes are removed from lists.

#### 4.2 `PlayerController : MonoBehaviour`

- Created by `GameController` when game starts.
- Holds a reference to the `Player` node.
- Registers `player.RegisterOnNodeChanged(OnPlayerNodeChanged)`.
- `OnPlayerNodeChanged`: updates transform position from `player.xPos` and `player.yPos`.
- In `Update`: reads `Input.GetKey(KeyCode.LeftArrow)` / `Input.GetKey(KeyCode.RightArrow)` → calls `player.MoveLeft(speed * Time.deltaTime)` / `player.MoveRight(speed * Time.deltaTime)`. Player movement speed = 6f units/sec.
- In `Update`: reads `Input.GetKeyDown(KeyCode.Space)` → calls `player.Shoot()`.
- Visual: blue quad (1×1 unit), created programmatically via `GameObject.CreatePrimitive(PrimitiveType.Quad)` with a blue material.
- Has `BoxCollider2D` (trigger) + `Rigidbody2D` (kinematic) for collision detection.

#### 4.3 `EnemyController : MonoBehaviour`

- Created by `GameController` when an enemy is spawned.
- Holds a reference to the `Enemy` node.
- Registers `enemy.RegisterOnNodeChanged(OnEnemyNodeChanged)`.
- `OnEnemyNodeChanged`: updates transform position from `enemy.xPos` and `enemy.yPos`.
- Visual: red quad (1×1 unit) with a red material.
- Has `BoxCollider2D` (trigger) + `Rigidbody2D` (kinematic) for collision detection.

#### 4.4 `ProjectileController : MonoBehaviour`

- Created by `GameController` when a projectile is spawned.
- Holds a reference to the `LaserProjectile` node.
- Registers `projectile.RegisterOnNodeChanged(OnProjectileNodeChanged)`.
- `OnProjectileNodeChanged`: updates transform position from `projectile.xPos` and `projectile.yPos`.
- Visual: small white quad (0.2×0.6 unit).
- Has `BoxCollider2D` (trigger) + `Rigidbody2D` (kinematic) for collision detection.
- `OnTriggerEnter2D(Collider2D other)`:
  - If other has `PlayerController` → `projectile.OnCollision(playerController.Player)` (only if projectile direction is downward).
  - If other has `EnemyController` → `projectile.OnCollision(enemyController.Enemy)` (only if projectile direction is upward).

#### 4.5 `GameUIController : MonoBehaviour`

- Lives on the Canvas GameObject.
- Finds `StartText`, `ScoreText`, and `HealthText` child Text components via `GameObject.Find()` in `Awake()`.
- Public methods called by `GameController`:
  - `ShowStartScreen()` — activates StartText, hides ScoreText and HealthText.
  - `HideStartScreen()` — hides StartText, activates ScoreText and HealthText.
  - `UpdateScore(int score)` — sets ScoreText to `"Score: {score}"`.
  - `UpdateHealth(int health)` — sets HealthText to `"HP: {health}"`.

---

### 5. Scene hierarchy

```
SpaceInvaders (scene)
├── Main Camera          — Orthographic, size=5, position=(0,0,-10)
├── GameController       — Has GameController component
└── Canvas (Screen Space - Overlay) — Has GameUIController component
    ├── StartText        — "Press SPACE to Start", centered, large font
    ├── ScoreText        — Top-left, "Score: 0"
    └── HealthText       — Top-right, "HP: 100"
```

Player, Enemy, and Projectile GameObjects are instantiated at runtime by `GameController`.

---

### 6. Game flow

1. **App starts:** `GameController.Start()` creates the `Game` node, registers listeners, and shows the start screen.
2. **Player presses Space:** `game.StartGame()` sets `gameActive = true`. `OnGameTreeChanged` detects the change on next tick, hides start screen, spawns the player GameObject and first enemy.
3. **Gameplay loop (Update):**
   - Player input moves player and fires.
   - Enemy formation moves side-to-side, stepping down at edges.
   - Projectiles move each frame.
   - Enemy shoot timers tick down; enemies fire when ready.
   - Off-screen projectiles are removed.
   - Collisions detected via Unity physics triggers → `OnCollision` called.
4. **Enemy killed:** `OnCollision` reduces enemy health to 0 → `OnHealthChange` removes enemy from `enemies` list → `GameController` detects removal via tree listener → destroys enemy GameObject, increments score.
5. **Player killed:** Player health reaches 0 → `GameController` detects via tree listener → calls `game.End()` → `gameActive` set to false → `OnGameTreeChanged` on next tick shows start screen, cleans up all GameObjects.
6. **Restart:** Player presses Space again → flow returns to step 2.

---

### 7. PlayMode tests

Tests live in `Assets/Tests/` and use the Unity Test Framework (`com.unity.test-framework`). All tests run in PlayMode so that MonoBehaviour lifecycle, physics triggers, and `Retree.Tick()` via `RetreeUpdater` work correctly.

#### 7.1 Assembly definition

`Assets/Tests/SpaceInvaders.Tests.asmdef`:
```json
{
    "name": "SpaceInvaders.Tests",
    "references": [
        "Retree.Unity",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll",
        "RetreeCore.dll"
    ],
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### 7.2 Test helpers

`TestHelpers.cs` — shared utilities for all tests:

```
static class TestHelpers:
  // Creates a minimal GameController on a new GameObject, returns (GameController, Game).
  // Adds RetreeUpdater if not already on the object.
  static (GameController controller, Game game) CreateGameController()

  // Waits for N frames so Retree.Tick() runs and physics process.
  static IEnumerator WaitFrames(int count = 2)

  // Waits until a condition is true or timeout (default 5s), fails test on timeout.
  static IEnumerator WaitUntil(Func<bool> condition, float timeout = 5f)

  // Cleans up all GameObjects created during a test.
  static void CleanupScene()
```

#### 7.3 Node logic tests

`NodeTests.cs` — verifies pure Retree node behavior (no controllers needed, but runs in PlayMode so `Retree.Tick()` works via coroutine waits).

| Test | Description |
|------|-------------|
| `Health_TakeDamage_ReducesHealth` | `new Health(100)` → `TakeDamage(30)` → health == 70 |
| `Health_TakeDamage_ClampsToZero` | `TakeDamage(999)` → health == 0 |
| `Health_Reset_RestoresStartHealth` | `TakeDamage(50)` → `Reset()` → health == startHealth |
| `Player_MoveLeft_ClampsAtBound` | Move far left → xPos == `GameConstants.LeftBound` |
| `Player_MoveRight_ClampsAtBound` | Move far right → xPos == `GameConstants.RightBound` |
| `Player_Reset_RestoresDefaults` | Modify score/xPos/health → `Reset()` → all back to initial |
| `LaserProjectile_Move_UpdatesYPos` | `Move(1f)` with direction +1 → yPos increases by 1 |
| `LaserProjectile_OnCollision_DamagesAndRemovesSelf` | Add projectile to `game.projectiles`, call `OnCollision` → target health reduced, projectile removed from list |
| `Ship_Shoot_CreatesProjectile` | Player in game → `Shoot()` → `game.projectiles.Count == 1`, direction == +1 (upward) |
| `Enemy_Shoot_CreatesDownwardProjectile` | Enemy in game → `Shoot()` → projectile direction == -1 (downward) |
| `Game_SpawnEnemy_AddsToList` | `SpawnEnemy()` → `enemies.Count == 1` |
| `Game_End_ResetsState` | Start game, add enemies/projectiles → `End()` → `gameActive == false`, lists empty, player reset |
| `Game_TreeChanged_FiresOnHealthChange` | Register tree listener on game → damage player → tick → listener fires with health change |

#### 7.4 Controller integration tests

`ControllerTests.cs` — verifies MonoBehaviour controllers respond to Retree node changes and physics interactions work.

| Test | Description |
|------|-------------|
| `GameController_ShowsStartScreen_OnLoad` | Create GameController → start text is active, score/health text hidden |
| `GameController_StartsGame_OnSpace` | Simulate space key or call `game.StartGame()` → `gameActive == true`, start text hidden, player GameObject exists |
| `GameController_SpawnsEnemyGameObject` | Start game → `game.SpawnEnemy()` → wait frames → enemy GameObject exists at correct position |
| `GameController_SpawnsProjectileGameObject` | Start game → `player.Shoot()` → wait frames → projectile GameObject exists |
| `GameController_RemovesOffScreenProjectile` | Spawn projectile → set `yPos` beyond screen bounds → wait frames → projectile removed from list, GameObject destroyed |
| `GameController_CleansUpEnemyGameObject_OnDeath` | Spawn enemy → kill enemy (reduce health to 0, remove from list) → wait frames → enemy GameObject destroyed |
| `GameController_IncrementsScore_OnEnemyKill` | Spawn enemy → kill enemy → `player.score == 1` |
| `GameController_EndsGame_OnPlayerDeath` | Start game → reduce player health to 0 → wait frames → `gameActive == false`, start text shown |
| `PlayerController_UpdatesPosition_OnNodeChange` | Modify `player.xPos` → tick → wait frames → player transform.position.x matches |
| `EnemyController_UpdatesPosition_OnNodeChange` | Modify `enemy.xPos` → tick → wait frames → enemy transform.position.x matches |
| `ProjectileController_UpdatesPosition_OnNodeChange` | Modify `projectile.yPos` → tick → wait frames → projectile transform.position.y matches |
| `Projectile_Collision_WithEnemy_DealsDamage` | Spawn upward projectile at enemy position → wait for physics trigger → enemy health reduced |
| `Projectile_Collision_WithPlayer_DealsDamage` | Spawn downward projectile at player position → wait for physics trigger → player health reduced |
| `FormationMovement_ReversesAtEdge` | Spawn enemies near right bound → update several frames → enemies should reverse and step down |

#### 7.5 Running tests

Tests can be run from:
- **Unity Editor:** Window → General → Test Runner → PlayMode tab → Run All
- **Command line:**
  ```bash
  Unity -runTests -testPlatform PlayMode -projectPath samples/SpaceInvaders -batchmode -nographics -logFile -
  ```

---

### 8. File listing

```
Assets/
├── Scenes/
│   └── SpaceInvaders.unity
├── Scripts/
│   ├── Nodes/
│   │   ├── GameConstants.cs
│   │   ├── Health.cs
│   │   ├── LaserProjectile.cs
│   │   ├── Ship.cs
│   │   ├── Player.cs
│   │   ├── Enemy.cs
│   │   └── Game.cs
│   └── Controllers/
│       ├── GameController.cs
│       ├── GameUIController.cs
│       ├── PlayerController.cs
│       ├── EnemyController.cs
│       └── ProjectileController.cs
├── Tests/
│   ├── SpaceInvaders.Tests.asmdef
│   ├── TestHelpers.cs
│   ├── NodeTests.cs
│   └── ControllerTests.cs
```

---

### 9. Key Retree patterns demonstrated

| Pattern | Where |
|---------|-------|
| Field change detection via `Retree.Tick()` | `RetreeUpdater` base class in `GameController` ticks every frame |
| `RegisterOnNodeChanged` | Controllers updating transforms when node fields change |
| `RegisterOnTreeChanged` | `GameController` single listener for all game state (gameActive, health, list mutations, score) |
| `RetreeList<T>` synchronous events | Enemy/projectile add/remove fires immediately |
| `Retree.Parent()` navigation | `Ship.Shoot()` walks up to `Game`, `LaserProjectile.OnCollision()` removes self |
| `[RetreeIgnore]` | `damage`, `startHealth`, `xPos`/`yDirection` on projectile, `formationDirection` on Game |
| `Health` as child `RetreeNode` | Nested node whose changes propagate up via tree listeners |
