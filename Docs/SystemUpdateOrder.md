# System Update Order

The game uses a fixed 120Hz simulation step (or 30Hz in SNES mode) with deterministic update order.

## Update Order (Fixed Step)

1. **AnimalBuddySystem** - Processes mount/transform input and buddy-specific controls
   - Handles mount/dismount logic
   - Processes transformation state
   - Applies buddy-specific movement and abilities

2. **PlayerControllerSystem** - Processes player input and movement state (skipped if mounted/transformed)
   - Updates coyote time and jump buffer
   - Handles ground/air movement
   - Processes cartwheel and glide states
   - Checks for mount/transform state before processing

3. **TeamUpSystem** - Handles partner pickup/throw mechanics
   - Checks pickup distance
   - Updates partner position when carrying
   - Handles throw velocities
   - Implements follow snap on landing

4. **WaterSystem** - Updates water physics and swimming state
   - Checks entities for water collision
   - Applies buoyancy and drag
   - Updates swimming components

5. **EnemySystem** - Updates enemy AI and collision
   - Processes enemy patrol AI
   - Handles player-enemy collision
   - Updates invulnerability timers

6. **AnimationSystem** - Updates animation playback
   - Updates animation time
   - Handles looping and clip duration

7. **PhysicsSystem** - Collision detection and resolution
   - Updates moving platforms
   - Integrates velocities to positions
   - Resolves collisions with ground, slopes, and platforms
   - Updates ground state
   - Fires landing events

8. **EventBus.Process()** - Consumes event ring buffers
   - Clears event queues after processing

## Render Order (Variable Step)

1. **RenderPipeline.BeginRender()** - Set low-res render target
2. **RenderSystem.Draw()** - Draw all renderable entities
3. **RenderPipeline.Present()** - Upscale to backbuffer with nearest-neighbor
4. **DebugOverlay.Draw()** - Draw debug information on top

## Determinism

- All systems use fixed timestep (1/120 seconds or 1/30 seconds in SNES mode)
- No per-frame allocations in UpdateFixed
- Deterministic RNG streams available but not used in physics
- Stable ordering ensures same inputs produce same results
