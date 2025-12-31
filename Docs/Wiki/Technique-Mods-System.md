# Technique Mods System [Later]

**Purpose**: Let relics change "how you engage" (timing windows, interaction triggers, rewards) without changing baseline movement parameters.

## Player-Facing Rules

- Technique Mods are relics that add **contextual bonuses** for using movement well.
- They do not make you faster, jump higher, or permanently change your kit.
- They can:
  - Reward good execution (more Flow, better loot tier)
  - Add optional constraints (challenge variants)
  - Add small helper effects (safety, economy)
- Never say "+score" - say "+Flow" or "+Reward Meter progress" (and only when it makes sense).

## System Rules

- Technique Mods are **run-scoped** unless explicitly flagged as persistent by keep/lose mapping.
- Effects apply through **event hooks**, not direct physics edits:
  - OnGlideStart/End
  - OnCartwheelStart/End
  - OnAirborneChain
  - OnSectionComplete
  - OnSecretFound
- Any Technique Mod that interacts with glide/cartwheel must be expressed as **reward/flow hooks**, not "+velocity" or "+jump".
- Processed in **deterministic slot order** (equip order matters for consistency).

## Data Model

**RelicDef** (`GL2Project/Inventory/ItemDef.cs`):
- `Id`: string
- `TagBits`: ulong (synergy tags, bitmask)
- `RelicType`: RelicType enum (TechniqueMod, StatMod, etc.)
- `Triggers[]`: FlowEventType[] (enum array of trigger conditions)
- `Effects[]`: TechniqueEffect[] (small struct array: kind + magnitude + constraints)

**TechniqueEffect** (`GL2Project/Gameplay/RelicSystem.cs`):
- `Kind`: EffectKind enum (FlowDelta, ThresholdProgressDelta, RewardQualityDelta, TokenGrant, UIHint)
- `Magnitude`: float or int (depending on kind)
- `Constraints`: EffectConstraints struct (cooldown, per-section max, eligibility)

**TechniqueProcState** (`GL2Project/ECS/Components.cs`):
- `CooldownTimers`: float[] (one per equipped Technique Mod)
- `PerSectionTriggered`: bool[] (flags for "already triggered this section")
- `ChargeCounts`: int[] (for charge-based mods)

**RelicSystem** (`GL2Project/Gameplay/RelicSystem.cs`):
- `_world`: GameWorld
- `_equippedRelics`: List<RelicDef> (run-scoped, deterministic order)
- `EvaluateTriggers(eventType, context)`: void - Checks equipped mods, applies effects

## Algorithms / Order of Operations

### Technique Mod Evaluation

1. **Gameplay Event**: System raises event (e.g., `OnGlideEnd`, `OnSecretFound`)
2. **RelicSystem.EvaluateTriggers()**: Called with event type and context
3. **Iterate Equipped Mods**: For each mod in `_equippedRelics` (deterministic order):
   - Check if mod's `Triggers[]` contains event type
   - Check eligibility (pacing tag, stage type, constraints)
   - Check cooldown: `TechniqueProcState.CooldownTimers[slotIndex] <= 0`
   - Check per-section cap: `TechniqueProcState.PerSectionTriggered[slotIndex] == false` OR `triggerCount < MaxProcsPerSection`
4. **Apply Effect**: If eligible:
   - Emit FlowEvent or modify RewardQuality or grant token
   - Update `TechniqueProcState`: set cooldown, increment trigger count
   - Log proc for telemetry

### Cooldown Management

1. **On Proc**: Set `CooldownTimers[slotIndex] = TechniqueProcCooldown`
2. **Per Frame**: Decrement all cooldowns: `CooldownTimers[i] -= dt`
3. **On New Ground Contact**: Reset per-section flags (prevents farming in safe spots)
4. **On New Section**: Reset per-section trigger counts

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `TechniqueProcCooldown` | float | 0–10s | 1.5s | Prevents spam |
| `MaxProcsPerSection` | int | 0–999 | 3 | Hard cap per section |
| `FlowBonusPerProc` | float | 0–0.3 | 0.05 | Typical Flow gain |
| `RewardQualityDelta` | int | -2..+2 | +1 | Discrete "tier bump" |

## Edge Cases + Counters

