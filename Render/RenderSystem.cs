using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GL2Engine.ECS;

namespace GL2Engine.Render;

/// <summary>
/// Render system - draws all renderable entities.
/// </summary>
public class RenderSystem
{
  private GameWorld _world;
  private BasicEffect _basicEffect;
  private GraphicsDevice _device;

  public RenderSystem(GameWorld world)
  {
    _world = world;
  }

  public void SetDevice(GraphicsDevice device)
  {
    Initialize(device);
  }

  public void Initialize(GraphicsDevice device)
  {
    _device = device;
    _basicEffect = new BasicEffect(device);
    _basicEffect.VertexColorEnabled = true;
    _basicEffect.World = Matrix.Identity;
  }

  public void Draw(RenderPipeline pipeline)
  {
    if (_device == null) return;

    var camera = pipeline.Camera;
    
    // Update effect matrices
    if (_basicEffect != null)
    {
      _basicEffect.View = camera.ViewMatrix;
      _basicEffect.Projection = camera.ProjectionMatrix;
    }

    // Draw in layer order: Background -> Mid -> Foreground
    DrawLayer(pipeline, RenderLayer.BackgroundLayer, camera);
    DrawLayer(pipeline, RenderLayer.MidLayer, camera);
    DrawLayer(pipeline, RenderLayer.ForegroundLayer, camera);
  }

  private void DrawLayer(RenderPipeline pipeline, RenderLayer layer, Camera camera)
  {
    // Apply parallax offset based on layer
    Vector2 parallaxOffset = GetParallaxOffset(layer, camera);

    // Efficiently query all Renderable components for this layer
    // Use SoA iteration pattern: iterate by index for cache efficiency
    var renderables = _world.Renderables;
    var positions = _world.Positions;
    
    // Collect entities in this layer with their render data
    // We'll sort by Z depth for proper rendering order
    var entitiesToDraw = new System.Collections.Generic.List<(Entity entity, Vector2 position, Renderable renderable)>();
    
    // Iterate through all Renderable components efficiently
    for (int i = 0; i < renderables.Count; i++)
    {
      var renderable = renderables.GetByIndex(i);
      
      // Filter by layer
      if (renderable.Layer != layer)
        continue;
      
      Entity entity = renderables.GetEntity(i);
      
      // Only draw entities that have a position component
      if (!positions.Has(entity))
        continue;
      
      var position = positions.Get(entity);
      entitiesToDraw.Add((entity, position.Value, renderable));
    }
    
    // Sort by Z depth (lower Z = drawn first, higher Z = drawn on top)
    entitiesToDraw.Sort((a, b) => a.renderable.Z.CompareTo(b.renderable.Z));
    
    // Draw all entities in this layer
    foreach (var (entity, position, renderable) in entitiesToDraw)
    {
      // Apply parallax offset (can be overridden by per-entity ParallaxFactor)
      float entityParallaxFactor = renderable.ParallaxFactor >= 0 ? renderable.ParallaxFactor : 1.0f;
      Vector2 entityParallaxOffset = parallaxOffset * entityParallaxFactor;
      
      // Calculate size (placeholder - will use MeshId later)
      Vector2 size = new Vector2(32, 48); // Default size
      
      // Determine color based on entity type (placeholder)
      Color color = Color.White;
      if (entity.Id == _world.PlayerEntity.Id)
        color = Color.Red;
      else if (entity.Id == _world.PartnerEntity.Id)
        color = Color.Blue;
      
      DrawEntity(_device!, position + entityParallaxOffset, size, color);
    }
  }

  private Vector2 GetParallaxOffset(RenderLayer layer, Camera camera)
  {
    // Parallax factors: background moves slower, foreground moves faster
    float parallaxFactor = layer switch
    {
      RenderLayer.BackgroundLayer => 0.3f, // Background moves 30% of camera
      RenderLayer.MidLayer => 1.0f,        // Mid layer moves with camera (no parallax)
      RenderLayer.ForegroundLayer => 1.2f, // Foreground moves 120% of camera (slight forward)
      _ => 1.0f
    };

    // Calculate parallax offset based on camera position
    Vector2 cameraPos = camera.Position;
    return cameraPos * (parallaxFactor - 1.0f);
  }

  private void DrawEntity(GraphicsDevice device, Vector2 position, Vector2 size, Color color)
  {
    // Simple rectangle drawing (will be replaced with 3D meshes later)
    var vertices = new VertexPositionColor[4];
    var halfSize = size * 0.5f;

    vertices[0] = new VertexPositionColor(new Vector3(position.X - halfSize.X, position.Y - halfSize.Y, 0), color);
    vertices[1] = new VertexPositionColor(new Vector3(position.X + halfSize.X, position.Y - halfSize.Y, 0), color);
    vertices[2] = new VertexPositionColor(new Vector3(position.X + halfSize.X, position.Y + halfSize.Y, 0), color);
    vertices[3] = new VertexPositionColor(new Vector3(position.X - halfSize.X, position.Y + halfSize.Y, 0), color);

    var indices = new short[] { 0, 1, 2, 0, 2, 3 };

    if (_basicEffect != null)
    {
      foreach (var pass in _basicEffect.CurrentTechnique.Passes)
      {
        pass.Apply();
        device.DrawUserIndexedPrimitives(
          PrimitiveType.TriangleList,
          vertices, 0, 4,
          indices, 0, 2);
      }
    }
  }
}
