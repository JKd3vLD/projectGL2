# Balatro Findings - Tag/Synergy System

## What We Adopt

- **Tag system with context triggers**: Tags can trigger on different contexts (`eval`, `immediate`, `new_blind_choice`, etc.)
- **Bitmask-based tag storage**: Tags stored as bitmask (ulong) for efficient storage and evaluation
- **Synergy evaluation hooks**: System for evaluating tag combinations and their effects
- **Ordering concepts**: Tags evaluated in specific order for synergy calculations

## What We Reject

- **Complex tag UI**: Balatro's extensive tag selection UI. Too complex for M2-M3.
- **Tag removal system**: Balatro's tag removal mechanics. We'll handle this differently.
- **Tag-specific card generation**: Balatro's tag-based card spawning. Not applicable to our system.

## Minimal GL2 Implementation Plan

1. **Tag bitmask system** (`GL2Project/Inventory/ItemTags.cs`):
   - Enum `ItemTag` with flags: `None = 0`, `Fire = 1`, `Ice = 2`, `Gravity = 4`, `Electric = 8`, etc.
   - `ItemDef.tagsBitmask` stores tags as ulong
   - Helper methods: `HasTag(bitmask, tag)`, `AddTag(bitmask, tag)`, `RemoveTag(bitmask, tag)`

2. **Synergy evaluation stub** (`GL2Project/Inventory/SynergyEvaluator.cs`):
   - Placeholder for future Relic/Modifier system
   - `EvaluateSynergies(List<ItemId> items) → List<SynergyEffect>` (stub for now)
   - Will be expanded in later milestones

3. **Tag context triggers** (future):
   - Context enum: `OnPickup`, `OnUse`, `OnCombatStart`, `OnCombatEnd`
   - Tag definitions can specify which contexts they trigger in
   - For M2-M3, we just store tags, evaluation happens later

## 3 Edge Cases to Test

1. **Tag bitmask operations**: Adding/removing tags from bitmask should correctly set/unset bits
2. **Multiple tags on item**: Item with Fire + Ice tags should have both bits set in bitmask
3. **Tag evaluation order**: When multiple items have tags, synergy evaluation should process in consistent order (alphabetical by ItemId)

