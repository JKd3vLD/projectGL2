# Core Loop & Run Structure [MVP Core]

**Purpose**: Defines the game's core loop, run structure, and progression flow from tier selection to game over.

## Player-Facing Rules

- **Run**: Single playthrough from tier selection to game over or tier completion.
- **Tier Selection**: Choose starting tier (default: max(1, HighestTierReached - 3)). Select seed codes for tier.
- **Stage Selection**: Choose FAST or SLOW pacing for each stage. Both paths available, never blocks progression.
- **Lives**: Start with 3 lives. Reset to 3 on game over.
- **Game Over**: Triggered by lives reaching 0. Regress to `max(1, HighestTierReached - 3)`. Reset lives to 3. Generate new random seeds unless seed locks set.
- **Tier Completion**: Complete all 7 stages + mastery requirements → advance to next tier. Select seed codes for new tier.

## System Rules

- **Fixed 120 Hz simulation**: Update loop runs at exactly 120 Hz. Variable render step decoupled.
- **Deterministic ordering**: Systems update in fixed order. Same inputs produce same results.
- **No per-frame allocations**: Fixed update loop targets zero allocations (ring buffers, SoA storage).
- **Tier-scoped seeds**: Seeds include tier index. Same symbol codes in Tier 1 ≠ Tier 2.
- **Separate RNG streams**: WorldGen (layout), Reward (loot), Bonus (bonus doors), Collectible (pickups). Layout never consumes reward RNG.

## Data Model

**Run State** (`GL2Project/Gameplay/SaveData.cs`):
- `CurrentTier`: int
- `CurrentStageIndex`: int (0-6)
- `Lives`: int (default: 3)
- `HighestTierReached`: int
- `TierStart`: int (starting tier for current run)

**Tier Package** (`GL2Project/World/TierPackage.cs`):
- `TierIndex`: int
- `Biomes`: Biome[3]
- `Stages`: Stage[7]
- `WorldGenSeed`: ulong

**Stage** (`GL2Project/World/Stage.cs`):
- `Id`: string
- `PacingTag`: PacingTag (FAST/SLOW)
- `RewardProfile`: RewardProfile
- `StagePlan`: StagePlan (assembled sections)
- `MasteryRequirements`: MasteryRequirements

## Algorithms / Order of Operations

### Run Start

1. **Load Save Data**: Read `HighestTierReached`, `TierStart`
2. **Calculate Starting Tier**: `TierStart = max(1, HighestTierReached - 3)`
3. **Seed Selection UI**: Player selects symbol codes for tier (if not locked)
4. **Resolve Seeds**: `SeedResolver.ResolveSeed(hashVersion, tierIndex, categoryId, symbolIds)`
5. **Initialize RNG Streams**: Create `RngStreams` with resolved seed
6. **Generate Tier Package**: `WorldGenerator.GenerateTierPackage(tierIndex, worldGenStream)`
7. **Initialize Systems**: Load content packs, initialize GameWorld, create player entities

### Stage Play

1. **Stage Selection**: Player chooses stage from tier package (0-6)
2. **Pacing Choice**: Player chooses FAST or SLOW (if stage supports both)
3. **Load Stage Plan**: `StageAssembler.AssembleStage()` if not pre-generated
4. **Load Level Data**: `LevelLoader.LoadStagePlan(stagePlan)` - merge section JSON files
5. **Place Flags**: `FlagPlacer.PlaceFlags(stagePlan)` - start, middle (if LONG sections), end
6. **Create Entities**: Spawn player, flags, collectibles, enemies from level data
7. **Game Loop**: Fixed 120 Hz update, variable render

### Game Over

1. **Trigger**: Lives reach 0 or explicit game over event
2. **Apply Keep/Lose Mapping**: Determine what persists (stub for now)
3. **Calculate New TierStart**: `TierStart = max(1, HighestTierReached - 3)`
4. **Reset Lives**: `Lives = 3`
5. **Generate Random Seeds**: Unless seed locks set, generate new random seeds for tier
6. **Save State**: Write `HighestTierReached`, `TierStart`, `Lives` to save file
7. **Return to Tier Selection**: Show tier selection UI with new tier start

### Tier Completion

1. **Check Mastery**: Verify all mastery requirements met (letters, artifacts, key pass)
2. **Calculate Rewards**: `FastRewardCalculator` or `SlowRewardCalculator` based on pacing
3. **Store Seed Badge**: Save seed combination to `TierMastery.SeedBadge`
4. **Advance Tier**: `CurrentTier++`, `HighestTierReached = max(HighestTierReached, CurrentTier)`
5. **Seed Selection UI**: Player selects seed codes for new tier
6. **Generate New Tier Package**: Create tier package for next tier
7. **Continue Run**: Start first stage of new tier

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `initialLives` | int | 1-10 | 3 | Lives at run start |
| `tierRegressionAmount` | int | 1-5 | 3 | Tiers to regress on game over |
| `minTierStart` | int | 1+ | 1 | Minimum starting tier |

## Edge Cases + Counters

- **TierStart < 1**: Clamp to 1. Never start below tier 1.
- **HighestTierReached = 0**: First run, start at tier 1.
- **Seed locks set**: Use locked seeds instead of random generation on game over.
- **Mastery incomplete**: Cannot advance tier. Must complete all requirements.
- **All stages completed but mastery incomplete**: Show mastery checklist UI, block tier advance.

## Telemetry Hooks

- Log run start: `RunStart(tierIndex, seedHash, timestamp)`
- Log stage start: `StageStart(stageId, pacingTag, timestamp)`
- Log stage completion: `StageComplete(stageId, finishTime, rewards)`
- Log game over: `GameOver(reason, tierReached, timestamp)`
- Log tier completion: `TierComplete(tierIndex, seedBadge, timestamp)`

## Implementation Notes

**File**: `GL2Project/Gameplay/GameOverSystem.cs`, `GL2Project/Game1.cs`

**Key Systems**:
- `GameOverSystem`: Handles game over logic, tier regression, seed generation
- `WorldGenerator`: Generates tier packages from seeds
- `SeedResolver`: Resolves tier-scoped seeds from symbol codes

**Deterministic Ordering**:
- Run start: Seed resolution → RNG stream init → Tier package generation
- Stage play: Stage selection → Plan assembly → Level loading → Entity creation
- Game over: Keep/lose mapping → Tier calculation → Seed generation → Save

**Save System**: `SaveData` struct serialized to JSON. Location: TBD (likely `%AppData%/GL2Engine/save.json`).

**Seed Persistence**: Seed badges stored in `TierMastery` per tier. Allows replaying exact tier layouts.

