using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GL2Engine.World;

/// <summary>
/// Plan for an assembled stage containing ordered sections and metadata.
/// </summary>
public class StagePlan
{
  public PacingTag PacingTag { get; set; }
  public List<SectionDef> Sections { get; set; } = new List<SectionDef>();
  public List<SectionDef> SidePockets { get; set; } = new List<SectionDef>(); // Optional exploration sections for SLOW
  public List<FlagPosition> Flags { get; set; } = new List<FlagPosition>();
  public RewardProfile RewardProfile { get; set; }
  public List<DifficultyRamp> DifficultyRamp { get; set; } = new List<DifficultyRamp>(); // Teach→Test→Twist→Finale
}

/// <summary>
/// Flag position in a stage plan.
/// </summary>
public struct FlagPosition
{
  public FlagType Type;
  public Vector2 Position;
  public int SectionIndex; // Which section this flag belongs to
  public bool IsConsumable; // Can be consumed for rewards (Shovel Knight style)
}

/// <summary>
/// Flag type: Start, Middle (checkpoint), or End.
/// </summary>
public enum FlagType
{
  Start,
  Middle,
  End
}

/// <summary>
/// Reward profile indicating what type of rewards this stage offers.
/// </summary>
public enum RewardProfile
{
  SPEED,    // FAST: speed/flow bonuses
  TREASURE, // SLOW: exploration/treasure bonuses
  QUEST,    // SLOW: quest item bonuses
  MIXED     // Combination
}

/// <summary>
/// Difficulty ramp position in Kishotenketsu structure.
/// </summary>
public enum DifficultyRamp
{
  Teach,  // 1-star, introduces mechanics
  Test,   // 2-3 star, standard challenge
  Twist,  // 3-4 star, introduces variation
  Finale  // 4-5 star, peak difficulty
}

