# Physics2D & Slopes [MVP Core]

**Purpose**: 2D collision system on 3D world. Handles AABB collision, slope segments, seam handling, and sliding mechanics.

## Player-Facing Rules

- **2D Collision**: Collision detection and resolution happen in 2D plane (X/Y). 3D assets are visual only.
- **Slopes**: Slopes can be any angle. Slopes > 30° trigger sliding. Slopes > 70° auto-slide.
- **Seam Handling**: Smooth transitions between slope segments. No gaps or overlaps.
- **Slide Mechanics**: When sliding, player accelerates along slope tangent at 400 px/s².

## System Rules

- **Collision Detection**: `PhysicsSystem` uses AABB (Axis-Aligned Bounding Box) for ground/platforms, segment-based for slopes.
- **Slope Segments**: Slopes defined by start/end points in block-local coordinates. Angle precomputed during level load.
- **Seam Handling**: `SlopeSolver` handles transitions between slope segments. Ensures continuous surface.
- **Slide Detection**: Check slope angle against thresholds (30° slide, 70° auto-slide). Apply slide acceleration along tangent.
- **Gravity Application**: Applied before collision resolution. `velocity.Y += gravity * dt`, clamped to terminal velocity.

## Data Model

**Collider** (`GL2Project/ECS/Components.cs`):
- `Size`: Vector2 (half-extents for AABB)
- `Type`: ColliderType (AABB, Capsule)

**SlopeData** (`GL2Project/Content/LevelData.cs`):
- `StartPoint`: Vector2 (block-local)
- `EndPoint`: Vector2 (block-local)
- `AngleDegrees`: float (precomputed)
- `SlideEligible`: bool (angle > 30°)

**BlockDefinition** (`GL2Project/Content/LevelData.cs`):
- `Id`: string
- `Size`: Vector2
- `CollisionType`: CollisionType (None, AABB, Slope)
- `Slope`: SlopeData? (null if not slope)

**GroundState** (`GL2Project/ECS/Components.cs`):
- `IsGrounded`: bool
- `GroundNormal`: Vector2
- `SlopeAngle`: float (degrees)
- `GroundType`: GroundType

## Algorithms / Order of Operations

### Collision Resolution

1. **Apply Gravity**: `velocity.Y += gravity * dt`, clamp to terminal velocity
2. **Integrate Velocity**: `position += velocity * dt`
3. **Broad Phase**: Collect potential collisions (AABB overlap test)
4. **Narrow Phase**: For each potential collision:
   - If AABB: Resolve AABB collision
   - If Slope: Resolve slope collision (segment-based)
5. **Update Ground State**: Set `IsGrounded`, `GroundNormal`, `SlopeAngle`
6. **Slide Check**: If `SlopeAngle > slideThreshold`:
   - Calculate tangent vector
   - Apply slide acceleration: `velocity += tangent * slideAcceleration * dt`
   - If `SlopeAngle > autoSlideThreshold`: Auto-slide even without down input

### Slope Collision

1. **Find Slope Segment**: Check player AABB against slope segment (line segment collision)
2. **Calculate Intersection**: Find intersection point between player bottom and slope line
3. **Check Angle**: Calculate angle from slope normal
4. **Resolve Position**: Move player to slope surface (along normal)
5. **Update Velocity**: Project velocity onto slope surface (remove normal component)
6. **Seam Handling**: If transitioning between segments, ensure continuous surface (no gaps)

### Seam Handling

1. **Detect Transition**: Check if player moving from one slope segment to another
2. **Find Continuity**: Ensure slope endpoints align (validation during level load)
3. **Smooth Transition**: Interpolate between segments if needed (future enhancement)
4. **Prevent Gaps**: Validate slope connections, reject invalid levels

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `slideThreshold` | float | 0-90 | 30° | Slopes steeper than this slide |
| `autoSlideThreshold` | float | 0-90 | 70° | Slopes steeper than this auto-slide |
| `slideAcceleration` | float | 0-1000 | 400 px/s² | Acceleration along slope tangent |
| `gravity` | float | 0-3000 | 1200 px/s² | Fall acceleration |
| `terminalVelocity` | float | 0-1000 | 600 px/s | Max fall speed |

## Edge Cases + Counters

- **Slope at exact threshold**: Use `>=` comparison to include threshold angle.
- **Multiple slopes**: Resolve against closest/highest slope.
- **Slope seam gap**: Validate during level load, reject invalid levels.
- **Slope into wall**: Player stops, no wall-slide.
- **Slope angle = 0**: Treat as flat ground, no sliding.

## Telemetry Hooks

- Log slope collision: `SlopeCollision(slopeId, angle, slideTriggered, timestamp)`
- Log slide events: `SlideStart(slopeId, angle, timestamp)`, `SlideEnd(slopeId, duration, timestamp)`
- Log seam transitions: `SeamTransition(fromSlopeId, toSlopeId, smoothTransition, timestamp)`

## Implementation Notes

**File**: `GL2Project/Physics2D/PhysicsSystem.cs`, `GL2Project/Physics2D/SlopeSolver.cs`

**Key Systems**:
- `PhysicsSystem`: Main physics update, collision detection, gravity application
- `SlopeSolver`: Slope collision resolution, seam handling
- `LevelLoader`: Precomputes slope angles during level load

**Deterministic Ordering**:
1. `PhysicsSystem.Update()` - Apply gravity, integrate velocity
2. Collision detection (broad phase → narrow phase)
3. Slope resolution (if on slope)
4. Slide check (if angle > threshold)
5. Update ground state

**Component Stores**: `GameWorld.Colliders`, `GameWorld.GroundStates`, `GameWorld.Positions`, `GameWorld.Velocities`

**Tuning File**: `GL2Project/Tuning/MovementTuning.json` (slopes section)

**Level Format**: Slopes defined in `BlockDefinition.Slope` with `StartPoint` and `EndPoint` in block-local coordinates (0-64px).