- **Hard caps per section**: Prevent farming in safe spots. Reset on new section or new ground contact.
- **Cooldowns tied to new ground contact**: Not button mashing. Requires actual gameplay progress.
- **Eligibility checks**: Each mod must specify eligibility (e.g., only in FAST stages; only while timer tier active).
- **Exploit checks**: Each mod must specify what spam loop it prevents and how it's countered.

## Telemetry Hooks

- Log proc counts: `TechniqueModProc(modId, eventType, effectApplied, timestamp)`
- Log proc causes: `TechniqueModTrigger(modId, triggerContext, eligibilityResult, timestamp)`
- Log reward quality changes: `RewardQualityChange(modId, previousQuality, newQuality, timestamp)`

## Implementation Notes

**File**: `GL2Project/Gameplay/RelicSystem.cs` (to be created)

**Key Systems**:
- `RelicSystem`: Evaluates equipped Technique Mods, applies effects
- `FlowSystem`: Consumes FlowEvents from Technique Mods
- `RewardSystem`: Applies RewardQualityDelta from Technique Mods

**Deterministic Ordering**:
1. Gameplay systems raise events
2. `RelicSystem.EvaluateTriggers()` - Process equipped mods in order
3. Effects applied (FlowEvents, RewardQuality changes)
4. `FlowSystem` / `RewardSystem` consume effects

**Component Stores**: `GameWorld.TechniqueProcStates` (to be added)

**Effect Structs**: Keep small, avoid dynamic lists at runtime. Pre-bake trigger maps into arrays per trigger enum.

**Tuning File**: `GL2Project/Tuning/RelicTuning.json` (to be created)

## Concrete Tech Modifier Examples

### FAST-oriented Tech Modifiers

#### 1) Soft Landing
- **Trigger condition**: Release Glide within **120 ms** before ground contact, then land without damage.
- **Payoff**: `+Flow` (small, default: 0.05) and `+RewardThresholdProgress` (tiny, default: 2 RV).
- **Constraints**: Cooldown **2.0s**; max **3 procs per section**.
- **Exploit check**: Player glide-taps repeatedly on flat ground.
- **Counter**: Requires **airborne time ≥ 0.35s** and **vertical drop ≥ minHeight** (default: 64px) before landing.

#### 2) No-Stops Dividend
- **Trigger condition**: Maintain **forward velocity above threshold** (default: 80% of run speed) for **5.0s** in a FAST stage (ignore brief collisions).
- **Payoff**: `+Flow` (medium, default: 0.15) and **+1 "Speed Tier token"** (used at stage end for bonus).
- **Constraints**: Only in `PacingTag=FAST`; resets token stack on taking damage.
- **Exploit check**: Farm by pacing back and forth in a safe corridor.
- **Counter**: Requires **net forward progress** (stage distance delta ≥ 200px) and penalizes backtracking ticks.

#### 3) Chain Exit Bonus
- **Trigger condition**: Complete a section with `TraversalMode=VEHICLE` or `AUTOSCROLL` without damage.
- **Payoff**: **Reward quality +1 step** for the next threshold reward roll.
- **Constraints**: Once per stage; disabled if Assist Modifiers are active.
- **Exploit check**: Player repeats the same easy vehicle segment via respawn to farm.
- **Counter**: Only triggers on **first clear of that segment instance** in the current StagePlan.

#### 4) Clean Sprint Refund
- **Trigger condition**: Finish a FAST stage within **Time Tier A** (best threshold, default: 30s).
- **Payoff**: Refund **one consumable flag charge** (if any spent) OR grant a small coin bundle (default: 50 coins).
- **Constraints**: Only if **a consumable flag was used** during the stage (or else coins).
- **Exploit check**: Players intentionally spend a flag at end to refund.
- **Counter**: Refund only if flag was used **before 60% stage progress**.

### SLOW-oriented Tech Modifiers

#### 5) Cartographer's Instinct
- **Trigger condition**: Discover a **secret room / bonus door** in a SLOW stage.
- **Payoff**: Spawn an extra **Treasure Ping** (UI hint) toward the nearest remaining secret/treasure *within that stage*.
- **Constraints**: Max **2 pings per stage**; pings do not reveal exact location, only direction/approx distance (default: 200px radius).
- **Exploit check**: Encourages brute-force wall-checking.
- **Counter**: Ping only triggers after **completing a meaningful action** (e.g., chest opened, bonus cleared, carry delivered), not on "secret found" alone.

