using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace GL2Engine.Content;

/// <summary>
/// Loads level data from JSON files.
/// </summary>
public static class LevelLoader
{
  public static LevelData LoadLevel(string path)
  {
    try
    {
      var json = File.ReadAllText(path);
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true
      };
      var result = JsonSerializer.Deserialize<LevelData>(json, options);
      // LevelData is a struct, so it can't be null - if deserialization fails, it returns default
      if (result.Blocks == null)
        return new LevelData { Blocks = [] };
      return result;
    }
    catch (Exception ex)
    {
      throw new Exception($"Failed to load level from {path}: {ex.Message}", ex);
    }
  }

  public static BlockDefinition GetBlockDefinition(string blockId)
  {
    // Block definitions - in a real implementation, these would come from a data file
    // For now, return hardcoded definitions
    return blockId switch
    {
      "ground_flat" => new BlockDefinition
      {
        Id = "ground_flat",
        Size = new Vector2(64, 64),
        CollisionType = CollisionType.AABB
      },
      "slope_gentle" => new BlockDefinition
      {
        Id = "slope_gentle",
        Size = new Vector2(64, 64),
        CollisionType = CollisionType.Slope,
        Slope = new SlopeData
        {
          StartPoint = new Vector2(0, 64),
          EndPoint = new Vector2(64, 0),
          AngleDegrees = 45.0f,
          SlideEligible = true
        }
      },
      "slope_steep" => new BlockDefinition
      {
        Id = "slope_steep",
        Size = new Vector2(64, 64),
        CollisionType = CollisionType.Slope,
        Slope = new SlopeData
        {
          StartPoint = new Vector2(0, 64),
          EndPoint = new Vector2(32, 0),
          AngleDegrees = 63.4f,
          SlideEligible = true
        }
      },
      _ => new BlockDefinition
      {
        Id = blockId,
        Size = new Vector2(64, 64),
        CollisionType = CollisionType.None
      }
    };
  }

  public static SlopeData PrecomputeSlopeAngle(SlopeData slope)
  {
    Vector2 dir = slope.EndPoint - slope.StartPoint;
    float angleDegrees = MathHelper.ToDegrees(MathF.Atan2(-dir.Y, dir.X));
    return slope with { AngleDegrees = angleDegrees, SlideEligible = angleDegrees > 30.0f };
  }

  /// <summary>
  /// Loads a stage plan by loading all section JSON files in order and merging them.
  /// </summary>
  public static LevelData LoadStagePlan(GL2Engine.World.StagePlan plan)
  {
    var mergedLevel = new LevelData { Blocks = new List<BlockPlacement>() };
    float currentX = 0f;
    float sectionWidth = 640f; // Default section width

    foreach (var section in plan.Sections)
    {
      try
      {
        var sectionData = LoadLevel(section.LevelDataPath);
        
        // Merge blocks with offset
        if (sectionData.Blocks != null)
        {
          foreach (var block in sectionData.Blocks)
          {
            mergedLevel.Blocks.Add(new BlockPlacement
            {
              BlockId = block.BlockId,
              GridX = block.GridX + (int)(currentX / 64f), // Convert to grid offset
              GridY = block.GridY,
              Rotation = block.Rotation,
              Flip = block.Flip,
              Variant = block.Variant
            });
          }
        }

        // Advance X position for next section
        currentX += sectionWidth;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to load section {section.Id}: {ex.Message}");
        // Continue with next section
      }
    }

    return mergedLevel;
  }
}
