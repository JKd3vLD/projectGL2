using System;

namespace GL2Engine.Inventory;

/// <summary>
/// Item tag system for synergy evaluation. Tags stored as bitmask.
/// </summary>
[Flags]
public enum ItemTag : ulong
{
  None = 0,
  Fire = 1UL << 0,
  Ice = 1UL << 1,
  Gravity = 1UL << 2,
  Electric = 1UL << 3,
  Poison = 1UL << 4,
  Light = 1UL << 5,
  Dark = 1UL << 6,
  Honey = 1UL << 7,
  Water = 1UL << 8,
  Wind = 1UL << 9,
  Earth = 1UL << 10,
  // Add more tags as needed
}

/// <summary>
/// Helper methods for tag bitmask operations.
/// </summary>
public static class ItemTagHelper
{
  public static bool HasTag(ulong bitmask, ItemTag tag)
  {
    return (bitmask & (ulong)tag) != 0;
  }

  public static ulong AddTag(ulong bitmask, ItemTag tag)
  {
    return bitmask | (ulong)tag;
  }

  public static ulong RemoveTag(ulong bitmask, ItemTag tag)
  {
    return bitmask & ~(ulong)tag;
  }

  public static ulong SetTags(ulong bitmask, params ItemTag[] tags)
  {
    ulong result = bitmask;
    foreach (var tag in tags)
    {
      result = AddTag(result, tag);
    }
    return result;
  }
}

