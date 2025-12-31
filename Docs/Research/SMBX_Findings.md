# SMBX2/TheXTech Findings - Modding & Level Format

## What We Adopt

- **Content pack structure**: `/Mods/<PackName>/` folder with JSON files (prefer JSON over INI for easier parsing)
- **Stable pack loading order**: Alphabetical order ensures deterministic loading
- **Level format concepts**: Blocks, entities, camera bounds stored in structured format
- **Editor integration concepts**: Level editor should be able to export to our JSON format

## What We Reject

- **INI file format**: SMBX2 uses INI files extensively. We prefer JSON for easier parsing and type safety.
- **Lua scripting**: SMBX2's extensive Lua scripting system. Too complex for M2-M3 foundation.
- **Complex block properties**: SMBX2's extensive block configuration. We keep it minimal.
- **World map system**: SMBX2's world/level separation. We use tier/stage structure instead.

## Minimal GL2 Implementation Plan

1. **ModLoader** (`GL2Project/Content/ModLoader.cs`):
   - Scans `/Mods/` directory for pack folders
   - Loads packs in alphabetical order
   - Merges content definitions (items, blocks, biomes, rooms)
   - Validates pack structure

2. **ContentPack structure** (`GL2Project/Content/ContentPack.cs`):
   - `Name` (string), `Version` (string), `Items` (List<ItemDef>), `Blocks` (List<BlockDef>), `Biomes` (List<BiomeDef>), `Rooms` (List<RoomDef>)
   - Optional: `textures/`, `models/`, `effects/` subdirectories

3. **Minimal JSON level format** (`GL2Project/Content/LevelFormat.cs`):
   ```json
   {
     "blocks": [{"blockId": "ground_flat", "gridX": 0, "gridY": 10}],
     "entities": [{"type": "chest", "x": 100, "y": 200}],
     "cameraVolumes": [{"bounds": {"x": 0, "y": 0, "width": 320, "height": 180}, "allowVerticalFollow": true}]
   }
   ```

4. **ExamplePack** (`GL2Project/Mods/ExamplePack/`):
   - `items.json` - Sample item definitions
   - `blocks.json` - Sample block definitions
   - `biomes.json` - Sample biome definitions
   - `rooms.json` - Sample room definitions
   - `README.md` - Documentation for mod authors

## 3 Edge Cases to Test

1. **Pack loading order**: Two packs define same item ID - later pack (alphabetically) should override earlier pack
2. **Missing pack files**: Pack folder exists but `items.json` is missing - should load other files, log warning
3. **Invalid JSON**: Malformed JSON in pack file - should log error, skip pack, continue loading other packs

