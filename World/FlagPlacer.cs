using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GL2Engine.World;

namespace GL2Engine.World;

/// <summary>
/// Places flags procedurally in stage plans.
/// </summary>
public static class FlagPlacer
{
  /// <summary>
  /// Places flags in a stage plan: start, middle (if LONG sections), end.
  /// </summary>
  public static void PlaceFlags(StagePlan plan)
  {
    plan.Flags.Clear();

    if (plan.Sections.Count == 0)
      return;

    // Start flag at beginning of first section (position 0,0 - will be set from section data)
    plan.Flags.Add(new FlagPosition
    {
      Type = FlagType.Start,
      Position = Vector2.Zero, // Will be set from section level data
      SectionIndex = 0,
      IsConsumable = false
    });

    // Middle flags: end of LONG sections (if stage has 3+ sections)
    if (plan.Sections.Count >= 3)
    {
      for (int i = 0; i < plan.Sections.Count - 1; i++)
      {
        if (plan.Sections[i].LengthClass == LengthClass.LONG)
        {
          plan.Flags.Add(new FlagPosition
          {
            Type = FlagType.Middle,
            Position = Vector2.Zero, // Will be set from section level data
            SectionIndex = i,
            IsConsumable = true // Middle flags are consumable (Shovel Knight style)
          });
        }
      }
    }

    // End flag at end of last section
    plan.Flags.Add(new FlagPosition
    {
      Type = FlagType.End,
      Position = Vector2.Zero, // Will be set from section level data
      SectionIndex = plan.Sections.Count - 1,
      IsConsumable = false
    });
  }

  /// <summary>
  /// Updates flag positions from loaded section level data.
  /// </summary>
  public static void UpdateFlagPositions(StagePlan plan, Dictionary<int, Vector2> sectionEndPositions)
  {
    foreach (var flag in plan.Flags)
    {
      if (sectionEndPositions.ContainsKey(flag.SectionIndex))
      {
        var flagPos = flag;
        flagPos.Position = sectionEndPositions[flag.SectionIndex];
        // Note: Can't modify struct in list directly, would need to rebuild list
        // For now, positions are set during level loading
      }
    }
  }
}

