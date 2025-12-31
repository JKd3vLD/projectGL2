using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GL2Engine.Content;

public struct LevelData
{
  public List<BlockPlacement> Blocks { get; set; }
}

public struct BlockPlacement
{
  public string BlockId { get; set; }
  public int GridX { get; set; }
  public int GridY { get; set; }
  public float Rotation { get; set; }
  public bool Flip { get; set; }
  public int Variant { get; set; }
}

public struct BlockDefinition
{
  public string Id;
  public Vector2 Size; // In pixels (64x64 default)
  public CollisionType CollisionType;
  public SlopeData? Slope; // Null if not a slope
}

public enum CollisionType
{
  None,
  AABB,
  Slope
}

public struct SlopeData
{
  public Vector2 StartPoint; // Block-local coordinates
  public Vector2 EndPoint;
  public float AngleDegrees; // Precomputed
  public bool SlideEligible; // True if angle > 30°
}
