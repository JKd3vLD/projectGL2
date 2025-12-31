# Rendering Pipeline [MVP Core]

**Purpose**: 3D pixelated rendering style with low-res render target, nearest-neighbor upscale, layered passes (Background/Mid/Foreground), and parallax.

## Player-Facing Rules

- **Pixelated Style**: Game renders at low resolution (e.g., 320x180) and upscales to screen with nearest-neighbor filtering.
- **Layered Depth**: Background (far parallax), Mid (gameplay plane), Foreground (occluders + vignette).
- **Parallax**: Background layers move slower than camera for depth effect. Parallax factor per layer (0 = no parallax, 1 = full parallax).

## System Rules

- **Low-Res Render Target**: `RenderPipeline` creates render target at fixed low resolution (320x180 or similar).
- **Nearest-Neighbor Upscale**: Upscale to backbuffer using nearest-neighbor filtering (pixel-perfect, no smoothing).
- **Layered Rendering**: `RenderSystem.DrawLayer()` renders each layer separately:
  - BackgroundLayer: Far parallax, rendered first
  - MidLayer: Gameplay plane, rendered second
  - ForegroundLayer: Occluders + vignette, rendered last
- **Z-Depth Sorting**: Within each layer, sort by Z depth. Lower Z = rendered first (background).
- **Parallax Calculation**: `parallaxOffset = cameraPosition * parallaxFactor`. Background moves slower than camera.

## Data Model

**Renderable** (`GL2Project/ECS/Components.cs`):
- `MeshId`: int (reference to 3D mesh/model)
- `Z`: float (render depth, lower = background)
- `Layer`: RenderLayer (BackgroundLayer, MidLayer, ForegroundLayer)
- `ParallaxFactor`: float (0 = no parallax, 1 = full parallax)

**RenderLayer** (`GL2Project/ECS/Components.cs`):
- `BackgroundLayer`: Far parallax
- `MidLayer`: Gameplay plane
- `ForegroundLayer`: Occluders + vignette

**RenderPipeline** (`GL2Project/Render/RenderPipeline.cs`):
- `_renderTarget`: RenderTarget2D (low-res)
- `_backbuffer`: RenderTarget2D (screen resolution)
- `Camera`: Camera

**RenderSystem** (`GL2Project/Render/RenderSystem.cs`):
- `_world`: GameWorld
- `DrawLayer(layer, camera, spriteBatch)`: Renders all entities in layer

## Algorithms / Order of Operations

### Render Frame

1. **Begin Render**: `RenderPipeline.BeginRender(graphicsDevice)`
   - Set render target to low-res target
   - Clear to background color
2. **Render Layers** (in order):
   - `RenderSystem.DrawLayer(BackgroundLayer, camera, spriteBatch)`
   - `RenderSystem.DrawLayer(MidLayer, camera, spriteBatch)`
   - `RenderSystem.DrawLayer(ForegroundLayer, camera, spriteBatch)`
3. **Present**: `RenderPipeline.Present(graphicsDevice)`
   - Set render target to backbuffer
   - Draw low-res target scaled up with nearest-neighbor filtering
4. **Debug Overlay**: `DebugOverlay.Draw()` on top (full resolution)

### Layer Rendering

1. **Query Entities**: `GameWorld.Renderables.GetActiveEntities()` - Get all entities with Renderable
2. **Filter by Layer**: Keep only entities with matching `Layer`
3. **Sort by Z**: Sort entities by `Z` (ascending: background first)
4. **Render Each Entity**:
   - Calculate parallax offset: `offset = cameraPosition * renderable.ParallaxFactor`
   - Apply camera transform: `worldPosition - cameraPosition + offset`
   - Draw mesh/model at transformed position

### Parallax Calculation

1. **Camera Position**: Get current camera position
2. **Per-Layer Offset**: `parallaxOffset = cameraPosition * parallaxFactor`
3. **Background Layers**: `parallaxFactor < 1.0` - Move slower than camera
4. **Mid Layer**: `parallaxFactor = 1.0` - Move with camera (no parallax)
5. **Foreground Layers**: `parallaxFactor > 1.0` - Move faster than camera (future enhancement)

## Tuning Parameters

| Parameter | Type | Range | Default | Notes |
|-----------|------|-------|---------|-------|
| `renderTargetWidth` | int | 160-640 | 320 | Low-res render target width |
| `renderTargetHeight` | int | 90-360 | 180 | Low-res render target height |
| `backgroundParallaxFactor` | float | 0-1 | 0.5 | Background layer parallax |
| `midParallaxFactor` | float | - | 1.0 | Mid layer parallax (no parallax) |
| `foregroundParallaxFactor` | float | 1-2 | 1.2 | Foreground layer parallax |

## Edge Cases + Counters

- **Render target size mismatch**: Scale to maintain aspect ratio, letterbox if needed.
- **Z depth ties**: Render in entity creation order (stable but not ideal).
- **Parallax factor = 0**: Layer completely static (useful for UI overlays).
- **Parallax factor > 1**: Layer moves faster than camera (foreground effect).

## Telemetry Hooks

- Log render performance: `RenderFrame(frameTime, layerCounts, entityCounts, timestamp)` (optional, for profiling)
- Log parallax usage: `ParallaxLayer(layer, parallaxFactor, offset, timestamp)` (optional, for debugging)

## Implementation Notes

**File**: `GL2Project/Render/RenderPipeline.cs`, `GL2Project/Render/RenderSystem.cs`

**Key Systems**:
- `RenderPipeline`: Manages render targets, upscaling, camera
- `RenderSystem`: Renders entities by layer, handles parallax
- `CameraSystem`: Updates camera position for parallax calculation

**Deterministic Ordering**:
1. `RenderPipeline.BeginRender()` - Set low-res target
2. `RenderSystem.Draw()` - Render all layers
3. `RenderPipeline.Present()` - Upscale to backbuffer
4. `DebugOverlay.Draw()` - Draw debug info

**Component Stores**: `GameWorld.Renderables`, `GameWorld.Positions`

**Shader**: `GL2Project/Shaders/Unlit.fx` - Basic unlit shader for 3D meshes (MonoGame HLSL).

**Reference Image**: `/metroid-3d-pixel-environment-art-reference.png` - Guides render layering and lighting/post look.

