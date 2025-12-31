# Terraria Findings - Inventory & Items

## What We Adopt

- **Item structure**: `type` (int ID), `stack` (current count), `maxStack` (capacity per item type), `favorited` (marking system)
- **Per-item capacity model**: Each item type has its own capacity limit (similar to Skyward Sword material system). Capacity can be upgraded individually per item type as catalog entries are discovered.
- **Discovery-based catalog unlock**: First time picking up an item unlocks its catalog entry, allowing future capacity upgrades.
- **Item categories**: Relics, Consumables, Materials, Collectibles, Tools/Props
- **Tag system**: Items can have tags (Fire, Ice, Gravity, etc.) stored as bitmask for synergy evaluation later
- **Drop tables**: Deterministic loot generation using seeded RNG streams
- **Crafting hooks**: Recipe system structure (stub for now, full implementation later)

## What We Reject

- **Global inventory slots**: Terraria uses a fixed inventory grid. We use per-item capacity instead.
- **Equipment slots**: Terraria's armor/accessory slot system. We handle equipment differently (run-scoped tools only).
- **Item rarity tiers**: Terraria's color-coded rarity system. We use simpler rarity enum for our needs.
- **Item prefixes/suffixes**: Terraria's modifier system. Too complex for M2-M3.
- **Auto-stack to nearby chests**: Terraria's quick-stack feature. Not needed for our scope.

## Minimal GL2 Implementation Plan

1. **ItemDef structure** (`GL2Project/Inventory/ItemDef.cs`):
   - `id` (string), `type` (ItemCategory enum), `maxStack` (int, default capacity)
   - `tagsBitmask` (ulong for tag storage), `rarity` (ItemRarity enum)
   - `useAction` (delegate/string for use behavior), `isRunScoped` (bool)

2. **Inventory model** (`GL2Project/Inventory/Inventory.cs`):
   - Dictionary<ItemId, ItemStack> where ItemStack contains `count` and `capacity`
   - Capacity starts at `ItemDef.maxStack`, can be upgraded via catalog unlocks
   - Methods: `AddItem(ItemId, count)`, `CanAdd(ItemId, count)`, `RemoveItem(ItemId, count)`

3. **ItemCatalog** (`GL2Project/Inventory/ItemCatalog.cs`):
   - HashSet<ItemId> for discovered items
   - Dictionary<ItemId, int> for capacity upgrades per item
   - `UnlockItem(ItemId)` method that triggers on first pickup

4. **LootTable** (`GL2Project/Inventory/LootTable.cs`):
   - Weighted drop table using Reward RNG stream
   - Deterministic results per seed
   - `RollLoot(RngStream rewardStream) → List<ItemDrop>`

5. **JSON item definitions** (`/Mods/Base/items.json`):
   - Array of ItemDef entries
   - Loaded at startup via ModLoader

## 3 Edge Cases to Test

1. **Capacity overflow**: Player picks up item when at max capacity - should drop excess as pickup entity
2. **Catalog unlock timing**: First pickup triggers catalog unlock, subsequent pickups don't trigger unlock event again
3. **Deterministic loot**: Same seed + tier + category codes produces identical loot drops across runs

