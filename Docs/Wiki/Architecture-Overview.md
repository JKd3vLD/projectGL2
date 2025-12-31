# Architecture Overview [MVP Core]

**Purpose**: High-level overview of GL2 Engine architecture: GameWorld singleton, ECS SoA, typed ring-buffer events, deterministic phase order.

## Core Architecture

- **GameWorld Singleton**: Owns all ECS component stores, event bus, RNG streams, content database. Lives in `GL2Project/ECS/World.cs`.
- **ECS SoA**: Structure of Arrays - each component type stored in separate array for cache locality.
- **Typed Event Bus**: Ring-buffer based events, no per-frame allocations. One ring buffer per event type.
- **Deterministic Ordering**: Fixed update order ensures same inputs produce same results.

## System Rules

- **Fixed 120 Hz Simulation**: Update loop runs at exactly 120 Hz. Variable render step decoupled.
- **No Per-Frame Allocations**: Fixed update loop targets zero allocations (ring buffers, SoA storage, pre-allocated arrays).
- **Component Stores**: SoA arrays with `Add`, `Remove`, `Get`, `Has`, `GetActiveEntities` methods.
- **Event Processing**: Events processed after all systems update. Ring buffers consumed, then cleared.

## Data Model

**GameWorld** (`GL2Project/ECS/World.cs`):
- Component stores: `Positions`, `Velocities`, `Colliders`, `PlayerControllers`, etc.
- `Events`: EventBus
- `Rng`: Rng (legacy)
- `RngStreams`: RngStreams (tier-scoped)
- `PlayerEntity`: Entity
- `PartnerEntity`: Entity

**ComponentStore<T>** (`GL2Project/ECS/ComponentStore.cs`):
- `_components`: T[] (SoA array)
- `_entityToIndex`: Dictionary<Entity, int>
- `_indexToEntity`: Dictionary<int, Entity>
- `Add(entity, component)`, `Remove(entity)`, `Get(entity)`: ref T, `Has(entity)`: bool

**EventBus** (`GL2Project/ECS/EventBus.cs`):
- `_ringBuffers`: Dictionary<Type, RingBuffer> (one per event type)
- `Publish<T>(event)`: void
- `Process()`: void (consumes all ring buffers)

**Entity** (`GL2Project/ECS/Entity.cs`):
- `Id`: int
- `Generation`: int
- `IsValid`: bool

## Algorithms / Order of Operations

### Fixed Update (120 Hz)

1. **AnimalBuddySystem**: Mount/transform input, buddy-specific controls
2. **PlayerControllerSystem**: Player input, movement state, coyote time, jump buffer
3. **TeamUpSystem**: Partner pickup/throw mechanics
4. **WaterSystem**: Water physics, swimming state
5. **EnemySystem**: Enemy AI, collision
6. **AnimationSystem**: Animation playback
7. **PhysicsSystem**: Collision detection, gravity, slope resolution
8. **CameraSystem**: Camera update, volume detection
9. **ToolSystem**: Movement tools (grapple, balloon)
10. **InventorySystem**: Item pickups, inventory management
11. **GameOverSystem**: Game over checks
12. **CurrencySystem**: XP collection, death drops, threshold checks
13. **FlagSystem**: Flag passing, respawn, fast travel
14. **EventBus.Process()**: Consume event ring buffers

### Render (Variable Step)

1. **RenderPipeline.BeginRender()**: Set low-res render target
2. **RenderSystem.Draw()**: Render all layers (Background/Mid/Foreground)
3. **RenderPipeline.Present()**: Upscale to backbuffer
4. **DebugOverlay.Draw()**: Draw debug information

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `fixedUpdateHz` | int | 30-144 | 120 | Fixed simulation frequency |
| `maxEntities` | int | 1024-16384 | 4096 | Maximum entities |
| `ringBufferSize` | int | 64-1024 | 256 | Event ring buffer size |

## Edge Cases + Counters

- **Entity ID exhaustion**: Wrap around, check generation. Max entities: 4096.
- **Ring buffer full**: Overwrite oldest events (FIFO). Size: 256 per event type.
- **Component store full**: Throw exception. Pre-allocate based on expected entity count.

## Telemetry Hooks

- Log system update times: `SystemUpdate(systemName, updateTime, timestamp)` (optional, for profiling)
- Log entity count: `EntityCount(total, active, timestamp)` (optional)
- Log allocation count: `AllocationCount(count, timestamp)` (optional, for memory profiling)

## Implementation Notes

**File**: `GL2Project/ECS/World.cs`, `GL2Project/ECS/ComponentStore.cs`, `GL2Project/ECS/EventBus.cs`

**Key Systems**:
- `GameWorld`: Singleton owning all systems and component stores
- `ComponentStore<T>`: SoA storage for component type
- `EventBus`: Typed ring-buffer event system

**Deterministic Ordering**: Fixed update order documented in `GL2Project/Docs/SystemUpdateOrder.md`.

**Component Stores**: All component types have dedicated `ComponentStore<T>` in `GameWorld`. Access via `GameWorld.ComponentName`.

**Event Types**: Events defined as structs. One ring buffer per event type. No inheritance or interfaces.

**Memory Layout**: SoA improves cache locality. Iterating over positions accesses contiguous memory, not interleaved with other components.

