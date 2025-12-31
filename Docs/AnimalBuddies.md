# Animal Buddy System

The Animal Buddy system allows players to mount or transform into various Animal Buddies from the Donkey Kong Country series.

## Mount vs Transformation

### Mount System
- **Activation**: Press **E** when near an Animal Buddy
- **Behavior**: Player rides the buddy, controlling its movement
- **Dismount**: Press **E** again (damage does NOT dismount)
- **Controls**: Player inputs control the buddy's movement

### Transformation System
- **Activation**: Touch a buddy barrel (collision-based)
- **Behavior**: Player transforms into the buddy, replacing normal movement
- **Transform Back**: Hit a stop sign or leave the designated area
- **Controls**: Player inputs control the transformed buddy directly

## Animal Buddy Control Schemas

### DKC2 Buddies

#### Rattly (Rattlesnake)
- **High Jump**: 3x normal jump height
- **Bounce Attack**: Press **Down** in air to bounce downward on enemies
- **Speed**: 1.5x faster ground movement
- **Restrictions**: Cannot glide

#### Squawks (Parrot)
- **Flight**: Hold **Jump** to fly upward (release to fall)
- **Egg Attack**: Press **X** to shoot egg projectile
- **Speed**: 0.8x slower ground movement
- **Special**: Can carry partner/items

#### Glimmer (Anglerfish)
- **Underwater Only**: Must be in water to function
- **Light Source**: Illuminates dark areas (radius-based)
- **Swimming**: 1.2x faster underwater movement
- **Attack**: Can attack underwater enemies

#### Squitter (Spider)
- **Web Platforms**: Press **Down + Jump** to create temporary web platform
- **Web Attack**: Press **X** to shoot web projectile
- **Wall Climbing**: Can climb walls (handled by physics system)
- **Movement**: Normal speed otherwise

#### Clapper (Seal)
- **Underwater Only**: Must be in water
- **Fast Swimming**: 2x faster than normal swimming
- **Ice Sliding**: Can slide on ice surfaces
- **Barrier Breaking**: Can break underwater barriers

### DKC1 Buddies

#### Rambi (Rhinoceros)
- **Charge Attack**: Press **X** to charge forward (breaks walls/enemies)
- **Speed**: 2x faster ground movement
- **Jump**: Normal jump height

#### Enguarde (Swordfish)
- **Underwater Only**: Must be in water
- **Dash Attack**: Press **X** to dash in facing direction (defeats enemies)
- **Speed**: 1.5x faster underwater movement

#### Winky (Frog)
- **High Jump**: 2.5x normal jump height
- **Bounce**: Can bounce on enemies by landing on them
- **Speed**: Normal ground movement

#### Expresso (Ostrich)
- **Fast Run**: 2.5x faster ground movement
- **Glide**: Can glide over obstacles (similar to player glide)
- **Restrictions**: Cannot attack enemies directly

### DKC3 Buddies

#### Ellie (Elephant)
- **Water Spray**: Press **X** to spray water projectile at enemies
- **Barrel Carry**: Can carry barrels (future feature)
- **Speed**: Normal ground movement

#### Nibbla (Fish)
- **Underwater Only**: Must be in water
- **Swimming**: 1.3x faster underwater movement
- **Basic**: Simple underwater buddy

#### Quawks (Purple Parrot)
- **Merged with Squawks**: Uses same controls as Squawks
- **Carry**: Can carry Kongs and items

### Unused/New Buddies

#### Hooter (Owl)
- **Flight**: Similar to Squawks, can fly
- **Night Vision**: Enhanced visibility in dark areas
- **Speed**: Normal ground movement

#### Miney (Mole)
- **Dig**: Press **Down + X** to dig into ground
- **Underground Movement**: Can move underground (future feature)
- **Speed**: 0.8x slower ground movement

## Implementation Details

### Component Structure
- `AnimalBuddy`: Stores buddy type, state, rider entity, and cooldown timers
- `AnimalBuddyMount`: Tracks which buddy is mounted by player
- `AnimalBuddyTransformation`: Tracks transformation state and timer

### System Integration
- `AnimalBuddySystem`: Handles mount/transform logic and buddy-specific controls
- `PlayerControllerSystem`: Checks for mount/transform before normal movement
- `PhysicsSystem`: Handles buddy-specific physics (wall climbing, web platforms)

### Control Override
When mounted or transformed, `PlayerControllerSystem` skips normal movement processing and `AnimalBuddySystem` handles all input.

## Usage Examples

### Mounting Rattly
1. Approach Rattly buddy entity
2. Press **E** to mount
3. Use normal movement controls (Rattly moves faster and jumps higher)
4. Press **E** again to dismount

### Transforming with Squawks
1. Touch Squawks barrel entity
2. Player transforms into Squawks
3. Hold **Space** to fly upward
4. Press **X** to shoot eggs
5. Hit stop sign to transform back

### Creating Web Platforms (Squitter)
1. Mount or transform into Squitter
2. Press **Down + Jump** to create web platform
3. Platform lasts for limited time
4. Can create multiple platforms (with cooldown)

## Tuning

Buddy-specific tuning values are stored in `AnimalBuddySystem.InitializeBuddyTuning()`:
- Jump multipliers
- Speed multipliers
- Ability flags (can glide, fly, climb walls, etc.)
- Cooldown timers for abilities

