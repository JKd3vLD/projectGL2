# GL2 Engine - Internal Design Wiki

**Purpose**: Technical documentation for GL2 Engine mechanics, systems, and design decisions. Implementation-focused for MonoGame/.NET 10/C#14 DOD/ECS architecture.

## What's Implemented Now (M2-M3 Foundation)

### Core Systems ✅
- **Fixed-step simulation**: 120 Hz deterministic update loop
- **ECS architecture**: Data-Oriented Design with Structure of Arrays (SoA) component storage
- **Typed event bus**: Ring-buffer based, no per-frame allocations
- **Movement system**: DKC2-inspired movement with tuning from disassembly
- **Physics2D**: 2D collision on 3D world, slope support, seam handling

### World Generation ✅
- **Tier Package system**: 7 stages from 3 biomes (A, B, C, AB, BC, CA, ABC mastery)
- **Section-based assembly**: Handcrafted sections assembled into stages
- **FAST/SLOW pacing**: Player choice between speed-focused and exploration-focused stages
- **Difficulty ramp**: Teach→Test→Twist→Finale structure
- **Section pool**: History-based anti-repeat system

### Economy & Progression ✅
- **XP/Currency system**: Souls-like collection with death drops
- **Reward thresholds**: Hades-style reward selection at XP milestones
- **Flag system**: Start/middle/end checkpoints with fast travel
- **Consumable flags**: Shovel Knight-style flag consumption for rewards

### Seeds & RNG ✅
- **Tier-scoped seeds**: Symbol codes per tier, deterministic across runs
- **Separate RNG streams**: WorldGen, Reward, Bonus, Collectible
- **Seed selection UI**: Player chooses seed codes when advancing tiers

### Content & Modding ✅
- **Mod pack system**: JSON-based content packs in `/Mods/`
- **Section loader**: Loads sections from base game and mod packs
- **Deterministic merge**: Alphabetical ordering for stable content loading

### Rendering ✅
- **3D pixelated style**: Low-res render target with nearest-neighbor upscale
- **Layered rendering**: Background/Mid/Foreground passes with parallax
- **Camera system**: DKC2 + Celeste-inspired with look-ahead and damping

### Player Systems [Later]
- **Flow Meter System**: Stage-local meter that fills from pacing-appropriate actions, affects bonus rewards via multiplier. Never modifies movement parameters.
- **Technique Mods**: Run-scoped relics that add contextual bonuses (Flow, Reward Quality) without changing baseline movement.
- **Rule Modifiers**: Assist + Challenge framework with reward multipliers and stage filtering.
- **Trophy Stamps**: Immutable snapshots of stage/tier completion with Flow Grade, seed badge, and key metrics.

## Quick Links

### Core Mechanics
- [Movement Spec](Movement-Spec) - Authoritative movement mechanics
- [Core Loop & Run Structure](Core-Loop-and-Run-Structure) - Game flow and run structure
- [Tier System](Tier-System) - Tier progression and package generation

### Stage Generation
- [Stage Generation Overview](Stage-Generation-Overview) - How stages are assembled
- [FAST vs SLOW Stages](FAST-vs-SLOW-Stages) - Pacing system and player choice
- [Section-Based Assembly](Section-Based-Assembly) - Section pool and assembly rules

### Systems
- [Currency & XP System](Currency-and-XP-System) - XP collection, death drops, rewards, Run Value (RV)
- [Flag System](Flag-System) - Checkpoints, fast travel, consumable flags
- [Seeds & RNG](Seeds-and-RNG) - Tier-scoped seeds and RNG streams
- [Flow Meter System](Flow-Meter-System) - Stage-local Flow Meter, affects bonus rewards
- [Technique Mods System](Technique-Mods-System) - Run-scoped relics with contextual bonuses
- [Rule Modifiers System](Rule-Modifiers-System) - Assist + Challenge framework
- [Trophy Stamps System](Trophy-Stamps-System) - Stage/tier completion snapshots

### Technical
- [Architecture Overview](Architecture-Overview) - ECS, GameWorld, deterministic ordering
- [Physics2D & Slopes](Physics2D-and-Slopes) - 2D collision on 3D world, slope mechanics
- [Rendering Pipeline](Rendering-Pipeline) - 3D pixelated rendering, layering, parallax

## Key Design Invariants

1. **Fixed core movement kit**: Never permanently altered. Tools/props can extend movement temporarily with counters.
2. **Tier-scoped seeds**: Seeds are per-tier namespaced. Same symbol codes in Tier 1 ≠ Tier 2.
3. **Separate RNG streams**: Layout generation never consumes reward RNG. Streams: WorldGen, Reward, Bonus, Collectible.
4. **Game Over regression**: Always regress to `max(1, HighestTierReached - 3)` with random seeds. Lives reset to 3.
5. **FAST vs SLOW choice**: Always player choice, never blocks progression. Both paths available.
6. **No per-frame allocations**: Fixed update loop targets zero allocations (ring buffers, SoA storage).
7. **Flow Meter never modifies movement**: Flow Meter only affects rewards via multiplier. Never changes Run/Jump/Glide physics values.
8. **No "score" terminology**: Use "Run Value" (RV) internally, "Flow Meter" or "Reward Quality" player-facing. Never say "+score".

## File Structure Reference

- **GameWorld**: `GL2Project/ECS/World.cs`
- **Components**: `GL2Project/ECS/Components.cs`
- **Movement Tuning**: `GL2Project/Tuning/MovementTuning.json`
- **Stage Tuning**: `GL2Project/Tuning/StageGenerationTuning.json`
- **Section Defs**: `GL2Project/World/SectionDef.cs`
- **Stage Assembler**: `GL2Project/World/StageAssembler.cs`
- **Currency System**: `GL2Project/Gameplay/CurrencySystem.cs`
- **Flag System**: `GL2Project/Gameplay/FlagSystem.cs`

## MVP Status

- **M0**: ✅ Core movement, physics, rendering
- **M1**: ✅ Player controller, team-up mechanics, basic level loading
- **M2-M3**: ✅ Section-based world generation, FAST/SLOW pacing, currency/XP, flags, mod packs
- **M4+**: 🔜 Full content pipeline, editor tools, advanced modding

See [MVP Roadmap](MVP-Roadmap) for details.

