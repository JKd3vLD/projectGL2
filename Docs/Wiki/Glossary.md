# Glossary

**Purpose**: Definitions of key terms used throughout GL2 Engine documentation.

## Core Terms

**Tier**: A progression level containing 7 stages. Each tier has its own seed namespace. Players advance tiers by completing stages and meeting mastery requirements.

**Stage**: A playable level assembled from handcrafted sections. Each stage belongs to a tier and has a biome signature (A, B, C, or combinations).

**Section**: A handcrafted level chunk with metadata (pacing tag, biome tags, difficulty, traversal mode). Sections are assembled into stages by `StageAssembler`.

**StagePlan**: The assembled plan for a stage, containing ordered sections, side pockets (for SLOW stages), flag positions, reward profile, and difficulty ramp assignments.

**PacingTag**: Stage pacing classification: `FAST` (speed/flow pressure) or `SLOW` (exploration/precision). Player choice, never blocks progression.

**BiomeSignature**: Bit flags indicating which biomes (A, B, C) are present in a stage. Used for section filtering and visual theming.

**SeedBadge**: Saved seed combination for a tier, stored on mastery trophy. Allows replaying exact tier layout.

**Flag**: Checkpoint marker in a stage. Types: Start (spawn point), Middle (checkpoint, consumable), End (completion). Used for respawn, fast travel, and consumable rewards.

**XP**: Experience points/currency collected from pickups. Used for reward thresholds. Lost on death (half dropped at last safe flag).

**Coins**: [Later] Collectible currency separate from XP.

**Stars**: Difficulty rating (1-5) assigned to sections. Used for difficulty ramp (Teach=1, Test=2-3, Twist=3-4, Finale=4-5).

**Flow Meter**: Stage-local meter that fills from pacing-appropriate actions, affects bonus rewards. Player-facing term (never "score"). Resets at stage start, stored in Trophy as summary.

**Run Value (RV)**: Internal aggregate used for reward threshold progress, bonus payout scaling, trophy stamping. Never exposed as "score" to players.

**Reward Quality**: Player-facing term for what you earned (replaces "score"). Discrete tier (0-5) shown in UI.

**Flow Grade**: Letter grade (D/C/B/A/S) based on Flow Meter final value. Calculated using FlowGradeThresholds.

**Flow Event**: Typed event emitted by systems to update Flow Meter. Events: TimeTierHit, Chain, SecretFound, CarryDelivered, BonusComplete, DamageTaken, IdleTick, BacktrackTick.

**Technique Mod**: Run-scoped relic that adds contextual bonuses without changing movement parameters. Effects apply through event hooks (OnGlideStart/End, OnCartwheelStart/End, etc.).

**Rule Modifier**: Toggle that changes run/stage rules (Assist or Challenge). Never blocks progression, only affects reward multipliers and eligible content pools.

**Trophy Stamp**: Immutable snapshot of stage/tier completion results. Includes Flow Grade, time tier, secrets found, seed badge hash. Persists across game over.

## Technical Terms

**RNG Stream**: Separate deterministic random number generator for different systems. Streams: WorldGen (layout), Reward (loot), Bonus (bonus doors), Collectible (pickups).

**Tier-scoped Seed**: Seed resolution includes tier index, ensuring same symbol codes produce different results across tiers.

**Section Pool**: Filtered collection of available sections matching tier, biome signature, and pacing tag. Maintains history to prevent repeats.

**Difficulty Ramp**: Kishotenketsu structure: Teach (introduces mechanics) → Test (standard challenge) → Twist (variation) → Finale (peak difficulty).

**Connector**: Section endpoint type (e.g., "ground", "air", "water"). Ensures compatible section chaining.

**Side Pocket**: Optional exploration section added to SLOW stages. Provides bonus content without blocking main path.

**Consumable Flag**: Middle checkpoint flag that can be consumed for rewards (Shovel Knight style). Consumed flags disappear but provide bonus rewards.

**Reward Threshold**: XP milestone that triggers Hades-style reward selection (3 options from loot table).

**Death Drop**: XP lost on death (half of current) dropped as collectible at last safe flag position. Can be retrieved.

**Reward Profile**: Stage reward type: SPEED (FAST stages), TREASURE (SLOW exploration), QUEST (SLOW quest items), MIXED (combination).

**Mod Pack**: Content package in `/Mods/<PackName>/` containing JSON definitions for sections, items, blocks, biomes, rooms.

**Deterministic Merge**: Content pack loading order (alphabetical) ensures stable content database regardless of file system order.

## Architecture Terms

**GameWorld**: Singleton owning ECS component stores, event bus, RNG streams, content database. Lives in `GL2Project/ECS/World.cs`.

**SoA**: Structure of Arrays - component storage pattern where each component type is stored in a separate array, improving cache locality.

**Ring Buffer**: Fixed-size circular buffer for events. No allocations, overwrites oldest entries when full.

**Fixed-Step Simulation**: Deterministic update loop running at fixed 120 Hz (or 30 Hz SNES mode). Variable render step decoupled.

**Component Store**: SoA array storage for a component type. Provides `Add`, `Remove`, `Get`, `Has`, `GetActiveEntities` methods.

**Typed Event Bus**: Event system using ring buffers per event type. No per-frame allocations, deterministic processing order.

