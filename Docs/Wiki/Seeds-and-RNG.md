# Seeds & RNG [MVP Core]

**Purpose**: Tier-scoped seed system with separate RNG streams for different game systems. Ensures deterministic generation while keeping layout and rewards independent.

## Player-Facing Rules

- **Seed Selection**: When advancing to a new tier, player selects symbol codes (e.g., "ABC123"). Same codes in different tiers produce different results.
- **Seed Badge**: Mastery trophy stores seed combination for that tier. Allows replaying exact tier layout.
- **Deterministic Runs**: Same tier + seed codes = same world layout + same loot (deterministic).
- **Random Seeds**: On game over, new random seeds generated unless seed locks explicitly set.

## System Rules

- **Tier-Scoped Seeds**: `SeedResolver.ResolveSeed(hashVersion, tierIndex, categoryId, symbolIds[])` includes tier index in hash. Same codes in Tier 1 ≠ Tier 2.
- **Separate RNG Streams**: `RngStreams` provides 4 independent streams:
  - `WorldGen`: Layout generation, section selection, biome selection
  - `Reward`: Loot table rolls, reward selection
  - `Bonus`: Bonus door rewards, minigame rewards
  - `Collectible`: XP collectible placement, pickup spawns
- **Stream Independence**: Each stream uses `baseSeed + streamOffset` to ensure independence. Layout never consumes reward RNG.
- **Deterministic Generation**: Same seed + same stream = same sequence of random numbers.

## Data Model

**RngStreams** (`GL2Project/Engine/RngStreams.cs`):
- `WorldGen`: Rng
- `Reward`: Rng
- `Bonus`: Rng
- `Collectible`: Rng

**SeedResolver** (`GL2Project/Engine/SeedResolver.cs`):
- `ResolveSeed(hashVersion, tierIndex, categoryId, symbolIds[])`: ulong

**Rng** (`GL2Project/Engine/Rng.cs`):
- `Next()`: int
- `NextInt(min, max)`: int
- `NextFloat()`: float
- `NextDouble()`: double

**SeedBadge** (`GL2Project/World/TierMastery.cs`):
- `TierIndex`: int
- `SymbolCodes`: string[]
- `ResolvedSeed`: ulong

## Algorithms / Order of Operations

### Seed Resolution

1. **Player Selects Codes**: Enter symbol codes (e.g., "ABC123") via `SeedSelectionUI`
2. **Resolve Seed**: `SeedResolver.ResolveSeed(hashVersion, tierIndex, categoryId, symbolIds)`
   - Hash function: `HashCode.Combine(hashVersion, tierIndex, categoryId, symbolIds)`
   - Convert to ulong: `(ulong)hash | ((ulong)hash << 32)`
3. **Store Seed Badge**: Save to `TierMastery.SeedBadge` for replay

### RNG Stream Initialization

1. **Base Seed**: Resolved seed from symbol codes (or random on game over)
2. **Create Streams**: `RngStreams(baseSeed)`
   - `WorldGen = new Rng(baseSeed + 0x100000000UL)`
   - `Reward = new Rng(baseSeed + 0x200000000UL)`
   - `Bonus = new Rng(baseSeed + 0x300000000UL)`
   - `Collectible = new Rng(baseSeed + 0x400000000UL)`
3. **Store in GameWorld**: `GameWorld.RngStreams = rngStreams`

### World Generation

1. **Use WorldGen Stream**: `WorldGenerator.GenerateTierPackage(tierIndex, rngStreams.WorldGen)`
2. **Generate Stages**: Each stage uses WorldGen stream for:
   - Section selection
   - Biome selection (future)
   - Flag placement randomization
   - Side pocket selection
3. **Never Use Reward Stream**: Layout generation never touches reward RNG

### Reward Generation

1. **Use Reward Stream**: `LootTable.RollLoot(rngStreams.Reward, count)`
2. **Reward Selection**: `RewardSelectionUI` uses Reward stream for 3 options
3. **Never Use WorldGen Stream**: Reward generation never touches layout RNG

### Collectible Placement

1. **Use Collectible Stream**: `CollectibleSystem` uses Collectible stream for:
   - XP collectible spawn positions
   - Pickup item spawns
   - Secret placement
2. **Deterministic Per Seed**: Same seed = same collectible positions

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `hashVersion` | int | 1+ | 1 | Seed hash algorithm version |
| `streamOffsetWorldGen` | ulong | - | 0x100000000UL | WorldGen stream offset |
| `streamOffsetReward` | ulong | - | 0x200000000UL | Reward stream offset |
| `streamOffsetBonus` | ulong | - | 0x300000000UL | Bonus stream offset |
| `streamOffsetCollectible` | ulong | - | 0x400000000UL | Collectible stream offset |

## Edge Cases + Counters

- **Tier-scoped collision**: Same codes in Tier 1 and Tier 2 produce different seeds (tier index in hash).
- **Stream exhaustion**: RNG streams are 32-bit, extremely unlikely to exhaust. If needed, reseed with new base.
- **Seed locks**: If seed locks set, use locked seeds instead of random on game over.
- **Invalid symbol codes**: Validate codes, reject invalid characters. Default to random if invalid.

## Telemetry Hooks

- Log seed resolution: `SeedResolved(tierIndex, symbolCodes, resolvedSeed, timestamp)`
- Log stream usage: `RNGStreamUsed(streamName, usageCount, timestamp)` (optional, for debugging)
- Log seed badge save: `SeedBadgeSaved(tierIndex, seedBadge, timestamp)`
- Log random seed generation: `RandomSeedGenerated(tierIndex, seed, timestamp)`

## Implementation Notes

**File**: `GL2Project/Engine/RngStreams.cs`, `GL2Project/Engine/SeedResolver.cs`, `GL2Project/Engine/Rng.cs`

**Key Systems**:
- `SeedResolver`: Resolves tier-scoped seeds from symbol codes
- `RngStreams`: Manages 4 independent RNG streams
- `Rng`: Deterministic random number generator (Xorshift or similar)

**Deterministic Ordering**:
1. Seed resolution (tier-scoped)
2. Stream initialization (base seed + offsets)
3. World generation (WorldGen stream only)
4. Reward generation (Reward stream only)
5. Collectible placement (Collectible stream only)

**Stream Offsets**: Large offsets (0x100000000UL = 2^32) ensure streams never overlap even with many random numbers.

**Seed Persistence**: Seed badges stored in `TierMastery` per tier. Allows replaying exact tier layouts.

**UI Component**: `GL2Project/UI/SeedSelectionUI.cs` - Placeholder for seed code input UI.

