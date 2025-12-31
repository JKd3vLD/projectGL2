# Reward Calculators [MVP Core]

**Purpose**: Calculates rewards for FAST and SLOW stages based on performance metrics. Separate calculators for each pacing type.

## Player-Facing Rules

- **FAST Rewards**: Speed bonus (time tiers), Flow Meter multiplier (stage-local Flow Meter affects bonus payout), perfect line bonus (no damage, no missed gates).
- **SLOW Rewards**: Exploration bonus (treasures, secrets, bonus doors), quest bonus (quest items), collection streak bonus (multiple treasures without damage), Flow Meter multiplier.
- **Base Reward**: Always granted on completion. Scales with difficulty and length.

## System Rules

- **FastRewardCalculator**: Calculates speed/perfect bonuses for FAST stages, applies Flow Meter multiplier.
- **SlowRewardCalculator**: Calculates exploration/quest/streak bonuses for SLOW stages, applies Flow Meter multiplier.
- **Flow Meter Integration**: Reads `FlowFinal` from `FlowSystem` at stage end, applies multiplier to total rewards. See [Flow Meter System](Flow-Meter-System) for details.
- **Metrics Collection**: `StageHUD` collects metrics during play. Passed to calculator on completion.
- **Reward Calculation**: Called on stage completion. Returns reward breakdown and total (with Flow Meter multiplier applied).

## Data Model

**FastRewardCalculator** (`GL2Project/World/FastRewardCalculator.cs`):
- `_timeBonusThresholds`: float[] (default: [30, 60, 90, 120] seconds)
- `_flowRewardMultiplierRange`: float (default: 0.5, max multiplier = 1.5x at Flow=1.0)

**SlowRewardCalculator** (`GL2Project/World/SlowRewardCalculator.cs`):
- `_maxSidePockets`: int (default: 3)
- `_secretQuotaPerStage`: int (default: 5)
- `_bonusDoorQuotaPerStage`: int (default: 2)

**FastStageMetrics** (`GL2Project/World/FastRewardCalculator.cs`):
- `FinishTime`: float
- `TotalIdleTime`: float
- `AverageSpeed`: float
- `DamageTaken`: int
- `MissedGates`: int
- `DifficultyStars`: int
- `EstimatedLength`: float
- `FlowFinal`: float (0..1, final Flow Meter value from FlowSystem)

**SlowStageMetrics** (`GL2Project/World/SlowRewardCalculator.cs`):
- `TreasuresFound`: int
- `SecretsFound`: int
- `BonusDoorsCompleted`: int
- `QuestItemsCollected`: int
- `CollectionStreak`: int
- `DamageTaken`: int
- `SidePocketsCompleted`: int
- `DifficultyStars`: int
- `CarryObjectiveCompleted`: bool
- `FlowFinal`: float (0..1, final Flow Meter value from FlowSystem)

## Algorithms / Order of Operations

### FAST Reward Calculation

1. **Read Flow Meter**: Get `FlowFinal` from `FlowSystem` (stage-local Flow Meter final value)
2. **Base Reward**: `50 + (difficultyStars * 10) + (estimatedLength * 0.5)`
3. **Speed Bonus**: Find time tier from thresholds:
   - `tier = thresholds.Length - i` where `finishTime <= thresholds[i]`
   - Bonus: `tier * tier * 25` (exponential: 25, 100, 225, 400, ...)
4. **Perfect Line Bonus**: If `damageTaken == 0` AND `missedGates == 0`: bonus = 100
5. **Calculate Flow Meter Multiplier**: `rewardMultiplier = 1.0 + (FlowFinal * FlowRewardMultiplierRange)` (default: 1.0 to 1.5x)
6. **Total**: `(BaseReward + SpeedBonus + PerfectLineBonus) * FlowMeterMultiplier`

### SLOW Reward Calculation

1. **Read Flow Meter**: Get `FlowFinal` from `FlowSystem` (stage-local Flow Meter final value)
2. **Base Reward**: `75 + (difficultyStars * 15) + (sidePocketsCompleted * 20)`
3. **Exploration Bonus**:
   - Treasures: `treasuresFound * 10`
   - Secrets: `min(secretsFound, quota) * 15 + (extra * 7)` (diminishing returns)
   - Bonus doors: `min(doorsCompleted, quota) * 25 + (extra * 12)` (diminishing returns)
4. **Quest Bonus**: `questItemsCollected * 20`
5. **Collection Streak Bonus**: If `damageTaken == 0` AND `streak >= 3`: `streak * streak * 5`
6. **Calculate Flow Meter Multiplier**: `rewardMultiplier = 1.0 + (FlowFinal * FlowRewardMultiplierRange)` (default: 1.0 to 1.5x)
7. **Total**: `(BaseReward + ExplorationBonus + QuestBonus + StreakBonus) * FlowMeterMultiplier`

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `fastStage.timeBonusThresholds` | float[] | - | [30, 60, 90, 120] | Time tiers in seconds |
| `flowRewardMultiplierRange` | float | 0-1 | 0.5 | Max multiplier = 1.5x at Flow=1.0 (see Flow Meter System) |
| `slowStage.secretQuotaPerStage` | int | 0-10 | 5 | Secrets needed for full bonus |
| `slowStage.bonusDoorQuotaPerStage` | int | 0-5 | 2 | Bonus doors needed for full bonus |

## Edge Cases + Counters

- **Finish time = 0**: Handle gracefully (perfect speed bonus).
- **No secrets found**: Exploration bonus still granted for treasures/bonus doors.
- **Collection streak broken**: Reset streak, no bonus. Can rebuild.
- **Perfect line with damage**: No perfect line bonus, but other bonuses still apply.

## Telemetry Hooks

- Log reward calculation: `RewardCalculated(stageId, pacingTag, baseReward, speedBonus, perfectBonus, explorationBonus, questBonus, streakBonus, flowMeterMultiplier, totalReward, timestamp)`
- Log FAST metrics: `FastStageComplete(stageId, finishTime, idleTime, averageSpeed, damageTaken, rewards, timestamp)`
- Log SLOW metrics: `SlowStageComplete(stageId, treasuresFound, secretsFound, questItemsCollected, rewards, timestamp)`

## Implementation Notes

**File**: `GL2Project/World/FastRewardCalculator.cs`, `GL2Project/World/SlowRewardCalculator.cs`

**Key Systems**:
- `FlowSystem`: Manages stage-local Flow Meter, provides `FlowFinal` value (see [Flow Meter System](Flow-Meter-System))
- `FastRewardCalculator`: Calculates FAST stage rewards, applies Flow Meter multiplier
- `SlowRewardCalculator`: Calculates SLOW stage rewards, applies Flow Meter multiplier
- `StageHUD`: Collects metrics during play, displays Flow Meter bar

**Deterministic Ordering**:
1. Stage completion
2. Collect metrics from `StageHUD`
3. Calculate rewards based on pacing tag
4. Store rewards, update currency

**Tuning File**: `GL2Project/Tuning/StageGenerationTuning.json` (fastStage/slowStage sections)

**Metrics Collection**: `StageHUD` tracks metrics during play. See [FAST vs SLOW Stages](FAST-vs-SLOW-Stages) for details.

