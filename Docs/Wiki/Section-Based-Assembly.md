# Section-Based Assembly [MVP Core]

**Purpose**: Defines how handcrafted sections are assembled into stages, including section pool filtering, history-based anti-repeat, connector compatibility, and difficulty ramp assignment.

## Player-Facing Rules

- **Stages from Sections**: Each stage is assembled from 2-6 handcrafted sections.
- **Section Variety**: History system prevents section repeats until pool is exhausted. Exhausted pools reset with recolor/retexture.
- **Connector Compatibility**: Sections chain together via compatible connectors (e.g., "ground" → "ground").
- **Difficulty Ramp**: Stages follow Teach→Test→Twist→Finale structure. First section introduces mechanics, last section is peak difficulty.

## System Rules

- **Section Pool**: `SectionPool` filters sections by tier, biome signature, and pacing tag. Maintains history per (tier, signature, pacing) key.
- **History Reset**: When pool exhausted, history clears and sections reused with recolor/retexture flag.
- **Connector Matching**: `StageAssembler.EnforceConnectorCompatibility()` ensures adjacent sections have compatible connectors. Defaults to "ground" if missing.
- **Difficulty Assignment**: `StageAssembler.ApplyDifficultyRamp()` assigns ramp positions based on section count and difficulty stars.
- **No Consecutive High Stars**: System avoids two consecutive 5-star sections (constraint in selection, not enforced post-assembly).

## Data Model

**SectionDef** (`GL2Project/World/SectionDef.cs`):
- `Id`: string
- `PacingTag`: PacingTag (FAST/SLOW)
- `BiomeTags`: List<string> (e.g., ["A", "B"])
- `TierMin`: int
- `TierMax`: int
- `DifficultyStars`: int (1-5)
- `LengthClass`: LengthClass (SHORT/MED/LONG)
- `TraversalMode`: TraversalMode (RUNLINE, VERTICAL_ASCENT, etc.)
- `InteractionTags`: InteractionTags (bitset: BarrelCannon, TeamUpRequired, etc.)
- `ConnectorsIn`: List<string> (e.g., ["ground"])
- `ConnectorsOut`: List<string> (e.g., ["ground"])
- `Quotas`: SectionQuotas (secretSlots, bonusDoorSlots, chestSlots)
- `LevelDataPath`: string (path to section JSON)

**SectionPool** (`GL2Project/World/SectionPool.cs`):
- `_allSections`: List<SectionDef>
- `_historyLists`: Dictionary<string, List<string>> (key: "tier_signature_pacing")

**StagePlan** (`GL2Project/World/StagePlan.cs`):
- `PacingTag`: PacingTag
- `Sections`: List<SectionDef> (ordered)
- `SidePockets`: List<SectionDef> (SLOW only)
- `Flags`: List<FlagPosition>
- `RewardProfile`: RewardProfile
- `DifficultyRamp`: List<DifficultyRamp> (Teach/Test/Twist/Finale per section)

## Algorithms / Order of Operations

### Stage Assembly

1. **Check Pool Exhaustion**: `SectionPool.ResetIfExhausted(tier, signature, pacing)` - If exhausted, clear history, set recolor flag
2. **Get Available Sections**: `SectionPool.GetAvailableSections(tier, signature, pacing)` - Filter by tier range, biome match, pacing tag, exclude history
3. **Select Sections** (FAST):
   - Count: `rng.NextInt(3, 7)` (3-6 sections)
   - Filter by length: Prefer SHORT/MED, fallback to any
   - Select with variety: Track used IDs, avoid duplicates
4. **Select Sections** (SLOW):
   - Count: `rng.NextInt(2, 5)` (2-4 sections)
   - Filter by length: Prefer MED/LONG, fallback to any
   - Select side pockets: Filter by interaction tags (BonusDoor, MinigamePortal, OpenExploration), select 1-3
5. **Apply Difficulty Ramp**: Assign ramp positions based on section count:
   - Progress = `i / (count - 1)`
   - < 0.25: Teach
   - < 0.5: Test
   - < 0.75: Twist
   - ≥ 0.75: Finale
6. **Enforce Connector Compatibility**: For each adjacent pair:
   - Check if `current.ConnectorsOut` matches `next.ConnectorsIn`
   - If no match and connectors exist: Add default "ground" connector
7. **Mark Sections Used**: `SectionPool.MarkUsed(tier, signature, pacing, sectionId)` - Add to history
8. **Place Flags**: `FlagPlacer.PlaceFlags(plan)` - Start, middle (if LONG sections), end

### Section Filtering

1. **Tier Range**: `section.TierMin <= tier && section.TierMax >= tier`
2. **Biome Match**: `section.BiomeTags` contains at least one biome from `signature`
3. **Pacing Match**: `section.PacingTag == pacing`
4. **History Exclusion**: `!historyList.Contains(section.Id)`

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `fastStage.minSections` | int | 2-10 | 3 | Minimum sections for FAST |
| `fastStage.maxSections` | int | 3-10 | 6 | Maximum sections for FAST |
| `slowStage.minSections` | int | 1-5 | 2 | Minimum sections for SLOW |
| `slowStage.maxSections` | int | 2-6 | 4 | Maximum sections for SLOW |
| `slowStage.maxSidePockets` | int | 0-5 | 3 | Maximum side pockets for SLOW |
| `difficultyRamp.teachStars` | int[] | - | [1] | Star ratings for Teach |
| `difficultyRamp.testStars` | int[] | - | [2, 3] | Star ratings for Test |
| `difficultyRamp.twistStars` | int[] | - | [3, 4] | Star ratings for Twist |
| `difficultyRamp.finaleStars` | int[] | - | [4, 5] | Star ratings for Finale |

## Edge Cases + Counters

- **Pool exhausted**: Clear history, apply recolor/retexture flag. Continue assembly.
- **No sections available**: Fall back to legacy stage selection or throw error.
- **Incompatible connectors**: Add default "ground" connector to both sections.
- **Single section stage**: Assign Finale ramp position (peak difficulty).
- **Side pocket selection empty**: SLOW stage continues without side pockets.

## Telemetry Hooks

- Log section selection: `SectionSelected(sectionId, tier, signature, pacing, rampPosition)`
- Log pool exhaustion: `SectionPoolExhausted(tier, signature, pacing, timestamp)`
- Log connector compatibility: `ConnectorMismatch(sectionId1, sectionId2, connectorsFixed)`
- Log stage assembly: `StageAssembled(stageId, sectionCount, sidePocketCount, rampDistribution)`

## Implementation Notes

**File**: `GL2Project/World/StageAssembler.cs`, `GL2Project/World/SectionPool.cs`

**Key Systems**:
- `StageAssembler`: Main assembly logic, section selection, ramp assignment, connector enforcement
- `SectionPool`: Section filtering, history management, pool exhaustion detection
- `SectionLoader`: Loads sections from base game and mod packs

**Deterministic Ordering**:
1. Load all sections (base + mods, alphabetical order)
2. Filter by tier, signature, pacing
3. Select sections with RNG (deterministic per seed)
4. Apply difficulty ramp
5. Enforce connectors
6. Mark used, place flags

**Tuning File**: `GL2Project/Tuning/StageGenerationTuning.json`

**Content Format**: `GL2Project/Mods/ExamplePack/sections.json` - Array of section definitions.

