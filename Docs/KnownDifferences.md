# Known Differences vs DKC2

This document tracks intentional and unintentional differences from the original DKC2 behavior.

## Intentional Differences

1. **120Hz Simulation**: DKC2 ran at 60Hz, this engine runs at 120Hz for smoother gameplay (with optional 30Hz SNES mode)
2. **3D Rendering**: DKC2 used 2D sprites, this engine uses 3D-rendered blocks with orthographic camera
3. **Simplified Physics**: Some edge cases and platform-specific behaviors are simplified
4. **Animal Buddy Expansion**: Includes all DKC1, DKC2, DKC3 buddies plus unused concepts

## Completed Features

1. **Slope Collision**: Full slope segment collision with line segment intersection implemented
2. **Cartwheel**: Ground cartwheel input implemented (E key)
3. **Moving Platforms**: Player riding and conveyor belts implemented
4. **Enemy Collision**: Enemy system with damage, invulnerability frames, and hitstun implemented
5. **Water Physics**: Swimming mechanics with buoyancy and drag implemented
6. **Animation System**: Basic animation system with clip registry implemented
7. **Level Loading**: Level data loading with collision world population implemented
8. **Animal Buddy System**: Mount and transformation mechanics with all buddy types implemented

## Movement Tuning Accuracy

Movement values are extracted from DKC2 disassembly but may need fine-tuning:
- Some values are estimated from common DKC2 behavior
- Conversion from SNES fixed-point to MonoGame units may need adjustment
- Frame-based timings converted from 60Hz to 120Hz

## Future Improvements

- Enhanced animation blending and state machines
- More sophisticated enemy AI
- Additional level geometry types
- Particle effects for abilities
- Sound effects integration
- More detailed buddy ability implementations (projectiles, web platforms, etc.)
