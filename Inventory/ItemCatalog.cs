using System;
using System.Collections.Generic;

namespace GL2Engine.Inventory;

/// <summary>
/// Discovery-based item catalog. Unlocks items on first pickup and tracks capacity upgrades.
/// </summary>
public class ItemCatalog
{
  private HashSet<string> _discoveredItems = new HashSet<string>();
  private Dictionary<string, int> _capacityUpgrades = new Dictionary<string, int>();
  private Dictionary<string, ItemDef> _itemDefs = new Dictionary<string, ItemDef>();

  /// <summary>
  /// Registers an item definition.
  /// </summary>
  public void RegisterItem(ItemDef itemDef)
  {
    _itemDefs[itemDef.Id] = itemDef;
    if (!_capacityUpgrades.ContainsKey(itemDef.Id))
    {
      _capacityUpgrades[itemDef.Id] = itemDef.MaxStack;
    }
  }

  /// <summary>
  /// Unlocks an item in the catalog (called on first pickup).
  /// </summary>
  public void UnlockItem(string itemId)
  {
    _discoveredItems.Add(itemId);
  }

  /// <summary>
  /// Checks if an item is discovered.
  /// </summary>
  public bool IsDiscovered(string itemId)
  {
    return _discoveredItems.Contains(itemId);
  }

  /// <summary>
  /// Gets the current capacity for an item (base + upgrades).
  /// </summary>
  public int GetCapacity(string itemId)
  {
    if (_capacityUpgrades.TryGetValue(itemId, out int capacity))
      return capacity;

    // Default capacity if not registered
    if (_itemDefs.TryGetValue(itemId, out var def))
      return def.MaxStack;

    return 99; // Default fallback
  }

  /// <summary>
  /// Upgrades capacity for an item.
  /// </summary>
  public void UpgradeCapacity(string itemId, int newCapacity)
  {
    _capacityUpgrades[itemId] = newCapacity;
  }

  /// <summary>
  /// Gets item definition.
  /// </summary>
  public ItemDef? GetItemDef(string itemId)
  {
    return _itemDefs.TryGetValue(itemId, out var def) ? def : null;
  }

  /// <summary>
  /// Gets all discovered items.
  /// </summary>
  public HashSet<string> GetDiscoveredItems()
  {
    return new HashSet<string>(_discoveredItems);
  }

  /// <summary>
  /// Gets all registered item definitions.
  /// </summary>
  public Dictionary<string, ItemDef> GetAllItemDefs()
  {
    return new Dictionary<string, ItemDef>(_itemDefs);
  }
}

