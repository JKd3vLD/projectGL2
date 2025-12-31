# Adding Slope Blocks

## Slope Definition

Slopes are defined with start and end points in block-local coordinates, and a precomputed angle.

### Slope Data Structure

```csharp
public class SlopeData
{
    public Vector2 StartPoint;  // Block-local coordinates (0-64)
    public Vector2 EndPoint;    // Block-local coordinates (0-64)
    public float AngleDegrees;  // Precomputed angle
    public bool SlideEligible;  // True if angle > 30°
}
```

## Slope Angles

- **< 30°**: Walkable, no sliding
- **30° - 70°**: Walkable, sliding when pressing down
- **> 70°**: Auto-slide (always slides)

## Creating a Slope Block

1. Define start and end points in block-local space (0-64 range)
2. Compute angle using `atan2`:
   ```csharp
   Vector2 dir = EndPoint - StartPoint;
   float angleDegrees = MathHelper.ToDegrees(MathF.Atan2(-dir.Y, dir.X));
   ```
3. Mark as slide-eligible if angle > 30°
4. Add to `LevelLoader.GetBlockDefinition()`

## Example: 45° Slope

```csharp
Slope = new SlopeData
{
    StartPoint = new Vector2(0, 64),   // Bottom-left
    EndPoint = new Vector2(64, 0),     // Top-right
    AngleDegrees = 45.0f,
    SlideEligible = true
}
```

## Collision Resolution

Slopes are resolved in `PhysicsSystem.ResolveCollisions()`:
- Player position is adjusted to sit on slope surface
- Ground normal is computed from slope angle
- Sliding is applied along slope tangent when eligible

