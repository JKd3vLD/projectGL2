# Rule Modifiers System [Later]

**Purpose**: Support accessibility, self-imposed challenges, and "locked option" variants using one clean system with consistent rewards.

## Player-Facing Rules

- Rule Modifiers are toggles that change the rules of a run or stage:
  - **Assists:** safety-oriented (extra flags, reduced penalties)
  - **Challenges:** constraints (no jump, no glide, one-button lockout)
- Modifiers never block progression; they only affect **reward multipliers** and **eligible content pools**.
- Player-facing: Show as "Assist Options" and "Challenge Options" in hub/stage selection UI.

## System Rules

- Each modifier has:
  - `Eligibility` (can it be used in this tier/stage?)
  - `RewardMultiplier` (>=1 for challenges, <=1 for assists)
  - `DisableList` (which actions are restricted)
- Stage assembler can tag stages that **conflict** with selected modifiers (avoid unwinnable generation).
- Modifiers are **run-scoped** or **stage-scoped** (specified per modifier).
- Input system applies `DisabledInputs` as hard gates (input blocked, not just penalized).

## Data Model

**RuleModifierDef** (`GL2Project/Content/ModLoader.cs` or new file):
- `Id`: string
- `Name`: string (player-facing)
- `Scope`: ModifierScope enum (Run|Stage)
- `Flags`: ModifierFlags enum (Assist|Challenge, bitmask)
- `DisabledInputs`: InputBitset (bitset of disabled input actions)
- `RewardMultiplier`: float (>=1 for challenges, <=1 for assists)
- `PoolFilters`: ModifierPoolFilters struct (allowed/disallowed InteractionTags, TraversalModes)

**ActiveModifiersComponent** (`GL2Project/ECS/Components.cs`):
- `ModifierIds`: int[] (fixed-size array, max 10)
- `ModifierCount`: int (active count)
- `RewardMultiplier`: float (aggregate multiplier)

**ModifierSystem** (`GL2Project/Gameplay/ModifierSystem.cs`):
- `_world`: GameWorld
- `_activeModifiers`: List<RuleModifierDef>
- `ApplyModifiers(stagePlan)`: StagePlan - Filters sections, validates compatibility
- `CheckInput(inputAction)`: bool - Returns true if input is allowed

## Algorithms / Order of Operations

### Modifier Selection

1. **Player Opens Hub/Stage Selection**: Show "Assist Options" and "Challenge Options" UI
2. **Player Selects Modifiers**: Toggle modifiers on/off (max `ModifierMaxActive` active)
3. **Validate Compatibility**: Check modifier pairs for incompatibilities (e.g., "No Jump" + "No Glide" = unwinnable)
4. **Store Selection**: Save to `ActiveModifiersComponent` on player entity

### Stage Assembly Filtering

1. **StageAssembler.AssembleStage()**: Called with `ActiveModifiersComponent`
2. **Filter Sections**: For each candidate section:
   - Check `PoolFilters`: Does section require disabled `InteractionTags` or `TraversalModes`?
   - If yes: Skip section (prevents unwinnable generation)
3. **Validate Stage Plan**: After assembly, verify stage is winnable with active modifiers
4. **Fallback**: If no valid sections found, show warning, allow player to adjust modifiers

### Input Application

1. **Input System**: `PlayerControllerSystem` checks input before processing
2. **ModifierSystem.CheckInput()**: For each active modifier:
   - Check if input action is in `DisabledInputs` bitset
   - If disabled: Return false (hard gate, input ignored)
3. **Input Processed**: Only if `CheckInput()` returns true

### Reward Multiplier Application

1. **At Stage End**: `RewardSystem` reads `ActiveModifiersComponent.RewardMultiplier`
2. **Calculate Aggregate**: Multiply all active modifier multipliers (with cap)
3. **Apply to Rewards**: Multiply stage completion rewards by aggregate multiplier
4. **UI Display**: Show multiplier in stage completion UI (e.g., "1.25x Challenge Bonus")

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `AssistRewardMultiplier` | float | 0.1–1.0 | 0.75 | Baseline assist multiplier |
| `ChallengeRewardMultiplier` | float | 1.0–3.0 | 1.25 | Baseline challenge multiplier |
| `ModifierMaxActive` | int | 0–10 | 3 | MVP max active modifiers |
| `MaxAggregateMultiplier` | float | 1.0–5.0 | 2.0 | Cap total multiplier (prevents stacking exploits) |

## Edge Cases + Counters

- **Challenge stacking exploits**: Cap total multiplier at `MaxAggregateMultiplier`. Mark incompatible modifier pairs.
- **Unwinnable seeds**: StageAssembler must reject sections requiring disabled inputs. Show warning if no valid sections.
- **Modifier conflicts**: Define incompatible pairs (e.g., "No Jump" + "No Glide" = incompatible). Prevent selection.
- **Stage-scoped modifiers**: Reset on stage exit. Run-scoped modifiers persist until game over.

## Telemetry Hooks

- Log modifier selection: `ModifierSelected(modifierId, scope, flags, timestamp)`
- Log completion rates: `StageCompleteWithModifiers(stageId, modifierIds[], rewardMultiplier, timestamp)`
- Log stage failures: `StageFailedWithModifiers(stageId, modifierIds[], failureReason, timestamp)`
- Log modifier conflicts: `ModifierConflictDetected(modifierId1, modifierId2, timestamp)`

## Implementation Notes

**File**: `GL2Project/Gameplay/ModifierSystem.cs` (to be created)

**Key Systems**:
- `ModifierSystem`: Manages active modifiers, validates compatibility, filters stages
- `StageAssembler`: Uses modifier filters to exclude incompatible sections
- `PlayerControllerSystem`: Checks modifier input restrictions
- `RewardSystem`: Applies modifier reward multipliers

**Deterministic Ordering**:
1. Player selects modifiers at hub/stage selection
2. `ModifierSystem` validates compatibility
3. `StageAssembler` filters sections using modifier `PoolFilters`
4. Input system applies `DisabledInputs` (hard gates)
5. Rewards apply multiplier at stage completion

**Component Stores**: `GameWorld.ActiveModifiers` (to be added)

**Bitset Implementation**: Use `InputBitset` (ulong or int array) for efficient input checking. Not strings in hot path.

**Tuning File**: `GL2Project/Tuning/ModifierTuning.json` (to be created)

**UI Components**: `ModifierSelectionUI` - Shows assist/challenge options, validates compatibility, displays warnings.

## Example Modifiers

### Assist Modifiers

- **Extra Flags**: Spawn additional middle flags (default: +1 flag per 3 sections). Multiplier: 0.9x
- **Reduced Penalty**: Death drop penalty reduced (default: 25% instead of 50%). Multiplier: 0.85x
- **Longer Coyote Time**: Jump buffer window increased (default: +0.1s). Multiplier: 0.95x

### Challenge Modifiers

- **No Jump**: Jump input disabled. Multiplier: 1.5x
- **No Glide**: Glide input disabled. Multiplier: 1.3x
- **One-Button**: Only one movement direction allowed at a time. Multiplier: 1.4x
- **Speedrun Mode**: Timer visible, no checkpoints (flags disabled). Multiplier: 1.6x

