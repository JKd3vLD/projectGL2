# Movement Spec [MVP Core]

**Purpose**: Authoritative specification for player movement mechanics. Based on DKC2 disassembly with tuning from `GL2Project/Tuning/MovementTuning.json`.

## Player-Facing Rules

- **Walk/Run**: Hold direction to walk, hold longer to run. Walk speed: 80 px/s, Run speed: 120 px/s.
- **Jump**: Variable-height jump. Hold longer = higher jump. Release early cuts velocity in half.
- **Coyote Time**: 0.1s window after leaving ground where jump still works.
- **Jump Buffer**: 0.15s window before landing where jump input is buffered.
- **Glide**: Retriggerable glide reduces fall speed to 100 px/s max.
- **Cartwheel**: Forward roll on ground (180 px/s). Can jump during cartwheel, which ends it.
- **Crouch**: Walk speed reduced to 40 px/s while crouched.
- **Slopes**: Slopes > 30° trigger sliding. Slopes > 70° auto-slide. Slide acceleration: 400 px/s².
- **Team-Up Throw**: Neutral throw (150, -200 px/s), Up throw (0, -350 px/s). Thrower snaps to landing point if within 64px.

## System Rules

- **Fixed movement kit**: Core movement constants never permanently altered. Tools/props can extend movement temporarily with counters/cooldowns.
- **120 Hz simulation**: Movement updates at fixed 120 Hz. Tuning values converted from SNES 60 Hz.
- **Ground acceleration**: 600 px/s² to reach walk speed, 800 px/s² deceleration when not pressing direction.
- **Friction**: 0.85 multiplier per frame (preserves momentum).
- **Gravity**: 1200 px/s² acceleration downward.
- **Terminal velocity**: 600 px/s maximum fall speed.
- **Slope collision**: 2D collision on 3D world. Slopes use segment-based collision with seam handling.

## Data Model

**MovementTuning** (`GL2Project/Tuning/MovementTuning.json`):
- `ground.walkSpeed`: float (px/s)
- `ground.runSpeed`: float (px/s)
- `ground.acceleration`: float (px/s²)
- `ground.deceleration`: float (px/s²)
- `ground.friction`: float (multiplier)
- `ground.crouchSpeed`: float (px/s)
- `jump.initialVelocity`: float (px/s, negative = up)
- `jump.variableJumpCutVelocity`: float (px/s)
- `jump.coyoteTime`: float (seconds)
- `jump.jumpBuffer`: float (seconds)
- `gravity.value`: float (px/s²)
- `terminalVelocity.value`: float (px/s)
- `glide.clampVelocity`: float (px/s)
- `glide.retriggerable`: bool
- `cartwheel.groundSpeed`: float (px/s)
- `cartwheel.airJumpAllowed`: bool
- `cartwheel.airJumpEndsCartwheel`: bool
- `slopes.slideThreshold`: float (degrees)
- `slopes.autoSlideThreshold`: float (degrees)
- `slopes.slideAcceleration`: float (px/s²)
- `teamUp.throwVelocity.neutral`: Vector2 (px/s)
- `teamUp.throwVelocity.up`: Vector2 (px/s)
- `teamUp.followSnapDistance`: float (px)

**Components** (`GL2Project/ECS/Components.cs`):
- `PlayerController`: Movement state, coyote time, jump buffer, glide/cartwheel flags
- `Velocity`: Current velocity vector
- `Position`: Current position vector
- `GroundState`: Ground contact info, slope angle, ground type

## Algorithms / Order of Operations

1. **Input Processing** (`PlayerControllerSystem.Update`):
   - Read input state (left/right, jump, crouch, glide, cartwheel)
   - Update coyote time timer (decrement if airborne)
   - Update jump buffer timer (decrement if on ground)
   - Check for mount/transform state (skip if mounted)

2. **Ground Movement**:
   - If pressing direction: accelerate toward walk/run speed
   - If not pressing: decelerate
   - Apply friction multiplier
   - Clamp to walk/run speed based on input duration

3. **Jump**:
   - Check coyote time or jump buffer
   - If valid: set Y velocity to `initialVelocity`
   - If holding jump: maintain velocity
   - If released early: clamp Y velocity to `variableJumpCutVelocity`

4. **Air Movement**:
   - Apply gravity: `velocity.Y += gravity * dt`
   - Clamp to terminal velocity
   - If gliding: clamp fall speed to `glide.clampVelocity`

5. **Cartwheel**:
   - If on ground and input: set X velocity to `cartwheel.groundSpeed`
   - If jump during cartwheel: apply jump, end cartwheel

