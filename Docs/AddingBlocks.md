# Adding Blocks to Levels

## Level Format

Levels are stored as JSON files in the `Levels/` directory. Each level contains a list of block placements.

### Block Placement Structure

```json
{
  "blockId": "ground_flat",
  "gridX": 0,
  "gridY": 4,
  "rotation": 0,
  "flip": false,
  "variant": 0
}
```

- **blockId**: String identifier for the block type (e.g., "ground_flat", "slope_gentle")
- **gridX**: X position in 64×64 grid units
- **gridY**: Y position in 64×64 grid units
- **rotation**: Rotation angle in degrees (0, 90, 180, 270)
- **flip**: Boolean for horizontal flip
- **variant**: Integer variant ID (for different visual styles)

## Block Types

### Flat Ground
- **blockId**: "ground_flat"
- **Size**: 64×64 pixels
- **Collision**: AABB (full block)

### Gentle Slope
- **blockId**: "slope_gentle"
- **Size**: 64×64 pixels
- **Collision**: Slope segment (45° angle)
- **Slide Eligible**: Yes (>30°)

### Steep Slope
- **blockId**: "slope_steep"
- **Size**: 64×64 pixels
- **Collision**: Slope segment (63.4° angle)
- **Slide Eligible**: Yes (>30°)

## Adding New Blocks

1. Define the block in `Content/LevelLoader.cs` → `GetBlockDefinition()`
2. Add block placement to level JSON file
3. Block will be loaded and rendered automatically

## Example Level

See `Levels/Testbed.json` for a complete example with multiple block types.

