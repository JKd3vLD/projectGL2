using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GL2Engine.Content;

/// <summary>
/// Minimal JSON level format for stage layout + collision + camera volumes.
/// </summary>
public class LevelFormat
{
  public List<BlockEntry> Blocks { get; set; } = new List<BlockEntry>();
  public List<EntityEntry> Entities { get; set; } = new List<EntityEntry>();
  public List<CameraVolumeEntry> CameraVolumes { get; set; } = new List<CameraVolumeEntry>();
}

public struct BlockEntry
{
  public string BlockId { get; set; }
  public int GridX { get; set; }
  public int GridY { get; set; }
}

public struct EntityEntry
{
  public string Type { get; set; }
  public float X { get; set; }
  public float Y { get; set; }
  public Dictionary<string, object>? Properties { get; set; }
}

public struct CameraVolumeEntry
{
  public Rectangle Bounds { get; set; }
  public Vector2 CameraOffset { get; set; }
  public bool AllowVerticalFollow { get; set; }
  public int VolumeId { get; set; }
}

