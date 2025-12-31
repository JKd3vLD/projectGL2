# FAST vs SLOW Stages [MVP Core]

**Purpose**: Defines the pacing system where players choose between FAST (speed/flow) and SLOW (exploration/precision) stages. Both paths available, never blocks progression.

## Player-Facing Rules

- **Pacing Choice**: Each stage offers FAST or SLOW pacing. Player chooses before entering stage.
- **FAST Stages**: Focus on speed, flow, and perfect execution. Timer shows bonus potential (not time limit). Rewards: Speed bonus, Flow Meter multiplier, perfect line bonus.
- **SLOW Stages**: Focus on exploration, secrets, and quest completion. Rewards: Exploration bonus, quest bonus, collection streak bonus, Flow Meter multiplier.
- **No Progression Block**: Both pacing options available. Choosing one never locks the other. Can replay stage with different pacing.
- **Visual Indicators**: Stage node UI shows pacing icon (⏱ FAST, 🗺 SLOW) and reward preview type.

## System Rules

- **Pacing Tag**: `PacingTag` enum: `FAST` or `SLOW`. Stored in `Stage` and `SectionDef`.
- **Section Filtering**: `SectionPool` filters sections by `PacingTag` when assembling stages.
- **Reward Calculation**: `FastRewardCalculator` for FAST stages, `SlowRewardCalculator` for SLOW stages.
- **Stage Assembly Rules**:
  - FAST: 3-6 sections, prefer SHORT/MED length. High obstacle/enemy density. Fewer branches.
  - SLOW: 2-4 sections, prefer MED/LONG length. More side pockets, secrets, bonus doors. Precision challenges.
- **HUD Differences**: FAST shows timer, speed rank, Flow Meter (stage-local bar). SLOW shows exploration checklist, quest items, carry progress, Flow Meter.
- **Flow Meter Integration**: Flow Meter is stage-local, resets at stage start. Feeds into bonus reward calculations via multiplier. See [Flow Meter System](Flow-Meter-System) for details.

## Data Model

**PacingTag** (`GL2Project/World/SectionDef.cs`):
- `FAST`: Speed/flow pressure
- `SLOW`: Exploration/precision

**RewardProfile** (`GL2Project/World/StagePlan.cs`):
- `SPEED`: FAST stage rewards
- `TREASURE`: SLOW exploration rewards
- `QUEST`: SLOW quest item rewards
- `MIXED`: Combination (mastery stages)

**FastStageMetrics** (`GL2Project/World/FastRewardCalculator.cs`):
- `FinishTime`: float (seconds)
- `TotalIdleTime`: float (seconds)
- `AverageSpeed`: float (px/s)
- `DamageTaken`: int
- `MissedGates`: int
- `DifficultyStars`: int (1-5)
- `EstimatedLength`: float (seconds)
- `FlowFinal`: float (0..1, final Flow Meter value from FlowSystem)

**SlowStageMetrics** (`GL2Project/World/SlowRewardCalculator.cs`):
- `TreasuresFound`: int
- `SecretsFound`: int
- `BonusDoorsCompleted`: int
- `QuestItemsCollected`: int
- `CollectionStreak`: int
- `DamageTaken`: int
- `SidePocketsCompleted`: int
- `DifficultyStars`: int (1-5)
- `CarryObjectiveCompleted`: bool
- `FlowFinal`: float (0..1, final Flow Meter value from FlowSystem)

## Algorithms / Order of Operations

### Stage Selection

1. **Player Views Stage Node**: UI shows pacing icons and reward preview
2. **Player Chooses Pacing**: Select FAST or SLOW (if stage supports both)
3. **Assemble Stage Plan**: `StageAssembler.AssembleStage(tier, signature, pacing, rewardProfile)`
4. **Filter Sections**: `SectionPool.GetAvailableSections(tier, signature, pacing)`
5. **Apply Assembly Rules**:
   - FAST: Select 3-6 sections (prefer SHORT/MED), high density
   - SLOW: Select 2-4 sections (prefer MED/LONG), add side pockets
6. **Load Stage**: Merge section JSON files, place flags, create entities

### Reward Calculation (FAST)

1. **Collect Metrics**: Track finish time, idle time, average speed, damage, missed gates
2. **Read Flow Meter**: Get `FlowFinal` from `FlowSystem` (stage-local Flow Meter final value)
3. **Calculate Base Reward**: `50 + (difficultyStars * 10) + (estimatedLength * 0.5)`
4. **Calculate Speed Bonus**: Find time tier from thresholds, apply exponential bonus (`tier² * 25`)
5. **Apply Flow Meter Multiplier**: `rewardMultiplier = 1.0 + (FlowFinal * FlowRewardMultiplierRange)` (default: 1.0 to 1.5x)
6. **Calculate Perfect Line Bonus**: If damage = 0 AND missedGates = 0: bonus = 100
7. **Total**: `(BaseReward + SpeedBonus + PerfectLineBonus) * FlowMeterMultiplier`