#### 6) Courier's Pocket
- **Trigger condition**: Deliver a carry objective (A→B) without dropping/breaking the item.
- **Payoff**: `+Flow` (medium, default: 0.15) and **+QuestYield** (e.g., +1–2 extra quest collectibles from that stage's quest pool).
- **Constraints**: Only in `PacingTag=SLOW`; max **1 proc per carry objective**.
- **Exploit check**: Farm carry deliveries by re-triggering pickup.
- **Counter**: Carry objectives are **single-completion** per stage instance; additional deliveries give no bonus.

#### 7) Bonus Specialist
- **Trigger condition**: Clear a bonus room (DKC2 pattern) in a SLOW stage.
- **Payoff**: Add **+1 Bonus Token** used at stage end to upgrade reward selection (e.g., reroll once).
- **Constraints**: Max **2 tokens per stage**; tokens expire on stage exit.
- **Exploit check**: Re-enter bonus rooms to farm tokens.
- **Counter**: Bonus room completion is recorded per StagePlan instance; **no repeat credit**.

#### 8) Gentle Hands
- **Trigger condition**: Open **3 distinct chests** in one SLOW stage without taking damage.
- **Payoff**: Next reward threshold roll offers **+1 option** (4 choices instead of 3).
- **Constraints**: Once per stage; resets if damage taken.
- **Exploit check**: Players camp easy chest clusters.
- **Counter**: Requires chest `DifficultyStarsSum ≥ X` (default: 6) OR at least one chest in a higher-risk tag (e.g., `VerticalAscent`, `CarryProp`, `BarrelCannon`).

### Cross-pacing / Universal Tech Modifiers

#### 9) Precision Banker
- **Trigger condition**: Complete any section without damage and without using a consumable flag.
- **Payoff**: Bank a small **"Safety Credit"**; at 3 credits, automatically prevent **one death drop** (once).
- **Constraints**: Credits reset on Game Over; max 3.
- **Exploit check**: Farm easy early sections.
- **Counter**: Credits only earned on sections with `difficultyStars ≥ 2` and unique section IDs (no repeats).

#### 10) Threshold Optimizer
- **Trigger condition**: Hit the XP threshold exactly within a narrow window (e.g., collect the "threshold pickup" with minimal overshoot, default: ±5 XP).
- **Payoff**: Reward selection UI gains **one reroll**.
- **Constraints**: Once per stage; UI shows "precision threshold" hint only after first success.
- **Exploit check**: Players leave one XP pickup and micro-manage forever.
- **Counter**: A soft timer: after **90s** in stage, this modifier disables for that stage.

#### 11) Biome Harmony
- **Trigger condition**: In mixed-biome stages (AB/BC/CA/ABC), clear a segment that uses both biome gimmicks without damage.
- **Payoff**: **Reward quality +1** OR +Flow (choose one based on stage profile, default: +Flow 0.10).
- **Constraints**: Once per segment; requires biome signature match.
- **Exploit check**: Farming in trivial mixed areas.
- **Counter**: Segment must be tagged `Twist` or `Finale` in the StagePlan ramp.

#### 12) Recovery Pact (Cursed)
- **Trigger condition**: Take damage.
- **Payoff**: Immediately grants a **temporary bonus** for 10s: increased Flow gains (not movement, default: +50% Flow gain rate).
- **Constraints**: Marked **Cursed**: cannot be moved once slotted until a Cleansing Station.
- **Exploit check**: Players intentionally take damage to farm boosted Flow.
- **Counter**: While buff active, **Flow decay increases** (default: +100% decay rate) and **reward multipliers cap** (default: max 1.2x instead of 1.5x) - net neutral unless you genuinely recover and finish clean.

## MVP Starter Set

For MVP, implement ~6 Tech Modifiers (3 FAST, 3 SLOW) that best reinforce intended player fantasies:
- **FAST**: Soft Landing, No-Stops Dividend, Clean Sprint Refund
- **SLOW**: Cartographer's Instinct, Courier's Pocket, Bonus Specialist

Mark remaining 6 as post-MVP.

