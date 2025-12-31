# Celeste Findings - Camera System

## What We Adopt

- **CameraTarget calculation**: Base position offset by camera viewport center (`player.X - 160, player.Y - 90` for 320x180 viewport)
- **Look-ahead based on velocity/facing**: `targetX += facing * lookAheadDistance` where lookAheadDistance scales with velocity (0.2 * speed.X for normal movement, 48px for dash state)
- **Vertical damping when airborne**: `cameraTarget.Y = (Camera.Y * 2f + cameraTarget.Y) / 3f` - prevents camera from following player upward too quickly
- **Vertical damping for small rooms**: When room height is small, camera should NOT follow player on Y axis to maintain ground visibility for precision platforming
- **Exponential damping**: `Position = position + (cameraTarget - position) * (1f - pow(0.01, dt))` - smooth camera follow with exponential ease
- **Room bounds clamping**: Camera position clamped to level bounds (AABB volumes)
- **CameraOffset**: Level-specific camera adjustments (offset from player position)
- **State-based look-ahead**: Different look-ahead distances based on player state (dash = 48px, normal = 0.2 * speed, climbing = different offset)

## What We Reject

- **Camera shake system**: Celeste's shake effects. We'll add this later if needed.
- **Camera lock modes**: Celeste's complex camera locking for boss fights. Too specific for M2-M3.
- **Camera anchor lerp**: Celeste's camera anchor system for specific level sections. We'll use camera volumes instead.
- **Killbox camera constraints**: Celeste's killbox-based camera limits. We use camera volumes.

## Minimal GL2 Implementation Plan

1. **CameraController** (`GL2Project/Render/CameraController.cs`):
   - `UpdateCamera(Camera camera, Vector2 playerPos, Vector2 playerVelocity, int facing, bool isGrounded, float dt)`
   - Calculates CameraTarget with look-ahead and state-based offsets
   - Applies vertical damping when airborne or in small rooms
   - Applies exponential damping for smooth follow

2. **CameraVolume component** (`GL2Project/ECS/Components.cs`):
   - `Bounds` (Rectangle AABB), `CameraOffset` (Vector2), `AllowVerticalFollow` (bool)
   - Entities with CameraVolume define camera constraints per room/area

3. **Camera system integration**:
   - New `CameraSystem` that runs after MovementSystem, before RenderSystem
   - Detects current camera volume based on player position
   - Applies volume bounds and offset to camera target
   - Smooth transitions between volumes

4. **Debug visualization**:
   - Draw camera volumes as wireframe rectangles
   - Draw camera target position and current position
   - Show look-ahead vector

## 3 Edge Cases to Test

1. **Small room vertical damping**: Player in room with height < 180px - camera should not follow Y movement, stays locked to show ground
2. **Volume transitions**: Player moves between camera volumes - camera should smoothly transition without snapping
3. **Look-ahead reversal**: Player changes facing direction while moving - look-ahead should smoothly reverse without camera jitter