### Reward Calculation (SLOW)

1. **Collect Metrics**: Track treasures, secrets, bonus doors, quest items, collection streak, damage
2. **Read Flow Meter**: Get `FlowFinal` from `FlowSystem` (stage-local Flow Meter final value)
3. **Calculate Base Reward**: `75 + (difficultyStars * 15) + (sidePocketsCompleted * 20)`
4. **Calculate Exploration Bonus**:
   - Treasures: `treasuresFound * 10`
   - Secrets: `min(secretsFound, quota) * 15 + (extra * 7)` (diminishing returns)
   - Bonus doors: `min(doorsCompleted, quota) * 25 + (extra * 12)` (diminishing returns)
5. **Calculate Quest Bonus**: `questItemsCollected * 20`
6. **Calculate Collection Streak Bonus**: If damage = 0 AND streak ≥ 3: `streak² * 5`
7. **Apply Flow Meter Multiplier**: `rewardMultiplier = 1.0 + (FlowFinal * FlowRewardMultiplierRange)` (default: 1.0 to 1.5x)
8. **Total**: `(BaseReward + ExplorationBonus + QuestBonus + StreakBonus) * FlowMeterMultiplier`

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `fastStage.minSections` | int | 2-10 | 3 | Minimum sections for FAST stage |
| `fastStage.maxSections` | int | 3-10 | 6 | Maximum sections for FAST stage |
| `slowStage.minSections` | int | 1-5 | 2 | Minimum sections for SLOW stage |
| `slowStage.maxSections` | int | 2-6 | 4 | Maximum sections for SLOW stage |
| `fastStage.timeBonusThresholds` | float[] | - | [30, 60, 90, 120] | Time tiers in seconds |
| `fastStage.flowBonusIdleThreshold` | float | 0-5 | 2.0 | [Deprecated] Max idle time for Flow Meter decay acceleration (see Flow Meter System) |
| `slowStage.secretQuotaPerStage` | int | 0-10 | 5 | Secrets needed for full bonus |
| `slowStage.bonusDoorQuotaPerStage` | int | 0-5 | 2 | Bonus doors needed for full bonus |

## Edge Cases + Counters

- **Stage supports both pacing**: Player chooses. Both options always available.
- **No sections for pacing**: Fall back to other pacing or use any available sections.
- **Finish time = 0**: Handle gracefully (perfect speed bonus).
- **Collection streak broken**: Reset streak, no bonus. Can rebuild.
- **Side pockets empty**: SLOW stage still playable, just no side pocket rewards.

## Telemetry Hooks

- Log pacing choice: `PacingChoice(stageId, pacingTag, timestamp)`
- Log FAST metrics: `FastStageComplete(stageId, finishTime, idleTime, averageSpeed, damageTaken, rewards)`
- Log SLOW metrics: `SlowStageComplete(stageId, treasuresFound, secretsFound, questItemsCollected, rewards)`
- Log reward calculation: `RewardCalculated(stageId, pacingTag, baseReward, bonuses, totalReward)`

## Implementation Notes

**File**: `GL2Project/World/StageAssembler.cs`, `GL2Project/World/FastRewardCalculator.cs`, `GL2Project/World/SlowRewardCalculator.cs`

**Key Systems**:
- `StageAssembler`: Filters sections by pacing tag, applies assembly rules
- `FlowSystem`: Manages stage-local Flow Meter, feeds into reward calculations (see [Flow Meter System](Flow-Meter-System))
- `FastRewardCalculator`: Calculates speed/perfect bonuses, applies Flow Meter multiplier
- `SlowRewardCalculator`: Calculates exploration/quest/streak bonuses, applies Flow Meter multiplier
- `StageHUD`: Shows pacing-specific UI (timer vs checklist) and Flow Meter bar

**Deterministic Ordering**:
1. Player chooses pacing
2. Filter sections by pacing tag
3. Assemble stage plan
4. Play stage, collect metrics
5. Calculate rewards based on pacing

**Tuning File**: `GL2Project/Tuning/StageGenerationTuning.json`

**UI Components**:
- `StageNodeUI`: Shows pacing icons and reward preview
- `StageHUD`: Shows pacing-specific metrics during play

