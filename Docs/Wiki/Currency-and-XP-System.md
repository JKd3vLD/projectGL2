# Currency & XP System [MVP Core]

**Purpose**: Souls-like XP collection system with death drops, reward thresholds, and Hades-style reward selection. XP is the primary progression currency.

## Player-Facing Rules

- **XP Collection**: Collect XP from pickups (XP collectibles) scattered throughout stages.
- **Reward Thresholds**: Every 100 XP (or Run Value equivalent) triggers reward selection (3 options from loot table). Player chooses one. **Reward Quality** (player-facing term) shows what you earned, never "score".
- **Flow Meter Integration**: Flow Meter can add to Run Value (RV), affecting XP threshold progress. See [Flow Meter System](Flow-Meter-System) for details.
- **Death Penalty**: On death, lose half of current XP (rounded down). Dropped XP appears at last safe flag position.
- **Drop Retrieval**: Return to drop location to retrieve lost XP. Drop persists until collected or timeout.
- **Total Collected**: Lifetime XP total tracked separately from current XP (for statistics).

## System Rules

- **XP Collection**: `CurrencySystem.AddXP(playerEntity, amount)` adds to current and total.
- **Run Value (RV)**: Internal aggregate combining XP and Flow Meter contributions. Used for reward threshold progress, bonus payout scaling, trophy stamping. Never exposed as "score" to players.
- **Flow Meter Contribution**: `FlowSystem` can add to Run Value via `FlowThresholdProgressMultiplier`. Flow Meter final value contributes to threshold progress.
- **Threshold Check**: When `CurrentXP + FlowContribution >= NextRewardThreshold`, trigger `RewardSelectionUI`.
- **Death Handling**: `CurrencySystem.HandleDeath(playerEntity)` calculates loss, creates drop entity at last safe flag.
- **Drop Entity**: `XPDropComponent` with value, position, lifetime. Visual: gold color, mid layer.
- **Threshold Update**: After reward selection, `NextRewardThreshold += RewardThresholdInterval * rewardsToGive`.
- **Safe Flag Position**: `FlagSystem.GetLastSafeFlagPosition()` returns last passed flag (start if none passed).

## Data Model

**CurrencyComponent** (`GL2Project/ECS/Components.cs`):
- `CurrentXP`: int
- `TotalCollected`: int (lifetime)
- `NextRewardThreshold`: int
- `RewardThresholdInterval`: int (default: 100)

**XPCollectibleComponent** (`GL2Project/ECS/Components.cs`):
- `Value`: int (XP amount)
- `IsCollected`: bool

**XPDropComponent** (`GL2Project/ECS/Components.cs`):
- `Value`: int (XP in drop)
- `IsCollected`: bool
- `Lifetime`: float (seconds)

**CurrencySystem** (`GL2Project/Gameplay/CurrencySystem.cs`):
- `_world`: GameWorld
- `_rewardThresholdInterval`: int (default: 100)
- `_deathDropPositions`: Dictionary<Entity, Vector2>

## Algorithms / Order of Operations

### XP Collection

1. **Player Touches Collectible**: `CurrencySystem.Update()` checks proximity (30px radius)
2. **Add XP**: `AddXP(playerEntity, collectible.Value)` - Add to current and total
3. **Get Flow Contribution**: Read `FlowSystem.GetFlowContribution()` - Flow Meter contribution to Run Value (RV)
4. **Check Threshold**: If `CurrentXP + FlowContribution >= NextRewardThreshold`:
   - Calculate rewards to give: `rewardsToGive = (CurrentXP + FlowContribution) / NextRewardThreshold`
   - Update threshold: `NextRewardThreshold += RewardThresholdInterval * rewardsToGive`
   - Trigger UI: `RewardSelectionUI.ShowRewardSelection(lootTable, rng)` - Shows "Reward Quality" (not "score")
5. **Mark Collected**: Set `IsCollected = true`, remove entity after animation

### Death Handling

