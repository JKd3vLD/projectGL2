# Flag System [MVP Core]

**Purpose**: Checkpoint system with start/middle/end flags, fast travel, respawn, and consumable flags (Shovel Knight style) for bonus rewards.

## Player-Facing Rules

- **Start Flag**: Spawn point at stage beginning. Always present.
- **Middle Flags**: Checkpoints at end of LONG sections (if stage has 3+ sections). Consumable for rewards.
- **End Flag**: Completion marker at stage end. Always present.
- **Fast Travel**: Travel between any passed flags from stage map or pause menu.
- **Respawn**: On death, respawn at last passed flag (start if none passed).
- **Consumable Flags**: Middle flags can be consumed for 1.5x reward multiplier. Consumed flags disappear but provide bonus.

## System Rules

- **Flag Placement**: `FlagPlacer.PlaceFlags(plan)` procedurally places flags:
  - Start: Beginning of first section
  - Middle: End of LONG sections (if stage has 3+ sections)
  - End: End of last section
- **Flag Passing**: `FlagSystem.Update()` checks player proximity (50px radius). Updates `_lastPassedFlagId`.
- **Respawn Logic**: `FlagSystem.RespawnAtLastFlag()` moves player to last passed flag, resets velocity.
- **Consumable Logic**: `FlagSystem.ConsumeFlag(flagId)` marks flag as consumed, triggers reward calculation.
- **Fast Travel**: `FlagSystem.FastTravelToFlag(flagId)` teleports player to flag position.

## Data Model

**FlagComponent** (`GL2Project/ECS/Components.cs`):
- `FlagId`: int (unique per stage)
- `FlagType`: int (0=Start, 1=Middle, 2=End)
- `IsConsumable`: bool
- `IsConsumed`: bool

**FlagPosition** (`GL2Project/World/StagePlan.cs`):
- `Type`: FlagType (Start/Middle/End)
- `Position`: Vector2
- `SectionIndex`: int
- `IsConsumable`: bool

**FlagSystem** (`GL2Project/Gameplay/FlagSystem.cs`):
- `_world`: GameWorld
- `_flagEntities`: Dictionary<int, Entity> (flagId → entity)
- `_lastPassedFlagId`: int

**FlagPlacer** (`GL2Project/World/FlagPlacer.cs`):
- Static class, no state

## Algorithms / Order of Operations

### Flag Placement

1. **Start Flag**: Always placed at beginning of first section (position 0,0 - updated from section data)
2. **Middle Flags**: For stages with 3+ sections:
   - Iterate sections (excluding last)
   - If section `LengthClass == LONG`: Place middle flag at section end
   - Mark as consumable: `IsConsumable = true`
3. **End Flag**: Always placed at end of last section
4. **Update Positions**: Positions updated from loaded section level data during `LevelLoader.LoadStagePlan()`

### Flag Passing

1. **Proximity Check**: `FlagSystem.Update()` checks player distance to all flags (50px radius)
2. **Flag Activation**: If player within radius AND `flagId > _lastPassedFlagId`:
   - Update `_lastPassedFlagId = flagId`
   - Play activation effect (TODO)
   - Log flag passed event

### Respawn

1. **Death Event**: `FlagSystem.RespawnAtLastFlag()` called
2. **Get Safe Position**: `GetLastSafeFlagPosition()` - Returns position of `_lastPassedFlagId` flag
3. **Move Player**: Set `PlayerEntity.Position` to safe position
4. **Reset Velocity**: Set `PlayerEntity.Velocity` to zero
5. **Reset State**: Reset player controller state (coyote time, jump buffer, etc.)

### Consumable Flag

1. **Player Interaction**: Player activates consumable flag (middle flag, not consumed)
2. **Check Eligibility**: `FlagSystem.ConsumeFlag(flagId)` - Verify `IsConsumable && !IsConsumed`
3. **Mark Consumed**: Set `IsConsumed = true`
4. **Calculate Reward**: Apply 1.5x multiplier to stage completion reward
5. **Update Visual**: Flag disappears or changes appearance (TODO)
6. **Log Event**: Log flag consumption for telemetry

### Fast Travel

1. **Player Opens Map**: Show stage map with all passed flags
2. **Player Selects Flag**: Choose flag from list
3. **Teleport**: `FlagSystem.FastTravelToFlag(flagId)` - Set player position to flag position
4. **Reset State**: Reset velocity, update camera target

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `flagActivationRadius` | float | 20-200 | 50 px | Proximity for flag passing |
| `flagConsumeRewardMultiplier` | float | 1.0-3.0 | 1.5 | Reward multiplier for consumed flags |
| `middleFlagMinStageLength` | int | 2-10 | 3 | Minimum sections for middle flags |

## Edge Cases + Counters

- **No flags passed**: Respawn at start flag (flagId = 0).
- **Multiple flags at same position**: Use highest flagId (most recent).
- **Consumable flag already consumed**: Cannot consume again. Show message or hide option.
- **Fast travel to unconsumed consumable flag**: Allowed. Player can return to consume later.
- **Flag position not set**: Use section end position from level data, or default to (0,0).

## Telemetry Hooks

- Log flag placement: `FlagPlaced(flagId, flagType, sectionIndex, isConsumable, position)`
- Log flag passed: `FlagPassed(flagId, flagType, timestamp)`
- Log respawn: `RespawnAtFlag(flagId, flagType, timestamp)`
- Log flag consumption: `FlagConsumed(flagId, rewardMultiplier, timestamp)`
- Log fast travel: `FastTravel(flagId, fromPosition, toPosition, timestamp)`

## Implementation Notes

**File**: `GL2Project/Gameplay/FlagSystem.cs`, `GL2Project/World/FlagPlacer.cs`

**Key Systems**:
- `FlagSystem`: Flag management, passing detection, respawn, consumption, fast travel
- `FlagPlacer`: Procedural flag placement in stage plans
- `CurrencySystem`: Uses flag positions for death drop placement

**Deterministic Ordering**:
1. `FlagPlacer.PlaceFlags()` - During stage plan assembly
2. `FlagSystem.CreateFlagsFromPlan()` - During stage load
3. `FlagSystem.Update()` - Check passing (after movement, before physics)
4. `FlagSystem.RespawnAtLastFlag()` - On death event

**Component Stores**: `GameWorld.Flags`, `GameWorld.Positions`, `GameWorld.Renderables`

**Flag Types**: Stored as int in `FlagComponent` (0=Start, 1=Middle, 2=End) to avoid enum conflicts. `FlagType` enum in `GL2Project/World/StagePlan.cs` used for stage plans.

**Visual Representation**: Flags use `Renderable` component with `Layer = MidLayer`, `Z = 0.3f`. Color determined by flag type (green=start, yellow=middle, red=end) in rendering system.

