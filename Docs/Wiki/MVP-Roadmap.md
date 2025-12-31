# MVP Roadmap [MVP Core]

**Purpose**: Brief roadmap of MVP milestones M0-M3, showing what's implemented and what's planned.

## M0: Core Foundation ✅

**Status**: Complete

**Implemented**:
- Fixed 120 Hz simulation loop
- ECS architecture (SoA component stores)
- Typed event bus (ring buffers)
- Basic movement system (DKC2-inspired)
- Physics2D (AABB collision, slopes)
- Basic rendering (3D pixelated style)
- Level loading (JSON format)

## M1: Player & Basic Gameplay ✅

**Status**: Complete

**Implemented**:
- Player controller (walk, run, jump, glide, cartwheel)
- Team-up mechanics (pickup, throw, follow snap)
- Animal buddy system (mount, transform)
- Enemy system (basic AI, collision)
- Water system (swimming, buoyancy)
- Animation system
- Basic level editor support

## M2-M3: World Generation & Progression ✅

**Status**: Complete

**Implemented**:
- Tier package system (7 stages from 3 biomes)
- Section-based stage assembly
- FAST/SLOW pacing system
- Difficulty ramp (Teach→Test→Twist→Finale)
- Section pool with history-based anti-repeat
- Tier-scoped seeds with separate RNG streams
- Seed selection UI (placeholder)
- Currency/XP system with death drops
- Reward thresholds with Hades-style selection
- Flag system (start/middle/end, fast travel, consumable)
- Mod pack system (JSON-based content packs)
- Section loader (base game + mods)
- Camera system (look-ahead, damping, volumes)
- Rendering layers (Background/Mid/Foreground, parallax)

## M4: Content Pipeline [Later]

**Status**: Planned

**Planned**:
- Full content pipeline (sections, items, blocks, biomes)
- Level editor integration
- Section editor tool
- Visual section preview in UI
- Advanced modding support (scripts, custom behaviors)

## M5: Polish & Optimization [Later]

**Status**: Planned

**Planned**:
- Performance optimization (profiling, optimization passes)
- Visual polish (particles, effects, post-processing)
- Audio system integration
- Save/load system (persistent saves)
- Settings menu (graphics, audio, controls)

## Key Metrics

- **M0-M3**: Core systems, world generation, progression ✅
- **M4+**: Content tools, advanced features, polish 🔜

## Implementation Notes

**Current Focus**: M2-M3 foundation complete. Next: Content creation tools, advanced modding, polish.

**File References**: See individual system pages for implementation details.

**Tuning Files**: `GL2Project/Tuning/MovementTuning.json`, `GL2Project/Tuning/StageGenerationTuning.json`