1. **Death Event**: `CurrencySystem.HandleDeath(playerEntity)` called
2. **Calculate Loss**: `lostXP = CurrentXP / 2` (rounded down)
3. **Update Currency**: `CurrentXP -= lostXP`
4. **Get Safe Position**: `FlagSystem.GetLastSafeFlagPosition()` - Last passed flag or player position
5. **Create Drop**: `CreateXPDrop(position, lostXP)` - Entity with `XPDropComponent`, `Position`, `Renderable`
6. **Store Position**: `_deathDropPositions[playerEntity] = position` for retrieval tracking

### Drop Retrieval

1. **Proximity Check**: `CurrencySystem.Update()` checks player distance to drop positions
2. **Find Drop Entity**: Iterate `XPDrops`, find entity at drop position within pickup radius (50px)
3. **Collect**: `AddXP(playerEntity, drop.Value)`, mark `IsCollected = true`
4. **Remove Tracking**: Remove from `_deathDropPositions`

### Reward Selection

1. **Threshold Reached**: `RewardSelectionUI.ShowRewardSelection(lootTable, rng)` called
2. **Roll Rewards**: `lootTable.RollLoot(rng, 3)` - Get 3 options
3. **Display UI**: Show 3 reward options, highlight selected (default: first). Display "Reward Quality" (discrete tier 0-5), never "score".
4. **Player Selects**: Arrow keys to navigate, Enter to confirm
5. **Apply Reward**: Add selected reward to inventory, update threshold. Reward Quality stored in Trophy Stamp.

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `rewardThresholdInterval` | int | 50-500 | 100 | XP between reward selections |
| `deathPenaltyPercent` | int | 0-100 | 50 | Percent of XP lost on death |
| `pickupRadius` | float | 10-100 | 30 px | XP collectible pickup radius |
| `dropPickupRadius` | float | 20-200 | 50 px | XP drop retrieval radius |
| `dropLifetime` | float | 0-∞ | ∞ | Drop timeout (0 = never expires) |

## Edge Cases + Counters

- **XP = 0 on death**: No drop created. Nothing to lose.
- **Multiple thresholds in one collection**: Calculate all rewards at once, update threshold accordingly.
- **Drop at same position**: Use closest drop, or merge drops (future enhancement).
- **Drop timeout**: Remove drop entity after `dropLifetime` seconds (if set).
- **Reward selection during death**: Queue reward selection, show after respawn.

## Telemetry Hooks

- Log XP collection: `XPCollected(collectibleId, value, currentXP, totalCollected)`
- Log threshold reached: `RewardThresholdReached(currentXP, threshold, rewardsToGive)`
- Log death: `DeathXP(previousXP, lostXP, dropPosition, timestamp)`
- Log drop retrieval: `XPDropRetrieved(dropValue, currentXP, timestamp)`
- Log reward selection: `RewardSelected(rewardOption, rewardId, timestamp)`

## Implementation Notes

**File**: `GL2Project/Gameplay/CurrencySystem.cs`, `GL2Project/UI/RewardSelectionUI.cs`

**Key Systems**:
- `CurrencySystem`: XP collection, death handling, drop management, threshold checking, Run Value (RV) calculation
- `FlowSystem`: Provides Flow Meter contribution to Run Value (see [Flow Meter System](Flow-Meter-System))
- `RewardSelectionUI`: Hades-style reward selection UI (shows "Reward Quality", never "score")
- `FlagSystem`: Provides safe flag position for death drops

**Deterministic Ordering**:
1. `CurrencySystem.Update()` - Check collectibles, check drops
2. `AddXP()` - Update currency, check threshold
3. `HandleDeath()` - Calculate loss, create drop
4. `RewardSelectionUI` - Show selection, apply reward

**Component Stores**: `GameWorld.Currencies`, `GameWorld.XPCollectibles`, `GameWorld.XPDrops`

**Tuning File**: `GL2Project/Tuning/StageGenerationTuning.json` (currency section)

**Loot Table**: Uses `LootTable` from `GL2Project/Inventory/LootTable.cs` with Reward RNG stream.

