# Mod Packs & Content Loading [MVP Core]

**Purpose**: JSON-based content pack system for sections, items, blocks, biomes, rooms. Deterministic merge ordering ensures stable content database.

## Player-Facing Rules

- **Mod Packs**: Place content packs in `/Mods/<PackName>/` directory. Pack contains JSON files: `sections.json`, `items.json`, `blocks.json`, `biomes.json`, `rooms.json`.
- **Load Order**: Packs loaded in alphabetical order. Later packs override earlier ones (deterministic merge).
- **Section Discovery**: Sections from mod packs appear in stage generation automatically. No manual activation needed.

## System Rules

- **Mod Loader**: `ModLoader.LoadAllPacks()` scans `/Mods/` directory, loads packs alphabetically.
- **Section Loader**: `SectionLoader.LoadAllSections()` loads sections from base game (`/Sections/`) and all mod packs (`/Mods/<Pack>/sections.json`).
- **Deterministic Merge**: Content merged in alphabetical order. Later packs override earlier ones (by ID).
- **Content Database**: `ContentDatabase` merges all packs into single database. Items/blocks/biomes keyed by ID.

## Data Model

**ContentPack** (`GL2Project/Content/ContentPack.cs`):
- `Name`: string
- `Version`: string
- `Items`: List<ItemDef>
- `Blocks`: List<BlockDefinition>
- `Biomes`: List<Biome>
- `Rooms`: List<RoomDef>

**ModLoader** (`GL2Project/Content/ModLoader.cs`):
- `_loadedPacks`: List<ContentPack>
- `LoadAllPacks(modsDirectory)`: void
- `GetLoadedPacks()`: List<ContentPack>
- `MergePacks()`: ContentDatabase

**SectionLoader** (`GL2Project/Content/SectionLoader.cs`):
- `LoadAllSections(modLoader, modsDirectory)`: List<SectionDef>

**ContentDatabase** (`GL2Project/Content/ContentPack.cs`):
- `Items`: Dictionary<string, ItemDef>
- `Blocks`: Dictionary<string, BlockDefinition>
- `Biomes`: Dictionary<string, Biome>
- `Rooms`: Dictionary<string, RoomDef>

## Algorithms / Order of Operations

### Mod Pack Loading

1. **Scan Directory**: `Directory.GetDirectories(modsDirectory)` - Get all pack directories
2. **Sort Alphabetically**: `OrderBy(d => Path.GetFileName(d))` - Deterministic order
3. **Load Each Pack**:
   - Read `items.json` → `pack.Items`
   - Read `blocks.json` → `pack.Blocks`
   - Read `biomes.json` → `pack.Biomes`
   - Read `rooms.json` → `pack.Rooms`
   - Read `sections.json` → loaded separately by `SectionLoader`
4. **Store Pack**: Add to `_loadedPacks` list

### Section Loading

1. **Load Base Sections**: `LoadSectionsFromDirectory("Sections/")` - Load all `*.json` files
2. **Load Mod Sections**: For each pack in `modLoader.GetLoadedPacks()`:
   - Read `sections.json` (if exists)
   - Parse array of `SectionDef` JSON objects
   - Convert to `SectionDef` structs
3. **Sort by ID**: `allSections.Sort((a, b) => Compare(a.Id, b.Id))` - Deterministic order
4. **Return List**: Return all sections for `SectionPool` initialization

### Content Merge

1. **Initialize Database**: Create empty `ContentDatabase` with dictionaries
2. **Iterate Packs**: For each pack in `_loadedPacks` (alphabetical order):
   - Merge items: `db.Items[item.Id] = item` (later overrides earlier)
   - Merge blocks: `db.Blocks[block.Id] = block`
   - Merge biomes: `db.Biomes[biome.Id] = biome`
   - Merge rooms: `db.Rooms[room.Id] = room`
3. **Return Database**: Single merged database with all content

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `modsDirectory` | string | - | "GL2Project/Mods" | Mod packs directory |
| `baseSectionsDirectory` | string | - | "Sections" | Base game sections directory |

## Edge Cases + Counters

- **Missing JSON files**: Skip missing files, continue loading other files. Pack still valid.
- **Invalid JSON**: Log error, skip pack. Continue loading other packs.
- **Duplicate IDs**: Later pack overrides earlier pack (deterministic merge).
- **Empty pack**: Pack with no JSON files still loaded (empty content).

## Telemetry Hooks

- Log pack loading: `ModPackLoaded(packName, version, itemCount, blockCount, biomeCount, roomCount, sectionCount, timestamp)`
- Log section loading: `SectionLoaded(sectionId, source, timestamp)` (optional)
- Log merge conflicts: `ContentMergeConflict(contentType, id, overriddenBy, timestamp)` (optional)

## Implementation Notes

**File**: `GL2Project/Content/ModLoader.cs`, `GL2Project/Content/SectionLoader.cs`, `GL2Project/Content/ContentPack.cs`

**Key Systems**:
- `ModLoader`: Scans and loads mod packs
- `SectionLoader`: Loads sections from base game and mods
- `ContentDatabase`: Merged content database

**Deterministic Ordering**:
1. Scan mods directory
2. Sort pack directories alphabetically
3. Load each pack (alphabetical order)
4. Merge content (later overrides earlier)
5. Load sections (base + mods, sorted by ID)

**Content Format**: JSON files in mod packs. See `GL2Project/Mods/ExamplePack/` for examples.

**Section Format**: `sections.json` contains array of section definitions. See `GL2Project/Mods/ExamplePack/sections.json`.

**Path Resolution**: Section `LevelDataPath` resolved relative to pack directory if not absolute.

