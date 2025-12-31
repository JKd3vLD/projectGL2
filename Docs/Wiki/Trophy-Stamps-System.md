# Trophy Stamps System [Later]

**Purpose**: Provide long-tail motivation and "run history" without forcing grind, and encode seed sharing identity.

## Player-Facing Rules

- After each stage and at tier completion, you receive a **Trophy Stamp**:
  - Stage results (FAST/SLOW, time tier, Flow grade)
  - Seed badge (tier-scoped)
  - Key collectibles (letters/treasures/bonus clears)
- Trophies are informational + cosmetic rewards; they can also unlock catalogue entries and hub upgrades later.
- Player-facing: Show as "Stage Trophy" and "Tier Mastery Trophy" in UI. Display Flow Grade as letter (D/C/B/A/S).

## System Rules

- Stamps are **immutable snapshots** (cannot be modified after creation).
- Tier Mastery Trophy stores:
  - Tier index
  - 3-biome signature
  - Seed badge combo
  - Mastery unlock checklist completion state
- Trophies persist across game over (unless explicitly cleared by keep/lose mapping).
- Flow Grade calculated from Flow Meter final value using thresholds (D/C/B/A/S).

## Data Model

**StageTrophyStamp** (`GL2Project/Gameplay/TrophySystem.cs`):
- `TierIndex`: int
- `StageId`: string
- `PacingTag`: PacingTag (FAST/SLOW)
- `SeedBadgeHash`: ulong (hash of seed badge for this stage)
- `TimeSeconds`: float (completion time, FAST only)
- `TimeTier`: int (0-4, FAST only, 0=best)
- `FlowFinal`: float (0..1, final Flow Meter value)
- `FlowGrade`: FlowGrade enum (D/C/B/A/S)
- `SecretsFound`: int
- `BonusCompleted`: int
- `CarryDelivered`: int
- `DamageTaken`: int
- `Deaths`: int
- `RewardQuality`: int (discrete tier, 0-5)

**TierMasteryTrophy** (`GL2Project/World/TierMastery.cs`):
- `TierIndex`: int
- `BiomeSignature`: BiomeSignature
- `SeedBadge`: SeedBadge (tier-scoped seed combination)
- `StageStamps`: StageTrophyStamp[] (7 stamps, one per stage)
- `MasteryComplete`: bool (all mastery requirements met)
- `CompletionTime`: DateTime (optional, timestamp)

**TrophySystem** (`GL2Project/Gameplay/TrophySystem.cs`):
- `_world`: GameWorld
- `_stageStamps`: Dictionary<string, StageTrophyStamp> (key: stageId)
- `_tierTrophies`: Dictionary<int, TierMasteryTrophy> (key: tierIndex)
- `CreateStageStamp(stageId, metrics)`: StageTrophyStamp
- `CreateTierTrophy(tierIndex, stageStamps)`: TierMasteryTrophy

## Algorithms / Order of Operations

### Stage Trophy Creation

1. **On Stage End**: `TrophySystem.CreateStageStamp()` called with stage metrics
2. **Collect Metrics**: Gather from `StageHUD`, `FlowSystem`, `CurrencySystem`:
   - Completion time (FAST only)
   - Flow Meter final value
   - Secrets found, bonuses completed, carries delivered
   - Damage taken, deaths
   - Reward quality (from reward selection)
3. **Calculate Flow Grade**: Use `FlowGradeThresholds`:
   - Flow < 0.25: D
   - Flow < 0.5: C
   - Flow < 0.75: B
   - Flow < 0.9: A
   - Flow >= 0.9: S
4. **Calculate Time Tier** (FAST only): Use `TimeTierThresholdsFast`:
   - Find tier from thresholds (0=best, 4=worst)
5. **Create Stamp**: Build `StageTrophyStamp` struct
6. **Persist**: Save to save file (or in-memory for run)

### Tier Trophy Creation

1. **On Tier Completion**: `TrophySystem.CreateTierTrophy()` called
2. **Aggregate Stage Stamps**: Collect all 7 `StageTrophyStamp` objects from tier
3. **Check Mastery**: Verify `MasteryComplete` flag (all mastery requirements met)
4. **Create Trophy**: Build `TierMasteryTrophy` with:
   - Tier index
   - Biome signature (from TierPackage)
   - Seed badge (from tier seed selection)
   - Stage stamps array
   - Mastery completion state
5. **Persist**: Save to save file

### Flow Grade Calculation

1. **Read Flow Final**: Get `FlowComponent.Flow` at stage end
2. **Normalize**: Ensure value is in [0, 1] range (divide by FlowMax if needed)
3. **Compare Thresholds**: Use `FlowGradeThresholds` array:
   - Iterate thresholds in ascending order
   - Find first threshold where Flow >= threshold
   - Return corresponding grade (D/C/B/A/S)

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `FlowGradeThresholds` | float[] | 3–6 entries | [0.25, 0.5, 0.75, 0.9] | D/C/B/A/S thresholds |
| `TimeTierThresholdsFast` | float[] | per tier | tier-tuned | Used only for FAST stages |
| `TrophyRetentionOnGameOver` | enum | - | KeepAll | Trophies persist (or ClearOnGameOver) |

## Edge Cases + Counters

- **Save bloat prevention**: Keep compact stamps, aggregate older ones into summaries (future enhancement).
- **No negative labels**: Ensure stamps don't create "punishment feel" - no negative labels, just "missed bonus tier".
- **Flow Grade ties**: Use lower bound (e.g., Flow=0.5 exactly = C grade, not B).
- **Missing metrics**: Handle gracefully (default values: Flow=0, TimeTier=4, etc.).

## Telemetry Hooks

- Log stamp creation: `StageTrophyCreated(stageId, flowGrade, timeTier, rewardQuality, timestamp)`
- Log tier trophy: `TierTrophyCreated(tierIndex, masteryComplete, averageFlowGrade, timestamp)`
- Log stamp distributions: `TrophyDistribution(pacingTag, flowGradeDistribution[], timestamp)` (optional, aggregate)

## Implementation Notes

**File**: `GL2Project/Gameplay/TrophySystem.cs` (to be created)

**Key Systems**:
- `TrophySystem`: Creates and manages trophy stamps
- `SaveSystem`: Persists trophies to save file
- `TierMastery`: Stores tier mastery trophies (existing)

**Deterministic Ordering**:
1. Stage completion
2. Collect metrics from systems
3. Calculate Flow Grade and Time Tier
4. Create StageTrophyStamp
5. Persist to save file
6. On tier completion: Aggregate into TierMasteryTrophy

**Component Stores**: Trophies stored in save file, not ECS components (persistent data).

**Serialization**: Store hashes/IDs, not strings. Keep trophy serialization deterministic and versioned.

**Tuning File**: `GL2Project/Tuning/TrophyTuning.json` (to be created)

**UI Components**: `TrophyDisplayUI` - Shows stage/tier trophies, Flow Grade display, seed badge sharing.

**Seed Sharing**: Trophy stamps encode seed badge hash, allowing players to share "replay this exact tier" codes.

