namespace GL2Engine.Inventory;

/// <summary>
/// Item stack containing count and capacity.
/// </summary>
public struct ItemStack
{
  public string ItemId;
  public int Count;
  public int Capacity; // Can be upgraded via catalog unlocks

  public ItemStack(string itemId, int capacity)
  {
    ItemId = itemId;
    Count = 0;
    Capacity = capacity;
  }

  public bool CanAdd(int amount)
  {
    return Count + amount <= Capacity;
  }

  public int GetRemainingSpace()
  {
    return Capacity - Count;
  }
}

