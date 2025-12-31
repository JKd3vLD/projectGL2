# Section Format [MVP Core]

**Purpose**: Defines the JSON format for section definitions in mod packs. Used by `SectionLoader` to load sections from base game and mods.

## File Structure

**Location**: `/Mods/<PackName>/sections.json` or `/Sections/*.json`

**Format**: JSON array of section definitions.

## Data Model

**SectionDef JSON**:
```json
{
  "id": "fast_runline_01",
  "pacingTag": "FAST",
  "biomeTags": ["A"],
  "tierMin": 1,
  "tierMax": 5,
  "difficultyStars": 2,
  "lengthClass": "SHORT",
  "traversalMode": "RUNLINE",
  "interactionTags": ["Enemies", "Obstacles"],
  "connectorsIn": ["ground"],
  "connectorsOut": ["ground"],
  "quotas": {
    "secretSlots": 0,
    "bonusDoorSlots": 0,
    "chestSlots": 2
  },
  "levelDataPath": "Sections/fast_runline_01.json"
}
```

## Field Definitions

- **id**: string (required) - Unique section identifier
- **pacingTag**: string (required) - "FAST" or "SLOW"
- **biomeTags**: string[] (required) - Biome IDs (e.g., ["A", "B"])
- **tierMin**: int (required) - Minimum tier (1+)
- **tierMax**: int (required) - Maximum tier (tierMin+)
- **difficultyStars**: int (required) - Difficulty rating (1-5)
- **lengthClass**: string (required) - "SHORT", "MED", or "LONG"
- **traversalMode**: string (required) - "RUNLINE", "VERTICAL_ASCENT", "VERTICAL_DESCENT", "AUTOSCROLL", "VEHICLE", "CANNON_CHAIN", "OPEN_EXPLORATION"
- **interactionTags**: string[] (optional) - Interaction flags: "BarrelCannon", "TeamUpRequired", "CarryProp", "Ropes", "BoostPole", "Water", "RisingHazard", "PuzzleGate", "MinigamePortal", "BonusDoor", "Enemies", "Obstacles", "OpenExploration"
- **connectorsIn**: string[] (optional) - Connector types (e.g., ["ground"])
- **connectorsOut**: string[] (optional) - Connector types (e.g., ["ground"])
- **quotas**: object (optional) - Quotas for secrets, bonus doors, chests
- **levelDataPath**: string (required) - Path to section JSON file (relative to pack directory)

## Validation Rules

- **id**: Must be unique across all packs. Later packs override earlier ones.
- **tierMin/tierMax**: Must satisfy `tierMin <= tierMax`, `tierMin >= 1`.
- **difficultyStars**: Must be 1-5.
- **lengthClass**: Must be "SHORT", "MED", or "LONG".
- **traversalMode**: Must be valid enum value.
- **levelDataPath**: Must exist relative to pack directory or be absolute path.

## Example

See `GL2Project/Mods/ExamplePack/sections.json` for complete example with multiple sections.

## Implementation Notes

**File**: `GL2Project/Content/SectionLoader.cs`

**Key Systems**:
- `SectionLoader`: Loads and parses section JSON files
- `SectionPool`: Uses loaded sections for stage assembly

**Deterministic Ordering**:
1. Load base sections (`/Sections/*.json`)
2. Load mod sections (`/Mods/<Pack>/sections.json`, alphabetical order)
3. Sort all sections by ID (deterministic)
4. Return merged list

**Path Resolution**: `levelDataPath` resolved relative to pack directory if not absolute. Base sections use relative paths from `/Sections/`.

