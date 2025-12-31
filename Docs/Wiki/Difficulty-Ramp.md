# Difficulty Ramp [MVP Core]

**Purpose**: Kishotenketsu structure (Teach→Test→Twist→Finale) for stage difficulty progression. Assigns sections to ramp positions based on section count and difficulty stars.

## Player-Facing Rules

- **Difficulty Progression**: Stages follow Teach→Test→Twist→Finale structure. First section introduces mechanics, last section is peak difficulty.
- **Star Ratings**: Sections rated 1-5 stars. Teach sections are 1-star, Finale sections are 4-5 stars.

## System Rules

- **Ramp Assignment**: `StageAssembler.ApplyDifficultyRamp()` assigns ramp positions based on section count and progress.
- **Progress Calculation**: `progress = i / (sectionCount - 1)` where `i` is section index.
- **Ramp Mapping**:
  - Progress < 0.25: Teach
  - Progress < 0.5: Test
  - Progress < 0.75: Twist
  - Progress ≥ 0.75: Finale
- **Star Constraints**: System avoids two consecutive 5-star sections (constraint in selection, not enforced post-assembly).

## Data Model

**DifficultyRamp** (`GL2Project/World/StagePlan.cs`):
- `Teach`: 1-star, introduces mechanics
- `Test`: 2-3 star, standard challenge
- `Twist`: 3-4 star, introduces variation
- `Finale`: 4-5 star, peak difficulty

**StagePlan** (`GL2Project/World/StagePlan.cs`):
- `DifficultyRamp`: List<DifficultyRamp> (one per section)

**Tuning** (`GL2Project/Tuning/StageGenerationTuning.json`):
- `difficultyRamp.teachStars`: int[] (default: [1])
- `difficultyRamp.testStars`: int[] (default: [2, 3])
- `difficultyRamp.twistStars`: int[] (default: [3, 4])
- `difficultyRamp.finaleStars`: int[] (default: [4, 5])

## Algorithms / Order of Operations

### Ramp Assignment

1. **Calculate Progress**: For each section `i` in `sections`:
   - `progress = (float)i / Math.Max(1, sections.Count - 1)`
2. **Assign Ramp**:
   - If `progress < 0.25f`: `ramp = Teach`
   - Else if `progress < 0.5f`: `ramp = Test`
   - Else if `progress < 0.75f`: `ramp = Twist`
   - Else: `ramp = Finale`
3. **Store in Plan**: `plan.DifficultyRamp.Add(ramp)`

### Star Constraint (Selection Phase)

1. **Check Consecutive**: After section selection, check if two consecutive sections are 5-star
2. **Avoid if Possible**: Prefer lower-star sections to avoid consecutive 5-star (not enforced, preference only)

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `teachStars` | int[] | - | [1] | Star ratings for Teach |
| `testStars` | int[] | - | [2, 3] | Star ratings for Test |
| `twistStars` | int[] | - | [3, 4] | Star ratings for Twist |
| `finaleStars` | int[] | - | [4, 5] | Star ratings for Finale |
| `maxConsecutiveHighStars` | int | 1-3 | 1 | Max consecutive 5-star sections |

## Edge Cases + Counters

- **Single section stage**: Assign Finale ramp position (peak difficulty).
- **Two sections**: First = Teach, Second = Finale.
- **Three sections**: First = Teach, Second = Test, Third = Finale.
- **Consecutive 5-star**: Preference to avoid, but not enforced. Acceptable if pool exhausted.

## Telemetry Hooks

- Log ramp assignment: `DifficultyRampAssigned(sectionId, rampPosition, difficultyStars, progress, timestamp)`
- Log ramp distribution: `RampDistribution(stageId, teachCount, testCount, twistCount, finaleCount, timestamp)`

## Implementation Notes

**File**: `GL2Project/World/StageAssembler.cs`

**Key Systems**:
- `StageAssembler.ApplyDifficultyRamp()`: Assigns ramp positions to sections

**Deterministic Ordering**:
1. Select sections
2. Apply difficulty ramp
3. Store in stage plan

**Tuning File**: `GL2Project/Tuning/StageGenerationTuning.json` (difficultyRamp section)

**Future Enhancements**:
- Dynamic difficulty adjustment based on player performance
- Ramp validation (ensure proper progression)
- Custom ramp curves per stage type

