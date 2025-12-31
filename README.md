# GL2Project Engine

A minimal but playable MonoGame project implementing DKC2-inspired movement mechanics with a data-oriented ECS architecture, 120Hz fixed-step simulation, and 3D-rendered 2D gameplay world.

## Features

- **120Hz Fixed-Step Simulation**: Deterministic physics at 120Hz (with optional 30Hz SNES mode)
- **ECS Architecture**: Custom SoA (Structure of Arrays) component storage
- **DKC2 Movement**: Walk/run, variable jump, coyote time, jump buffer, cartwheel, glide
- **Slope Physics**: Support for slopes of any angle with sliding behavior
- **Team-Up Mechanics**: Partner pickup/throw with follow snap
- **Animal Buddy System**: Mount and transformation mechanics for all DKC1, DKC2, and DKC3 Animal Buddies
- **Moving Platforms**: Player riding and conveyor belts
- **Enemy System**: Collision, damage, invulnerability frames
- **Water Physics**: Swimming mechanics with buoyancy and drag
- **Animation System**: Basic 3D model animation playback
- **Pixel-Art Rendering**: Low-res render target with nearest-neighbor upscale
- **Movement Tuning**: Values extracted from DKC2 disassembly

## M2-M3 Foundation Features

- **Tier-Based World Generation**: TierPackage system with 3 biomes → 7 stages (A, B, C, AB, BC, CA, ABC mastery)
- **Tier-Scoped Seed System**: Deterministic RNG streams (WorldGen, Reward, Bonus, Collectible) with seed selection UI
- **Celeste-Like Camera**: Look-ahead, damping, camera volumes with smooth transitions
- **Inventory Framework**: Per-item capacity model, discovery-based catalog, loot tables
- **Movement Tools**: Grapple and balloon tools as contextual props (no permanent upgrades)
- **Modding Foundation**: Content pack system with JSON-based mods
- **Render Layering**: Background/Mid/Foreground layers with parallax

## Project Structure

```text
GL2Project/
├── Engine/          # Core engine systems (loop, timing, RNG, config, seed resolver)
├── ECS/            # Entity Component System (SoA storage)
├── Render/         # Rendering pipeline (RT, camera, shaders, animation, camera system)
├── Physics2D/      # 2D collision and physics (slopes, platforms, water)
├── Gameplay/       # Player controller, team-up, animal buddies, enemies, tools, game over
├── Content/        # Asset loading, level definitions, mod loader
├── Inventory/      # Inventory system, item catalog, loot tables
├── World/          # TierPackage, biome, stage, world generation
├── UI/             # Seed selection UI
├── Testbeds/       # Test scenes for worldgen, camera, inventory
├── Mods/           # Content packs (ExamplePack)
├── Debug/          # Debug overlay and utilities
├── Tuning/         # Movement tuning data (JSON)
├── Docs/           # Documentation (including Research findings)
└── Levels/         # Level data files (JSON)
```

## Building

1. Ensure .NET 10.0 SDK is installed
2. Install MonoGame 3.8.5
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run:
   ```bash
   dotnet run
   ```

## Controls

### Basic Movement
- **A/Left Arrow**: Move left
- **D/Right Arrow**: Move right
- **S/Down Arrow**: Crouch
- **Space**: Jump
- **E**: Cartwheel (ground) / Mount/Dismount Animal Buddy
- **T**: Transformation (when touching buddy barrel)

### Team-Up
- **E**: Pickup partner (when near)
- **Q**: Throw partner (neutral)
- **W/Up Arrow**: Throw partner (up)

### Animal Buddy Abilities
- **X**: Attack/Ability (buddy-specific)
- **Up + Jump**: Special abilities (Squitter web platform, etc.)

### Debug
- **F1**: Toggle framerate mode (120Hz / 30Hz SNES)

## Movement Features

- **Walk/Run**: Acceleration-based movement with friction
- **Variable Jump**: Hold for full height, release early to cut jump
- **Coyote Time**: Jump window after leaving ground
- **Jump Buffer**: Jump input buffered before landing
- **Cartwheel**: Ground cartwheel transitions to air state with special jump rules
- **Glide**: Retriggerable glide that clamps fall speed
- **Slopes**: Walk on any angle, slide on slopes >30°
- **Swimming**: Underwater movement with buoyancy and drag

## Animal Buddies

The engine supports all Animal Buddies from DKC1, DKC2, and DKC3, plus unused concepts:

### DKC2 Buddies
- **Rattly**: High jump (3x), bounce attack
- **Squawks**: Flight mode, egg attack
- **Glimmer**: Underwater, light source
- **Squitter**: Web platforms, web attack, wall climbing
- **Clapper**: Fast swimming

### DKC1 Buddies
- **Rambi**: Charge attack, break walls
- **Enguarde**: Underwater dash attack
- **Winky**: High jump, bounce on enemies
- **Expresso**: Fast run, glide

### DKC3 Buddies
- **Ellie**: Water spray attack
- **Nibbla**: Underwater buddy
- **Quawks**: Parrot variant (merged with Squawks)

### Unused/New
- **Hooter**: Owl with flight and night vision
- **Miney**: Mole with dig ability

See `Docs/AnimalBuddies.md` for detailed control schemas.

## Documentation

See `Docs/` directory for:
- `SystemUpdateOrder.md` - System execution order
- `AddingBlocks.md` - How to add blocks to levels
- `AddingSlopes.md` - How to add slope blocks
- `ModifyingTuning.md` - How to modify movement tuning
- `KnownDifferences.md` - Differences vs DKC2
- `AnimalBuddies.md` - Animal Buddy system documentation

## Tuning

Movement values are stored in `Tuning/MovementTuning.json` and extracted from DKC2 disassembly. Values are converted from SNES 8.8 fixed-point format to MonoGame units (pixels/second).

## License

See parent repository for license information.
