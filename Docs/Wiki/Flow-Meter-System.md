# Flow Meter System [Later]

**Purpose**: Make "play well" legible in moment-to-moment feedback for both FAST and SLOW pacing, without changing core movement stats.

## Player-Facing Rules

- Each stage has a **Flow Meter** that fills from pacing-appropriate actions.
- Flow never blocks completion; it only increases **bonus rewards** and/or **reward threshold progress**.
- Flow decays if you stop engaging with the stage's intended pacing (idle, backtracking in FAST, repeated safe resets, etc.).
- Player-facing UI shows "Flow Meter" (stage-local bar) and "Reward Quality" (what you earned), never "score".

## System Rules

- Flow is **stage-local**: resets at stage start; stored in Trophy as a summary (grade + key contributors).
- **FAST stages**:
  - Flow sources: forward momentum, time tiers, "no-stops" behavior, clean traversal chains.
- **SLOW stages**:
  - Flow sources: secrets found, bonus completions, carry deliveries, puzzle clears, catalogue pickups.
- Flow output:
  - Adds a multiplier to stage bonus payout (coins/XP threshold progress/reward roll quality).
  - Never modifies Run/Jump/Glide physics values.
- **Run Value (RV)**: Internal aggregate used for reward threshold progress, bonus payout scaling, trophy stamping. Never exposed as "score" to players.

## Data Model

**FlowComponent** (`GL2Project/ECS/Components.cs`):
- `Flow`: float (0..1, normalized to FlowMax for UI)
- `DecayTimer`: float (seconds since last progress)
- `LastProgressTime`: float (timestamp of last Flow gain)
- `ComboCount`: ushort (optional, for chain tracking)
- `FlowMode`: FlowMode (FAST|SLOW)

**FlowEvent** (typed event bus, `GL2Project/ECS/EventBus.cs`):
- `FlowEventType`: enum (TimeTierHit, Chain, SecretFound, CarryDelivered, BonusComplete, DamageTaken, IdleTick, BacktrackTick)
- `Value`: int or `Delta`: float (Flow change amount)

**FlowSystem** (`GL2Project/Gameplay/FlowSystem.cs`):
- `_world`: GameWorld
- `Update(dt)`: void - Consumes FlowEvents, applies decay, updates FlowComponent

## Algorithms / Order of Operations

### Flow Update (per frame)

1. **Consume FlowEvents**: `FlowSystem.Update()` reads events from ring buffer
2. **Process Events**: For each event:
   - Check eligibility (pacing tag match, cooldowns, caps)
   - Apply Flow delta: `FlowComponent.Flow += event.Delta`
   - Clamp to [0, FlowMax]
   - Update `LastProgressTime`
3. **Apply Decay**: If `DecayTimer > IdleGraceSeconds`:
   - `decayRate = FlowDecayPerSecond * (1.0 + (DecayTimer - IdleGraceSeconds) * accelerationFactor)`
   - `FlowComponent.Flow -= decayRate * dt`
   - Clamp to [0, FlowMax]
4. **Update DecayTimer**: Increment if no progress, reset on Flow gain

### Flow Sources (FAST)

1. **Time Tier Hit**: When player completes section within time tier threshold
   - Flow gain: `timeTierValue * FlowGainMultiplier`
   - Once per section segment (prevents farming)
2. **Forward Momentum**: Maintain forward velocity above threshold for duration
   - Flow gain: `momentumDuration * FlowGainPerSecond`
   - Requires net forward progress (backtracking penalizes)
3. **Clean Traversal Chain**: Complete section without damage
   - Flow gain: `chainLength * FlowGainPerChainLink`
   - Resets on damage

### Flow Sources (SLOW)

1. **Secret Found**: Discover secret room/bonus door
   - Flow gain: `SecretFlowGain` (default: 0.10)
   - Once per secret per stage instance (prevents respawn farming)
2. **Carry Delivered**: Complete carry objective (A→B) without dropping
   - Flow gain: `CarryDeliveryFlowGain` (default: 0.15)
   - Once per carry objective per stage
3. **Bonus Complete**: Clear bonus room
   - Flow gain: `BonusCompleteFlowGain` (default: 0.12)
   - Once per bonus room per stage instance
