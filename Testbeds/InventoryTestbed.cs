using System;
using GL2Engine.Engine;
using GL2Engine.ECS;

namespace GL2Engine.Testbeds;

/// <summary>
/// Testbed for inventory system: pickups, per-item caps, catalog unlock.
/// </summary>
public class InventoryTestbed
{
  private GameWorld _world;
  private GL2Engine.Inventory.Inventory _inventory;
  private GL2Engine.Inventory.ItemCatalog _catalog;
  private GL2Engine.Inventory.LootTable _lootTable;

  public InventoryTestbed(GameWorld world)
  {
    _world = world;
    _catalog = new GL2Engine.Inventory.ItemCatalog();
    _inventory = new GL2Engine.Inventory.Inventory(_catalog);
    _lootTable = new GL2Engine.Inventory.LootTable();

    // Register some test items
    _catalog.RegisterItem(new GL2Engine.Inventory.ItemDef("test_relic", GL2Engine.Inventory.ItemCategory.Relic, 1));
    _catalog.RegisterItem(new GL2Engine.Inventory.ItemDef("test_consumable", GL2Engine.Inventory.ItemCategory.Consumable, 10));
    _catalog.RegisterItem(new GL2Engine.Inventory.ItemDef("test_material", GL2Engine.Inventory.ItemCategory.Material, 99));

    // Setup test loot table
    _lootTable.AddEntry("test_relic", 10, 1, 1);
    _lootTable.AddEntry("test_consumable", 50, 1, 3);
    _lootTable.AddEntry("test_material", 100, 1, 5);
  }

  public GL2Engine.Inventory.Inventory GetInventory() => _inventory;
  public GL2Engine.Inventory.ItemCatalog GetCatalog() => _catalog;
  public GL2Engine.Inventory.LootTable GetLootTable() => _lootTable;

  /// <summary>
  /// Creates a pickup entity in the world.
  /// </summary>
  public Entity CreatePickup(Microsoft.Xna.Framework.Vector2 position, string itemId, int count)
  {
    var entity = _world.CreateEntity();
    _world.Positions.Add(entity, new Position { Value = position });
    _world.Colliders.Add(entity, new Collider 
    { 
      Size = new Microsoft.Xna.Framework.Vector2(8, 8), 
      Type = ColliderType.AABB 
    });
    
    // TODO: Add ItemPickup component when we implement pickup system
    // For now, this is a stub

    return entity;
  }

  /// <summary>
  /// Creates a chest entity that drops loot when opened.
  /// </summary>
  public Entity CreateChest(Microsoft.Xna.Framework.Vector2 position, Rng rewardStream)
  {
    var entity = _world.CreateEntity();
    _world.Positions.Add(entity, new Position { Value = position });
    _world.Colliders.Add(entity, new Collider 
    { 
      Size = new Microsoft.Xna.Framework.Vector2(16, 16), 
      Type = ColliderType.AABB 
    });

    // Roll loot from table
    var drops = _lootTable.RollLoot(rewardStream, 3);
    
    // TODO: Store drops in chest component
    // For now, just log them
    Console.WriteLine($"Chest created at {position} with {drops.Count} drops");

    return entity;
  }

  /// <summary>
  /// Tests catalog unlock on first pickup.
  /// </summary>
  public void TestCatalogUnlock(string itemId)
  {
    bool wasDiscovered = _catalog.IsDiscovered(itemId);
    int added = _inventory.AddItem(itemId, 1);
    bool isDiscovered = _catalog.IsDiscovered(itemId);

    Console.WriteLine($"Item {itemId}: Was discovered: {wasDiscovered}, Added: {added}, Now discovered: {isDiscovered}");
  }

  /// <summary>
  /// Tests per-item capacity limits.
  /// </summary>
  public void TestCapacityLimit(string itemId, int attemptAmount)
  {
    int before = _inventory.GetCount(itemId);
    int capacity = _inventory.GetCapacity(itemId);
    int added = _inventory.AddItem(itemId, attemptAmount);
    int after = _inventory.GetCount(itemId);

    Console.WriteLine($"Item {itemId}: Before: {before}, Capacity: {capacity}, Attempted: {attemptAmount}, Added: {added}, After: {after}");
  }
}

