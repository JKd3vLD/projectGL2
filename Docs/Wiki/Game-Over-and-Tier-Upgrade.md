# Game Over & Tier Upgrade [MVP Core]

**Purpose**: Defines game over behavior (tier regression, seed generation, lives reset) and tier upgrade flow (seed selection, mastery trophy, tier advance).

## Player-Facing Rules

- **Game Over Trigger**: Lives reach 0 or explicit game over event (e.g., fall into void).
- **Tier Regression**: On game over, regress to `max(1, HighestTierReached - 3)`. Minimum tier 1.
- **Lives Reset**: Lives reset to 3 on game over.
- **Seed Generation**: New random seeds generated for regressed tier (unless seed locks set).
- **Tier Upgrade**: Complete all 7 stages + mastery requirements → advance to next tier.
- **Seed Selection**: When advancing tier, player selects symbol codes for new tier.
- **Mastery Trophy**: Stores seed badge for completed tier. Allows replaying exact tier layout.

## System Rules

- **Game Over Logic**: `GameOverSystem.HandleGameOver()` applies keep/lose mapping, calculates new tier, resets lives, generates seeds.
- **Keep/Lose Mapping**: Stub for now. Determines what persists across runs (future: items, currency, etc.).
- **Tier Calculation**: `TierStart = max(1, HighestTierReached - 3)`. Never below tier 1.
- **Seed Generation**: If seed locks not set, generate random seeds. Otherwise use locked seeds.
- **Mastery Check**: Verify all `MasteryRequirements` complete for all 7 stages before tier advance.
- **Seed Badge Storage**: `TierMastery.SeedBadge` stores seed combination for replay.

## Data Model

**SaveData** (`GL2Project/Gameplay/SaveData.cs`):
- `HighestTierReached`: int
- `CurrentTier`: int
- `Lives`: int
- `TierStart`: int (starting tier for current run)

**TierMastery** (`GL2Project/World/TierMastery.cs`):
- `TierIndex`: int
- `SeedBadge`: SeedBadge
- `CompletionTime`: DateTime (optional)

**SeedBadge** (`GL2Project/World/TierMastery.cs`):
- `TierIndex`: int
- `SymbolCodes`: string[]
- `ResolvedSeed`: ulong

**GameOverSystem** (`GL2Project/Gameplay/GameOverSystem.cs`):
- `_world`: GameWorld
- `_keepLoseMapping`: Dictionary<string, bool> (stub)

## Algorithms / Order of Operations

### Game Over

1. **Trigger**: `GameOverSystem.HandleGameOver()` called (lives = 0 or explicit event)
2. **Apply Keep/Lose Mapping**: Determine what persists (stub: nothing persists for now)
3. **Calculate New Tier**: `TierStart = max(1, HighestTierReached - 3)`
4. **Reset Lives**: `Lives = 3`
5. **Generate Seeds**: If seed locks not set:
   - Generate random symbol codes
   - Resolve seed: `SeedResolver.ResolveSeed(hashVersion, tierStart, categoryId, randomCodes)`
   - Initialize RNG streams: `RngStreams(resolvedSeed)`
6. **Save State**: Write `HighestTierReached`, `TierStart`, `Lives` to save file
7. **Return to Tier Selection**: Show tier selection UI with new tier start

### Tier Upgrade

1. **Check Mastery**: Verify all 7 stages have `MasteryRequirements.IsComplete() == true`
2. **Calculate Rewards**: `FastRewardCalculator` or `SlowRewardCalculator` based on stage pacing
3. **Store Seed Badge**: `TierMastery.SeedBadge = currentSeedBadge` - Save seed combination
4. **Advance Tier**: `CurrentTier++`, `HighestTierReached = max(HighestTierReached, CurrentTier)`
5. **Seed Selection UI**: `SeedSelectionUI.Show()` - Player selects symbol codes for new tier
6. **Resolve Seed**: `SeedResolver.ResolveSeed(hashVersion, newTier, categoryId, symbolCodes)`
7. **Generate Tier Package**: `WorldGenerator.GenerateTierPackage(newTier, worldGenStream)`
8. **Continue Run**: Start first stage of new tier

### Mastery Check

1. **Iterate Stages**: Check all 7 stages in tier package
2. **Check Letters**: Verify `stage.MasteryRequirements.LettersCollected` all true (4 per stage)
3. **Check Artifacts**: Verify `stage.MasteryRequirements.ArtifactsCollected` all true (1 per stage)
4. **Check Key Pass**: Verify `stage.MasteryRequirements.KeyPassObtained == true` (all stages)
5. **All Complete**: If all checks pass, allow tier advance

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `initialLives` | int | 1-10 | 3 | Lives at run start |
| `tierRegressionAmount` | int | 1-5 | 3 | Tiers to regress on game over |
| `minTierStart` | int | 1+ | 1 | Minimum starting tier |

## Edge Cases + Counters

- **HighestTierReached < 3**: Regression to tier 1 (never below 1).
- **HighestTierReached = 0**: First run, start at tier 1.
- **Seed locks set**: Use locked seeds instead of random generation.
- **Mastery incomplete**: Block tier advance, show mastery checklist UI.
- **Tier 1 completion**: Still requires mastery check, no special case.

## Telemetry Hooks

- Log game over: `GameOver(reason, tierReached, livesRemaining, timestamp)`
- Log tier regression: `TierRegression(fromTier, toTier, timestamp)`
- Log seed generation: `RandomSeedGenerated(tierIndex, seed, timestamp)`
- Log tier upgrade: `TierUpgrade(fromTier, toTier, seedBadge, timestamp)`
- Log mastery check: `MasteryCheck(tierIndex, allComplete, missingRequirements, timestamp)`

## Implementation Notes

**File**: `GL2Project/Gameplay/GameOverSystem.cs`, `GL2Project/World/TierMastery.cs`

**Key Systems**:
- `GameOverSystem`: Handles game over logic, tier regression, seed generation
- `SeedResolver`: Resolves tier-scoped seeds
- `WorldGenerator`: Generates tier packages for new tier

**Deterministic Ordering**:
1. Game over trigger
2. Keep/lose mapping (stub)
3. Tier calculation
4. Seed generation (if not locked)
5. Save state
6. Return to tier selection

**Save System**: `SaveData` serialized to JSON. Location: TBD (likely `%AppData%/GL2Engine/save.json`).

**Mastery Persistence**: `TierMastery` stored per tier. Allows checking completion status and replaying with seed badge.

**UI Components**:
- `SeedSelectionUI`: Placeholder for seed code input
- Mastery checklist UI: Future implementation

