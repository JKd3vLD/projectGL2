using System;
using GL2Engine.ECS;
using GL2Engine.World;

namespace GL2Engine.Inventory;

/// <summary>
/// Item definition structure matching our rules.
/// </summary>
public struct ItemDef
{
  public string Id;
  public ItemCategory Type;
  public int MaxStack; // Default capacity per item type
  public ulong TagsBitmask; // Tags for synergy (Fire, Ice, Gravity, etc.)
  public ItemRarity Rarity;
  public string? UseAction; // Action identifier (delegate/string for use behavior)
  public bool IsRunScoped; // If true, item is lost on game over
  public RelicType? RelicType; // For Technique Mods: TechniqueMod, StatMod, etc.
  public FlowEventType[]? Triggers; // Event types that trigger this relic
  public TechniqueEffect[]? Effects; // Effects this relic applies

  public ItemDef(string id, ItemCategory type, int maxStack = 99)
  {
    Id = id;
    Type = type;
    MaxStack = maxStack;
    TagsBitmask = 0;
    Rarity = ItemRarity.Common;
    UseAction = null;
    IsRunScoped = false;
    RelicType = null;
    Triggers = null;
    Effects = null;
  }
}

/// <summary>
/// Relic type for Technique Mods.
/// </summary>
public enum RelicType
{
  TechniqueMod, // Contextual bonuses without movement changes
  StatMod,      // Direct stat modifications (not used for Technique Mods)
  UtilityMod    // Utility effects
}

/// <summary>
/// Technique effect applied by a relic.
/// </summary>
public struct TechniqueEffect
{
  public EffectKind Kind;
  public float Magnitude; // Float or int depending on kind
  public EffectConstraints Constraints;
}

/// <summary>
/// Effect kind for Technique Mods.
/// </summary>
public enum EffectKind
{
  FlowDelta,              // Add to Flow Meter
  ThresholdProgressDelta, // Add to Run Value (XP threshold progress)
  RewardQualityDelta,     // Modify reward quality tier (+1, -1, etc.)
  TokenGrant,             // Grant a token (speed tier, bonus token, etc.)
  UIHint                  // Show UI hint (treasure ping, etc.)
}

/// <summary>
/// Constraints for Technique Mod effects.
/// </summary>
public struct EffectConstraints
{
  public float Cooldown; // Cooldown in seconds
  public int PerSectionMax; // Max procs per section
  public bool RequiresPacingTag; // Only in FAST or SLOW stages
  public PacingTag? PacingTag; // Which pacing tag required
}

/// <summary>
/// Item categories for inventory organization.
/// </summary>
public enum ItemCategory
{
  Relic,
  Consumable,
  Material,
  Collectible,
  Tool,
  Prop
}

/// <summary>
/// Item rarity levels.
/// </summary>
public enum ItemRarity
{
  Common,
  Uncommon,
  Rare,
  Epic,
  Legendary
}