6. **Slope Handling** (`PhysicsSystem`):
   - Detect slope collision
   - If angle > `slideThreshold`: apply slide acceleration along tangent
   - If angle > `autoSlideThreshold`: auto-slide even without down input

7. **Team-Up Throw** (`TeamUpSystem`):
   - Calculate throw velocity based on input (neutral/up)
   - Apply velocity to partner
   - After landing: check distance, snap thrower if within `followSnapDistance`

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `walkSpeed` | float | 0-200 | 80 px/s | Max walk speed |
| `runSpeed` | float | 0-300 | 120 px/s | Max run speed |
| `acceleration` | float | 0-2000 | 600 px/s² | Ground acceleration |
| `deceleration` | float | 0-2000 | 800 px/s² | Ground deceleration |
| `friction` | float | 0-1 | 0.85 | Per-frame multiplier |
| `initialVelocity` | float | -500 to 0 | -320 px/s | Jump initial speed (negative = up) |
| `coyoteTime` | float | 0-0.5 | 0.1s | Window after leaving ground |
| `jumpBuffer` | float | 0-0.5 | 0.15s | Window before landing |
| `gravity` | float | 0-3000 | 1200 px/s² | Fall acceleration |
| `terminalVelocity` | float | 0-1000 | 600 px/s | Max fall speed |
| `slideThreshold` | float | 0-90 | 30° | Slopes steeper than this slide |
| `autoSlideThreshold` | float | 0-90 | 70° | Slopes steeper than this auto-slide |

## Edge Cases + Counters

- **Coyote time + jump buffer overlap**: Both can trigger jump. Jump buffer takes priority if on ground.
- **Slope at exact threshold**: Use `>=` comparison to include threshold angle.
- **Cartwheel into wall**: Cartwheel ends, player stops. No wall-slide.
- **Glide retrigger**: Can activate glide multiple times in air. Each activation resets fall speed clamp. **Important**: Glide retrigger is a **control affordance** (precision timing + landing adjustment), not a reward mechanic by itself. Only affects rewards via Technique Mods (e.g., "Soft Landing" mod). Never say "+score" - say "+Flow" or "+Reward Meter progress" (and only when it makes sense via Technique Mods).
- **Team-up throw into ceiling**: Partner bounces off ceiling, thrower still snaps if within distance.

## Telemetry Hooks

- Log jump activation (coyote time vs jump buffer vs normal)
- Log cartwheel usage count per stage
- Log glide activation count per stage
- Log slope slide events (angle, duration)
- Log team-up throw usage (neutral vs up)

## Implementation Notes

**File**: `GL2Project/Gameplay/PlayerController.cs`

**Key Systems**:
- `PlayerControllerSystem`: Main movement controller, runs first in update order
- `MovementSystem`: Legacy system, still loads tuning
- `PhysicsSystem`: Applies gravity, resolves collisions, handles slopes

**Deterministic Ordering**:
1. `PlayerControllerSystem.Update()` - Process input, update movement state
2. `TeamUpSystem.Update()` - Handle partner throw/carry
3. `PhysicsSystem.Update()` - Apply gravity, resolve collisions

**Tuning Loading**: `PlayerControllerSystem` loads `MovementTuning.json` at initialization. Values cached in `MovementTuning` struct.

**SNES Conversion**: Original DKC2 values in 8.8 fixed-point format. `$0140` = 320 decimal = 1.25 fixed-point. At 60Hz: 320 subpixels/frame. Converted to 120Hz by multiplying frame-based values by 2.

## Glide Retrigger Clarification

**What glide retrigger is (baseline)**:
- Glide is a **fall-speed clamp** you can enter/exit freely in normal airborne state.
- Retriggerable means: releasing and re-holding glide re-enters the clamp any number of times while airborne.
- It's purely a **control affordance** (precision timing + landing adjustment), not a reward mechanic by itself.

**When glide should affect rewards (optional, via Technique Mods only)**:
- If you want glide to visually encourage "good control" without arcade "score", do it through **Flow events** that are:
  - **rare** (not every glide activation)
  - **contextual** (specific conditions like "Soft Landing")
  - **anti-spam** (cooldowns, per-section caps)
- Example Technique Mod: **"Soft Landing"**
  - Trigger: Release glide within 120ms before touching ground AND land without taking damage.
  - Payoff: +small Flow gain (or +small XP threshold progress).
  - Constraints: Once per section segment; cooldown 2s.
  - UI: Tiny callout "Soft Landing" + a small Flow tick.
- This makes glide feel "recognized" without turning the game into score-chasing.
- See [Technique Mods System](Technique-Mods-System) for details.

