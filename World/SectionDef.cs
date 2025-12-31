using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GL2Engine.World;

/// <summary>
/// Definition for a handcrafted section that can be assembled into stages.
/// </summary>
public struct SectionDef
{
  public string Id;
  public PacingTag PacingTag;
  public List<string> BiomeTags; // Biome IDs (A, B, C, etc.)
  public int TierMin;
  public int TierMax;
  public int DifficultyStars; // 1-5
  public LengthClass LengthClass;
  public TraversalMode TraversalMode;
  public InteractionTags InteractionTags; // Bitset
  public List<string> ConnectorsIn; // Connector types for chaining
  public List<string> ConnectorsOut;
  public SectionQuotas Quotas;
  public string LevelDataPath; // Path to section JSON file
}

/// <summary>
/// Pacing tag: FAST (timer/flow pressure) or SLOW (exploration/precision).
/// </summary>
public enum PacingTag
{
  FAST,
  SLOW
}

/// <summary>
/// Length class for section duration estimation.
/// </summary>
public enum LengthClass
{
  SHORT, // 20-45s
  MED,   // 45-90s
  LONG   // 90-180s
}

/// <summary>
/// Traversal mode indicating how the section is navigated.
/// </summary>
public enum TraversalMode
{
  RUNLINE,              // Horizontal A→B
  VERTICAL_ASCENT,      // Upward climbing
  VERTICAL_DESCENT,     // Downward descent
  AUTOSCROLL,           // Camera scroll pressure
  VEHICLE,              // Minecart/track/forced speed
  CANNON_CHAIN,         // Barrel cannon sequence
  OPEN_EXPLORATION      // Hub-like pockets
}

/// <summary>
/// Interaction tags as bitset for filtering and matching.
/// </summary>
[Flags]
public enum InteractionTags
{
  None = 0,
  BarrelCannon = 1 << 0,
  TeamUpRequired = 1 << 1,
  CarryProp = 1 << 2,
  Ropes = 1 << 3,
  BoostPole = 1 << 4,
  Water = 1 << 5,
  RisingHazard = 1 << 6,
  PuzzleGate = 1 << 7,
  MinigamePortal = 1 << 8,
  BonusDoor = 1 << 9,
  Enemies = 1 << 10,
  Obstacles = 1 << 11,
  OpenExploration = 1 << 12
}

/// <summary>
/// Quotas for secrets, bonus doors, and chests in a section.
/// </summary>
public struct SectionQuotas
{
  public int SecretSlots;
  public int BonusDoorSlots;
  public int ChestSlots;
}

