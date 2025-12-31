# Collision System [MVP Core]

**Purpose**: 2D collision detection and resolution on 3D world. Handles AABB collision, slope segments, platform riding, and seam handling.

## Player-Facing Rules

- **2D Collision**: Collision happens in 2D plane (X/Y). 3D assets are visual only.
- **Slope Collision**: Slopes use segment-based collision. Smooth transitions between segments.
- **Platform Riding**: Moving platforms carry players. Players snap to platform when within threshold.

## System Rules

- **Collision Detection**: `PhysicsSystem` uses AABB for ground/platforms, segment-based for slopes.
- **Broad Phase**: AABB overlap test to find potential collisions.
- **Narrow Phase**: Detailed collision resolution (AABB vs AABB, AABB vs slope segment).
- **Seam Handling**: `SlopeSolver` ensures continuous surface between slope segments.
- **Platform Riding**: `PlatformRider` component tracks platform entity. Player position updated with platform movement.

## Data Model

**Collider** (`GL2Project/ECS/Components.cs`):
- `Size`: Vector2 (half-extents for AABB)
- `Type`: ColliderType (AABB, Capsule)

**GroundState** (`GL2Project/ECS/Components.cs`):
- `IsGrounded`: bool
- `GroundNormal`: Vector2
- `SlopeAngle`: float (degrees)
- `GroundType`: GroundType

**PlatformRider** (`GL2Project/ECS/Components.cs`):
- `PlatformEntity`: Entity
- `Offset`: Vector2 (relative to platform)

**MovingPlatform** (`GL2Project/ECS/Components.cs`):
- `Path`: Vector2[] (waypoints)
- `Speed`: float
- `CurrentWaypoint`: int

## Algorithms / Order of Operations

### Collision Resolution

1. **Broad Phase**: Collect potential collisions (AABB overlap test)
2. **Narrow Phase**: For each potential collision:
   - If AABB: Resolve AABB collision (separate along normal)
   - If Slope: Resolve slope collision (move to surface, project velocity)
3. **Update Ground State**: Set `IsGrounded`, `GroundNormal`, `SlopeAngle`
4. **Platform Riding**: If `PlatformRider` exists, update position with platform movement

### Slope Collision

1. **Find Segment**: Check player AABB against slope segment (line segment collision)
2. **Calculate Intersection**: Find intersection point between player bottom and slope line
3. **Resolve Position**: Move player to slope surface (along normal)
4. **Project Velocity**: Remove normal component from velocity (keep tangent component)

### Seam Handling

1. **Detect Transition**: Check if player moving from one slope segment to another
2. **Find Continuity**: Ensure slope endpoints align (validation during level load)
3. **Smooth Transition**: Interpolate between segments if needed (future enhancement)

## Tuning Parameters

See [Physics2D & Slopes](Physics2D-and-Slopes) for detailed tuning.

## Edge Cases + Counters

- **Multiple collisions**: Resolve against closest/highest collision.
- **Slope seam gap**: Validate during level load, reject invalid levels.
- **Platform despawn**: Remove `PlatformRider` component, player falls.

## Telemetry Hooks

- Log collision events: `CollisionResolved(entity1, entity2, collisionType, normal, timestamp)` (optional)
- Log slope collisions: `SlopeCollision(slopeId, angle, slideTriggered, timestamp)`

## Implementation Notes

**File**: `GL2Project/Physics2D/PhysicsSystem.cs`, `GL2Project/Physics2D/SlopeSolver.cs`

**Key Systems**:
- `PhysicsSystem`: Main collision detection and resolution
- `SlopeSolver`: Slope-specific collision handling
- `MovingPlatform`: Platform movement and rider updates

**Deterministic Ordering**:
1. `PhysicsSystem.Update()` - Apply gravity, integrate velocity
2. Collision detection (broad phase → narrow phase)
3. Collision resolution (AABB, slopes)
4. Platform riding updates
5. Ground state updates

**Component Stores**: `GameWorld.Colliders`, `GameWorld.GroundStates`, `GameWorld.MovingPlatforms`, `GameWorld.PlatformRiders`

**See Also**: [Physics2D & Slopes](Physics2D-and-Slopes)

