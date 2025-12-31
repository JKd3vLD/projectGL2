# Modifying Movement Tuning

Movement tuning values are stored in `Tuning/MovementTuning.json`.

## File Structure

The tuning file contains:
- **ground**: Ground movement parameters
- **jump**: Jump mechanics
- **gravity**: Gravity acceleration
- **terminalVelocity**: Maximum fall speed
- **glide**: Glide mechanics
- **cartwheel**: Cartwheel mechanics
- **slopes**: Slope behavior
- **teamUp**: Team-up throw mechanics

## Units

- **Speeds**: Pixels per second
- **Accelerations**: Pixels per second squared
- **Times**: Seconds (converted from frames)
- **Angles**: Degrees

## SNES Conversion Notes

Original DKC2 values are in SNES 8.8 fixed-point format:
- `$0140` = 320 decimal = 1.25 fixed-point
- At 60Hz: 320 subpixels/frame = 320/256 = 1.25 pixels/frame
- Converted to 120Hz: multiply by 2 for frame-based values

## Modifying Values

1. Edit `Tuning/MovementTuning.json`
2. Restart the game to load new values
3. Values are loaded at runtime by `PlayerControllerSystem`

## Key Parameters

### Walk Speed
- Default: 80 pixels/second
- Controls maximum horizontal speed while walking

### Jump Initial Velocity
- Default: -320 pixels/second (negative = up)
- Controls jump height

### Coyote Time
- Default: 0.1 seconds (12 frames at 120Hz)
- Window after leaving ground where jump still works

### Jump Buffer
- Default: 0.15 seconds (18 frames at 120Hz)
- Window before landing where jump input is buffered

### Gravity
- Default: 1200 pixels/second²
- Controls fall acceleration

### Terminal Velocity
- Default: 600 pixels/second
- Maximum fall speed