4. **Puzzle Clear**: Solve puzzle gate
   - Flow gain: `PuzzleClearFlowGain` (default: 0.08)
   - Once per puzzle per stage

### Flow Decay

1. **Idle Detection**: If `DecayTimer > IdleGraceSeconds`:
   - Apply base decay: `FlowDecayPerSecond * dt`
   - Accelerate if idle continues: `decayRate *= (1.0 + idleAccelerationFactor)`
2. **Backtrack Penalty** (FAST only): If player moves backward:
   - Apply `BacktrackPenaltyRate * dt` to Flow
   - Requires net forward progress to avoid penalty

### Flow → Rewards Integration

1. **At Stage End**: `RewardSystem` reads `FlowComponent.Flow`
2. **Calculate Multiplier**: `rewardMultiplier = 1.0 + (Flow * FlowRewardMultiplierRange)`
3. **Apply to Bonuses**: Multiply stage bonus payout by multiplier
4. **Apply to Threshold Progress**: Add `Flow * FlowThresholdProgressMultiplier` to XP threshold progress (Run Value)

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `FlowMax` | float | 0.5–2.0 | 1.0 | Normalize to 1.0 for UI |
| `FlowDecayPerSecond` | float | 0–1 | 0.12 | FAST higher (0.15), SLOW lower (0.08) |
| `IdleGraceSeconds` | float | 0–5 | 1.0 | Time before decay accelerates |
| `DamageFlowPenalty` | float | 0–0.8 | 0.25 | Applied on hit |
| `BacktrackPenaltyRate` | float | 0–1 | 0.15 | FAST only, per second |
| `SecretFlowGain` | float | 0–0.5 | 0.10 | SLOW only |
| `CarryDeliveryFlowGain` | float | 0–0.5 | 0.15 | SLOW only |
| `FlowRewardMultiplierRange` | float | 0–1 | 0.5 | Max multiplier = 1.5x at Flow=1.0 |
| `FlowThresholdProgressMultiplier` | float | 0–100 | 10 | Run Value added per Flow point |

## Edge Cases + Counters

- **FAST farming prevention**: Time-tier Flow only awarded once per section segment. Backtracking reduces Flow.
- **SLOW farming prevention**: Secrets/bonus completions only award Flow once per stage instance (tracked per StagePlan).
- **Assist Modifier cap**: If "Assist Modifier" active, cap max Flow bonus multiplier (e.g., 1.2x instead of 1.5x).
- **Flow overflow**: Clamp to [0, FlowMax]. Never exceed maximum.
- **Decay during pause**: Pause game pauses Flow decay (future enhancement).

## Telemetry Hooks

- Log Flow curve: `FlowUpdate(stageId, currentFlow, delta, eventType, timestamp)` (sampled every 0.5s)
- Log Flow events: `FlowEvent(eventType, value, currentFlow, timestamp)`
- Log Flow at completion: `FlowComplete(stageId, finalFlow, flowGrade, rewardMultiplier, timestamp)`
- Log Flow contributors: `FlowContributors(stageId, topContributors[], timestamp)` (top 3 event types)

## Implementation Notes

**File**: `GL2Project/Gameplay/FlowSystem.cs` (to be created)

**Key Systems**:
- `FlowSystem`: Consumes FlowEvents, applies decay, updates FlowComponent
- `RewardSystem`: Reads FlowComponent at stage end, applies multiplier
- `CurrencySystem`: Uses Flow for Run Value (RV) threshold progress

**Deterministic Ordering**:
1. Gameplay systems emit FlowEvents (movement, pickups, secrets)
2. `FlowSystem.Update()` - Consume events, apply decay
3. `RewardSystem` - Read Flow at stage end, calculate multiplier
4. `CurrencySystem` - Apply Flow to Run Value threshold progress

**Component Stores**: `GameWorld.FlowComponents` (to be added)

**Event Bus**: FlowEvents use typed ring buffer, no per-frame allocations

**UI Integration**: `StageHUD` reads `FlowComponent.Flow` for Flow Meter bar display. Shows "Flow Meter" label, never "score".

**Tuning File**: `GL2Project/Tuning/FlowTuning.json` (to be created)

**No Movement Changes**: Flow Meter never modifies `MovementTuning` values. All effects are reward/flow multipliers only.

