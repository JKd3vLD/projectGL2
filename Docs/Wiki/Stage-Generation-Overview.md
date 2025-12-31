# Stage Generation Overview [MVP Core]

**Purpose**: High-level overview of stage generation: from tier package to playable stage, including section assembly, flag placement, and level loading.

## Player-Facing Rules

- **Stage Selection**: Choose stage from tier package (0-6: Pure A/B/C, Mixed AB/BC/CA, Mastery ABC).
- **Pacing Choice**: Choose FAST or SLOW pacing (if stage supports both).
- **Stage Assembly**: Stage assembled from 2-6 handcrafted sections based on pacing choice.
- **Flag Placement**: Flags placed procedurally: start (beginning), middle (LONG sections), end (completion).

## System Rules

- **Tier Package Generation**: `WorldGenerator.GenerateTierPackage()` creates 7 stages from 3 biomes.
- **Stage Assembly**: `StageAssembler.AssembleStage()` filters sections, applies rules, enforces connectors.
- **Level Loading**: `LevelLoader.LoadStagePlan()` merges section JSON files into single playable level.
- **Flag Placement**: `FlagPlacer.PlaceFlags()` procedurally places flags based on section structure.

## Data Model

**TierPackage** → **Stage[7]** → **StagePlan** → **LevelData**

**Flow**:
1. `TierPackage` contains 7 `Stage` objects
2. Each `Stage` has `StagePlan` (assembled sections)
3. `StagePlan` contains ordered `SectionDef` list
4. `LevelLoader.LoadStagePlan()` merges section JSON files
5. Result: `LevelData` with merged blocks, entities, camera volumes

## Algorithms / Order of Operations

### Stage Generation Pipeline

1. **Tier Package Generation**: `WorldGenerator.GenerateTierPackage(tierIndex, worldGenStream)`
   - Select 3 biomes
   - Generate 7 stages with biome signatures
2. **Stage Assembly** (per stage): `StageAssembler.AssembleStage(tier, signature, pacing, rewardProfile)`
   - Filter sections by tier, signature, pacing
   - Select sections (FAST: 3-6, SLOW: 2-4 + side pockets)
   - Apply difficulty ramp
   - Enforce connector compatibility
   - Mark sections used
3. **Flag Placement**: `FlagPlacer.PlaceFlags(plan)`
   - Start flag: Beginning of first section
   - Middle flags: End of LONG sections (if 3+ sections)
   - End flag: End of last section
4. **Level Loading**: `LevelLoader.LoadStagePlan(plan)`
   - Load each section JSON file
   - Merge blocks with X-offset (section width * section index)
   - Merge entities, camera volumes
   - Update flag positions from section data
5. **Entity Creation**: Create player, flags, collectibles, enemies from level data

## Tuning Parameters

See [Section-Based Assembly](Section-Based-Assembly) and [FAST vs SLOW Stages](FAST-vs-SLOW-Stages) for detailed tuning.

## Edge Cases + Counters

- **No sections available**: Fall back to legacy stage selection or throw error.
- **Section JSON missing**: Skip section, continue with others. Log error.
- **Flag position not set**: Use section end position or default to (0,0).

## Telemetry Hooks

- Log tier package generation: `TierPackageGenerated(tierIndex, worldGenSeed, biomeIds, timestamp)`
- Log stage assembly: `StageAssembled(stageId, sectionCount, sidePocketCount, rampDistribution, timestamp)`
- Log level loading: `LevelLoaded(stageId, blockCount, entityCount, flagCount, timestamp)`

## Implementation Notes

**File**: `GL2Project/World/WorldGenerator.cs`, `GL2Project/World/StageAssembler.cs`, `GL2Project/Content/LevelLoader.cs`

**Key Systems**:
- `WorldGenerator`: Generates tier packages
- `StageAssembler`: Assembles stages from sections
- `LevelLoader`: Loads and merges section JSON files
- `FlagPlacer`: Places flags procedurally

**Deterministic Ordering**:
1. Resolve tier-scoped seed
2. Generate tier package
3. Assemble each stage
4. Place flags
5. Load level data
6. Create entities

**See Also**: [Section-Based Assembly](Section-Based-Assembly), [FAST vs SLOW Stages](FAST-vs-SLOW-Stages), [Flag System](Flag-System)

