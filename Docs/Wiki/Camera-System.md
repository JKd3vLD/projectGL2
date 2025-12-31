# Camera System [MVP Core]

**Purpose**: DKC2 + Celeste-inspired camera with look-ahead, vertical damping, room bounds/volumes, and smooth transitions.

## Player-Facing Rules

- **Look-Ahead**: Camera looks ahead in movement direction based on velocity and facing.
- **Vertical Damping**: When airborne, camera follows player upward more slowly to maintain ground visibility.
- **Small Room Lock**: In small rooms (height < 180px), camera locks Y position to show ground for precision platforming.
- **Room Bounds**: Camera constrained to level bounds (AABB volumes). Smooth transitions between volumes.

## System Rules

- **CameraTarget Calculation**: Base position = player position - viewport center. Add look-ahead based on velocity/facing.
- **Vertical Damping**: When airborne: `cameraTarget.Y = (Camera.Y * 2f + cameraTarget.Y) / 3f`. Prevents upward camera movement.
- **Exponential Damping**: `Position = position + (cameraTarget - position) * (1f - pow(0.01, dt))`. Smooth follow with exponential ease.
- **Room Bounds**: Camera position clamped to `CameraVolume.Bounds` (AABB). Smooth transitions between volumes.
- **State-Based Look-Ahead**: Different look-ahead distances based on player state (dash = 48px, normal = 0.2 * speed, climbing = different offset).

## Data Model

**CameraVolumeComponent** (`GL2Project/ECS/Components.cs`):
- `Bounds`: Rectangle (AABB)
- `CameraOffset`: Vector2 (level-specific camera adjustments)
- `AllowVerticalFollow`: bool (false for small rooms)

**Camera** (`GL2Project/Render/Camera.cs`):
- `Position`: Vector2
- `Width`: int (viewport width)
- `Height`: int (viewport height)

**CameraController** (`GL2Project/Render/CameraController.cs`):
- `_targetPosition`: Vector2
- `_currentPosition`: Vector2
- `UpdateCamera(camera, playerPos, playerVelocity, facing, isGrounded, dt)`

**CameraSystem** (`GL2Project/Render/CameraSystem.cs`):
- `_world`: GameWorld
- `_camera`: Camera
- `FindActiveVolume(playerPos)`: Entity? - Finds camera volume containing player

## Algorithms / Order of Operations

### Camera Update

1. **Get Player State**: `PlayerController`, `Position`, `Velocity`, `GroundState`
2. **Find Active Volume**: `CameraSystem.FindActiveVolume(playerPos)` - Query `CameraVolumeComponent` entities
3. **Calculate CameraTarget**:
   - Base: `playerPos - viewportCenter`
   - Look-ahead: `lookAheadX = facing * lookAheadDistance` (based on velocity/state)
   - Apply offset: `cameraTarget += volume.CameraOffset` (if volume found)
4. **Vertical Damping**:
   - If airborne: `cameraTarget.Y = (Camera.Y * 2f + cameraTarget.Y) / 3f`
   - If small room (`!volume.AllowVerticalFollow`): Lock Y to room center
5. **Exponential Damping**: `currentPosition += (cameraTarget - currentPosition) * (1f - pow(0.01, dt))`
6. **Clamp to Bounds**: If volume found, clamp `currentPosition` to `volume.Bounds`
7. **Update Camera**: `Camera.Position = currentPosition`

### Look-Ahead Calculation

1. **Get Player State**: Check `PlayerController.State` (dash, normal, climbing)
2. **Calculate Distance**:
   - Dash state: `lookAheadDistance = 48px`
   - Normal: `lookAheadDistance = 0.2 * abs(velocity.X)`
   - Climbing: `lookAheadDistance = climbingOffset` (different value)
3. **Apply Facing**: `lookAheadX = facing * lookAheadDistance` (facing = -1 or 1)

### Volume Transitions

1. **Detect Transition**: Check if player moved from one volume to another
2. **Smooth Transition**: Interpolate camera bounds/offset over transition time (future enhancement)
3. **Update Active Volume**: Set new active volume, apply new bounds/offset

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `lookAheadNormalMultiplier` | float | 0-1 | 0.2 | Look-ahead multiplier for normal movement |
| `lookAheadDashDistance` | float | 0-100 | 48 px | Look-ahead distance for dash state |
| `verticalDampingFactor` | float | 0-1 | 0.67 | Vertical damping when airborne (2/3) |
| `exponentialDampingRate` | float | 0-1 | 0.01 | Exponential damping rate (pow base) |
| `smallRoomHeightThreshold` | float | 0-500 | 180 px | Rooms below this lock Y position |

## Edge Cases + Counters

- **No active volume**: Use level bounds or default bounds (full level).
- **Volume transition**: Smooth interpolation (future enhancement). For now, snap to new volume bounds.
- **Look-ahead reversal**: Player changes facing while moving. Look-ahead smoothly reverses without jitter.
- **Small room detection**: Check `volume.Bounds.Height < smallRoomHeightThreshold` OR `!volume.AllowVerticalFollow`.

## Telemetry Hooks

- Log camera volume transitions: `CameraVolumeTransition(fromVolumeId, toVolumeId, timestamp)`
- Log look-ahead usage: `CameraLookAhead(distance, facing, playerState, timestamp)` (optional, for debugging)
- Log vertical damping: `CameraVerticalDamping(airborne, dampingApplied, timestamp)` (optional)

## Implementation Notes

**File**: `GL2Project/Render/CameraSystem.cs`, `GL2Project/Render/CameraController.cs`, `GL2Project/Render/Camera.cs`

**Key Systems**:
- `CameraSystem`: Finds active volume, updates camera position
- `CameraController`: Calculates camera target, applies damping, clamps bounds
- `Camera`: Stores position and viewport size

**Deterministic Ordering**:
1. `CameraSystem.Update()` - After movement, before render
2. Find active volume
3. Calculate camera target
4. Apply damping
5. Clamp bounds
6. Update camera position

**Component Stores**: `GameWorld.CameraVolumes`, `GameWorld.Positions`, `GameWorld.PlayerControllers`, `GameWorld.GroundStates`

**Debug Visualization**: `CameraTestbed` draws camera volumes, target position, current position, look-ahead vector.

**Reference**: Based on Celeste camera system (see `GL2Project/Docs/Research/Celeste_Findings.md`).

