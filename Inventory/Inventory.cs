using System;
using System.Collections.Generic;

namespace GL2Engine.Inventory;

/// <summary>
/// Inventory model with per-item capacity.
/// </summary>
public class Inventory
{
  private Dictionary<string, ItemStack> _items = new Dictionary<string, ItemStack>();
  private ItemCatalog _catalog;

  public Inventory(ItemCatalog catalog)
  {
    _catalog = catalog;
  }

  /// <summary>
  /// Adds items to inventory. Returns amount actually added (may be less if capacity exceeded).
  /// </summary>
  public int AddItem(string itemId, int amount)
  {
    if (amount <= 0)
      return 0;

    // Unlock item in catalog on first pickup
    if (!_items.ContainsKey(itemId))
    {
      _catalog.UnlockItem(itemId);
      var itemDef = _catalog.GetItemDef(itemId);
      if (itemDef.HasValue)
      {
        int capacity = _catalog.GetCapacity(itemId);
        _items[itemId] = new ItemStack(itemId, capacity);
      }
      else
      {
        // Item not in catalog, use default capacity
        _items[itemId] = new ItemStack(itemId, 99);
      }
    }

    var stack = _items[itemId];
    int canAdd = Math.Min(amount, stack.GetRemainingSpace());
    stack.Count += canAdd;
    _items[itemId] = stack;

    return canAdd;
  }

  /// <summary>
  /// Checks if items can be added.
  /// </summary>
  public bool CanAdd(string itemId, int amount)
  {
    if (!_items.ContainsKey(itemId))
    {
      // Item not in inventory yet, check catalog for capacity
      int capacity = _catalog.GetCapacity(itemId);
      return amount <= capacity;
    }

    return _items[itemId].CanAdd(amount);
  }

  /// <summary>
  /// Removes items from inventory. Returns amount actually removed.
  /// </summary>
  public int RemoveItem(string itemId, int amount)
  {
    if (!_items.ContainsKey(itemId))
      return 0;

    var stack = _items[itemId];
    int canRemove = Math.Min(amount, stack.Count);
    stack.Count -= canRemove;
    
    if (stack.Count <= 0)
    {
      _items.Remove(itemId);
    }
    else
    {
      _items[itemId] = stack;
    }

    return canRemove;
  }

  /// <summary>
  /// Gets the count of an item.
  /// </summary>
  public int GetCount(string itemId)
  {
    return _items.TryGetValue(itemId, out var stack) ? stack.Count : 0;
  }

  /// <summary>
  /// Gets the capacity of an item.
  /// </summary>
  public int GetCapacity(string itemId)
  {
    if (_items.TryGetValue(itemId, out var stack))
      return stack.Capacity;
    
    return _catalog.GetCapacity(itemId);
  }

  /// <summary>
  /// Upgrades capacity for an item (via catalog unlock).
  /// </summary>
  public void UpgradeCapacity(string itemId, int newCapacity)
  {
    if (_items.ContainsKey(itemId))
    {
      var stack = _items[itemId];
      stack.Capacity = newCapacity;
      _items[itemId] = stack;
    }
  }

  /// <summary>
  /// Gets all items in inventory.
  /// </summary>
  public Dictionary<string, ItemStack> GetAllItems()
  {
    return new Dictionary<string, ItemStack>(_items);
  }

  /// <summary>
  /// Clears run-scoped items (called on game over).
  /// </summary>
  public void ClearRunScopedItems(ItemCatalog catalog)
  {
    var toRemove = new List<string>();
    foreach (var kvp in _items)
    {
      var itemDef = catalog.GetItemDef(kvp.Key);
      if (itemDef.HasValue && itemDef.Value.IsRunScoped)
      {
        toRemove.Add(kvp.Key);
      }
    }

    foreach (var itemId in toRemove)
    {
      _items.Remove(itemId);
    }
  }
}

