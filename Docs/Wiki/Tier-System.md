# Tier System [MVP Core]

**Purpose**: Defines tier progression, tier package generation (7 stages from 3 biomes), and mastery unlock requirements.

## Player-Facing Rules

- **Tier Structure**: Each tier contains exactly 7 stages: Pure A, Pure B, Pure C, Mixed AB, Mixed BC, Mixed CA, Mastery ABC.
- **Biome Selection**: 3 biomes selected per tier (A, B, C). Biomes define available sections and visual theme.
- **Stage Order**: Stages can be played in any order. Mastery stage (ABC) requires completion of all 6 previous stages.
- **Mastery Requirements**: Each stage has mastery checklist: Letters (4 per stage), Artifacts (1 per stage), Key Pass (crafted from materials).
- **Tier Completion**: Complete all 7 stages + all mastery requirements → advance to next tier.

## System Rules

- **Tier Package Generation**: `WorldGenerator.GenerateTierPackage(tierIndex, worldGenStream)` creates 7 stages from 3 biomes.
- **Biome Selection**: Currently hardcoded. Future: Load from content packs, select based on tier difficulty curve.
- **Stage Assembly**: Stages assembled from sections via `StageAssembler`. Each stage has `StagePlan` with ordered sections.
- **Mastery Tracking**: `MasteryRequirements` struct tracks completion per stage. Stored in `Stage` object.
- **Tier-Scoped Seeds**: Seeds include tier index. Same symbol codes produce different results across tiers.

## Data Model

**TierPackage** (`GL2Project/World/TierPackage.cs`):
- `TierIndex`: int
- `Biomes`: Biome[3] - Selected biomes for this tier
- `Stages`: Stage[7] - Generated stages
- `WorldGenSeed`: ulong - Seed used for tier generation

**Biome** (`GL2Project/World/Biome.cs`):
- `Id`: string (e.g., "biome_a")
- `Name`: string
- `AvailableStages`: string[] - Stage IDs available in this biome (legacy, will be replaced by sections)
- `DifficultyMultiplier`: float
- `ThemeColor`: string

**Stage** (`GL2Project/World/Stage.cs`):
- `Id`: string (e.g., "pure_a", "mastery_abc")
- `PacingTag`: PacingTag (FAST/SLOW)
- `RewardProfile`: RewardProfile
- `StagePlan`: StagePlan - Assembled sections
- `MasteryRequirements`: MasteryRequirements

**MasteryRequirements** (`GL2Project/World/Stage.cs`):
- `LettersCollected`: bool[] - 4 letters per stage
- `ArtifactsCollected`: bool[] - 1 artifact per stage
- `KeyPassObtained`: bool - Crafted from materials

**BiomeSignature** (`GL2Project/World/TierPackage.cs`):
- `HasA`: bool
- `HasB`: bool
- `HasC`: bool

## Algorithms / Order of Operations

### Tier Package Generation

1. **Select 3 Biomes**: Currently hardcoded. Future: Filter biomes by tier range, select 3 via RNG.
2. **Generate WorldGen Seed**: `WorldGenSeed = (ulong)rng.Next() | ((ulong)rng.Next() << 32)`
3. **Generate 7 Stages**:
   - Stage 0: Pure A (`BiomeSignature { HasA = true }`)
   - Stage 1: Pure B (`BiomeSignature { HasB = true }`)
   - Stage 2: Pure C (`BiomeSignature { HasC = true }`)
   - Stage 3: Mixed AB (`BiomeSignature { HasA = true, HasB = true }`)
   - Stage 4: Mixed BC (`BiomeSignature { HasB = true, HasC = true }`)
   - Stage 5: Mixed CA (`BiomeSignature { HasC = true, HasA = true }`)
   - Stage 6: Mastery ABC (`BiomeSignature { HasA = true, HasB = true, HasC = true }`)
4. **Assemble Each Stage**: `StageAssembler.AssembleStage(tier, signature, pacing, rewardProfile)`
5. **Store Stage Plans**: Each `Stage` contains `StagePlan` with sections and flags

### Mastery Check

1. **Check Letters**: Verify all 4 letters collected in stage
2. **Check Artifacts**: Verify artifact collected in stage
3. **Check Key Pass**: Verify key pass crafted (requires letters + artifact + materials + quest completion)
4. **Check All Stages**: Verify all 7 stages have mastery complete
5. **Allow Tier Advance**: If all checks pass, unlock tier completion

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `lettersPerStage` | int | 1-10 | 4 | Letters required per stage |
| `artifactsPerStage` | int | 1-5 | 1 | Artifacts required per stage |
| `stagesPerTier` | int | - | 7 | Fixed: Pure A/B/C, Mixed AB/BC/CA, Mastery ABC |
| `biomesPerTier` | int | - | 3 | Fixed: Always 3 biomes per tier |

## Edge Cases + Counters

- **Biome pool exhausted**: Reset history, apply recolor/retexture to reused biomes.
- **No sections available**: Fall back to legacy stage selection from `Biome.AvailableStages`.
- **Mastery incomplete on tier completion attempt**: Show checklist UI, block advance.
- **Tier 1 mastery**: Still requires all checks. No special case.

## Telemetry Hooks

- Log tier package generation: `TierPackageGenerated(tierIndex, worldGenSeed, biomeIds, timestamp)`
- Log stage mastery progress: `MasteryProgress(stageId, lettersCollected, artifactCollected, keyPassObtained)`
- Log tier completion: `TierComplete(tierIndex, completionTime, timestamp)`

## Implementation Notes

**File**: `GL2Project/World/WorldGenerator.cs`, `GL2Project/World/TierPackage.cs`

**Key Systems**:
- `WorldGenerator`: Generates tier packages, selects biomes, creates stages
- `StageAssembler`: Assembles stages from sections based on tier, signature, pacing
- `SectionPool`: Filters sections by tier, biome signature, pacing tag

**Deterministic Ordering**:
1. Resolve tier-scoped seed
2. Initialize RNG streams
3. Select biomes (deterministic if seeded)
4. Generate stages in fixed order (0-6)
5. Assemble each stage with section pool

**Future Enhancements**:
- Biome selection from content packs
- Dynamic biome difficulty scaling
- Tier-specific biome themes
- Mastery reward system (beyond tier advance)

