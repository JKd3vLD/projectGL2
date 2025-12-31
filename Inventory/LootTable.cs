using System;
using System.Collections.Generic;
using System.Linq;
using GL2Engine.Engine;

namespace GL2Engine.Inventory;

/// <summary>
/// Drop table system for deterministic loot generation using Reward RNG stream.
/// </summary>
public class LootTable
{
  private List<LootEntry> _entries = new List<LootEntry>();

  public LootTable()
  {
  }

  /// <summary>
  /// Adds a loot entry with weight.
  /// </summary>
  public void AddEntry(string itemId, int weight, int minCount = 1, int maxCount = 1)
  {
    _entries.Add(new LootEntry
    {
      ItemId = itemId,
      Weight = weight,
      MinCount = minCount,
      MaxCount = maxCount
    });
  }

  /// <summary>
  /// Rolls loot from the table using Reward RNG stream. Returns deterministic results per seed.
  /// </summary>
  public List<ItemDrop> RollLoot(Rng rewardStream, int count = 1)
  {
    var results = new List<ItemDrop>();

    if (_entries.Count == 0)
      return results;

    int totalWeight = _entries.Sum(e => e.Weight);

    for (int i = 0; i < count; i++)
    {
      if (totalWeight <= 0)
        break;

      int roll = rewardStream.NextInt(0, totalWeight);
      int accumulatedWeight = 0;

      foreach (var entry in _entries)
      {
        accumulatedWeight += entry.Weight;
        if (roll < accumulatedWeight)
        {
          int itemCount = rewardStream.NextInt(entry.MinCount, entry.MaxCount + 1);
          results.Add(new ItemDrop
          {
            ItemId = entry.ItemId,
            Count = itemCount
          });
          break;
        }
      }
    }

    return results;
  }

  /// <summary>
  /// Clears all entries.
  /// </summary>
  public void Clear()
  {
    _entries.Clear();
  }
}

/// <summary>
/// Loot table entry.
/// </summary>
public struct LootEntry
{
  public string ItemId;
  public int Weight;
  public int MinCount;
  public int MaxCount;
}

/// <summary>
/// Item drop result.
/// </summary>
public struct ItemDrop
{
  public string ItemId;
  public int Count;
}

