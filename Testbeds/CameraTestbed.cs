using Microsoft.Xna.Framework;
using GL2Engine.ECS;
using GL2Engine.Render;

namespace GL2Engine.Testbeds;

/// <summary>
/// Testbed for camera volumes and transitions. Creates multiple camera volumes and shows debug overlay.
/// </summary>
public class CameraTestbed
{
  private GameWorld _world;
  private CameraSystem _cameraSystem;
  private CameraVolume[] _volumes;
  private Microsoft.Xna.Framework.Graphics.SpriteFont? _debugFont;

  public CameraTestbed(GameWorld world, Microsoft.Xna.Framework.Graphics.SpriteFont? debugFont = null)
  {
    _world = world;
    _cameraSystem = new CameraSystem(world);
    _debugFont = debugFont;
    
    // Create test camera volumes
    _volumes = new CameraVolume[]
    {
      new CameraVolume
      {
        Bounds = new Rectangle(0, 0, 640, 360),
        CameraOffset = Vector2.Zero,
        AllowVerticalFollow = true,
        VolumeId = 1
      },
      new CameraVolume
      {
        Bounds = new Rectangle(640, 0, 640, 180), // Small vertical room
        CameraOffset = Vector2.Zero,
        AllowVerticalFollow = false, // No vertical follow in small room
        VolumeId = 2
      },
      new CameraVolume
      {
        Bounds = new Rectangle(1280, 0, 960, 540),
        CameraOffset = new Vector2(0, -20), // Slight upward offset
        AllowVerticalFollow = true,
        VolumeId = 3
      }
    };
  }

  public CameraSystem GetCameraSystem() => _cameraSystem;

  public CameraVolume[] GetVolumes() => _volumes;

  /// <summary>
  /// Sets the debug font for text rendering. If null, text won't be drawn.
  /// </summary>
  public void SetDebugFont(Microsoft.Xna.Framework.Graphics.SpriteFont? font)
  {
    _debugFont = font;
  }

  /// <summary>
  /// Creates camera volume entities in the world for testing.
  /// </summary>
  public void CreateVolumeEntities()
  {
    foreach (var volume in _volumes)
    {
      var entity = _world.CreateEntity();
      
      // Add position component (optional, for debug visualization)
      _world.Positions.Add(entity, new Position 
      { 
        Value = new Vector2(volume.Bounds.X + volume.Bounds.Width / 2, volume.Bounds.Y + volume.Bounds.Height / 2) 
      });
      
      // Add CameraVolumeComponent - this is what CameraSystem queries
      _world.CameraVolumes.Add(entity, new CameraVolumeComponent
      {
        Bounds = volume.Bounds,
        CameraOffset = volume.CameraOffset,
        AllowVerticalFollow = volume.AllowVerticalFollow,
        VolumeId = volume.VolumeId
      });
    }
  }

  /// <summary>
  /// Draws debug visualization of camera volumes and camera state.
  /// </summary>
  public void DrawDebug(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Camera camera, CameraController controller)
  {
    var graphicsDevice = spriteBatch.GraphicsDevice;
    
    // Create a 1x1 white pixel texture for drawing (create once, reuse)
    Microsoft.Xna.Framework.Graphics.Texture2D? pixelTexture = null;
    if (graphicsDevice != null)
    {
      pixelTexture = new Microsoft.Xna.Framework.Graphics.Texture2D(graphicsDevice, 1, 1);
      pixelTexture.SetData(new[] { Microsoft.Xna.Framework.Color.White });
    }
    
    if (pixelTexture == null) return;

    var activeVolume = controller.GetActiveVolume();
    var cameraCurrentPos = controller.GetCurrentPosition();
    var cameraTargetPos = controller.GetTargetPosition();
    var cameraViewport = camera.GetViewportBounds();
    var font = GetDebugFont(spriteBatch); // Get font once for reuse

    // Draw all camera volumes as wireframe rectangles
    foreach (var volume in _volumes)
    {
      bool isActive = activeVolume.HasValue && activeVolume.Value.VolumeId == volume.VolumeId;
      Color volumeColor = isActive ? Color.Yellow : Color.Green;
      float alpha = isActive ? 0.6f : 0.3f;
      
      // Draw volume bounds as wireframe
      DrawWireframeRectangle(spriteBatch, pixelTexture, volume.Bounds, volumeColor * alpha, 2);
      
      // Draw volume ID label
      if (font != null)
      {
        var labelPos = new Vector2(volume.Bounds.X + 5, volume.Bounds.Y + 5);
        spriteBatch.DrawString(font, $"Vol {volume.VolumeId}", labelPos, volumeColor);
        
        // Draw volume properties
        if (!volume.AllowVerticalFollow)
        {
          spriteBatch.DrawString(font, "No Vert", new Vector2(volume.Bounds.X + 5, volume.Bounds.Y + 20), Color.Orange);
        }
        if (volume.CameraOffset != Vector2.Zero)
        {
          spriteBatch.DrawString(font, $"Offset: {volume.CameraOffset}", new Vector2(volume.Bounds.X + 5, volume.Bounds.Y + 35), Color.Cyan);
        }
      }
    }

    // Draw camera viewport bounds
    DrawWireframeRectangle(spriteBatch, pixelTexture, cameraViewport, Color.Red * 0.5f, 2);

    // Draw camera current position (center of viewport)
    var cameraCenter = new Vector2(cameraCurrentPos.X + cameraViewport.Width / 2, cameraCurrentPos.Y + cameraViewport.Height / 2);
    DrawCircle(spriteBatch, pixelTexture, cameraCenter, 8, Color.Red, 2);

    // Draw camera target position
    var targetCenter = new Vector2(cameraTargetPos.X + cameraViewport.Width / 2, cameraTargetPos.Y + cameraViewport.Height / 2);
    DrawCircle(spriteBatch, pixelTexture, targetCenter, 6, Color.Blue, 2);

    // Draw text labels for camera positions
    if (font != null)
    {
      // Camera current position label
      spriteBatch.DrawString(font, $"Cam: ({cameraCurrentPos.X:F0}, {cameraCurrentPos.Y:F0})", 
        new Vector2(cameraCenter.X + 12, cameraCenter.Y - 8), Color.Red);
      
      // Camera target position label
      spriteBatch.DrawString(font, $"Target: ({cameraTargetPos.X:F0}, {cameraTargetPos.Y:F0})", 
        new Vector2(targetCenter.X + 12, targetCenter.Y - 8), Color.Blue);
      
      // Active volume info
      if (activeVolume.HasValue)
      {
        spriteBatch.DrawString(font, $"Active Volume: {activeVolume.Value.VolumeId}", 
          new Vector2(10, 10), Color.Yellow);
      }
      else
      {
        spriteBatch.DrawString(font, "No Active Volume", 
          new Vector2(10, 10), Color.Gray);
      }
    }

    // Draw look-ahead vector (from current to target)
    var lookAheadVector = cameraTargetPos - cameraCurrentPos;
    if (lookAheadVector.LengthSquared() > 0.1f)
    {
      DrawArrow(spriteBatch, pixelTexture, cameraCenter, targetCenter, Color.Cyan * 0.7f, 2);
    }

    // Draw player position if available
    if (_world.Positions.Has(_world.PlayerEntity))
    {
      var playerPos = _world.Positions.Get(_world.PlayerEntity).Value;
      DrawCircle(spriteBatch, pixelTexture, playerPos, 10, Color.Magenta, 2);
      
      // Draw line from player to camera target
      DrawLine(spriteBatch, pixelTexture, playerPos, targetCenter, Color.Magenta * 0.5f, 1);
      
      // Draw player position label
      if (font != null)
      {
        spriteBatch.DrawString(font, $"Player: ({playerPos.X:F0}, {playerPos.Y:F0})", 
          new Vector2(playerPos.X + 12, playerPos.Y - 8), Color.Magenta);
        
        // Draw look-ahead distance
        var lookAheadDistance = lookAheadVector.Length();
        spriteBatch.DrawString(font, $"Look-ahead: {lookAheadDistance:F1}px", 
          new Vector2(10, 30), Color.Cyan);
      }
    }

    // Draw active volume highlight
    if (activeVolume.HasValue)
    {
      var activeBounds = activeVolume.Value.Bounds;
      DrawWireframeRectangle(spriteBatch, pixelTexture, activeBounds, Color.Yellow * 0.8f, 3);
    }
  }

  private void DrawWireframeRectangle(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Graphics.Texture2D pixel, Rectangle rect, Color color, int thickness)
  {
    // Top
    spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
    // Bottom
    spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
    // Left
    spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
    // Right
    spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
  }

  private void DrawLine(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Graphics.Texture2D pixel, Vector2 start, Vector2 end, Color color, int thickness)
  {
    Vector2 direction = end - start;
    float length = direction.Length();
    if (length < 0.1f) return;

    float angle = MathF.Atan2(direction.Y, direction.X);
    spriteBatch.Draw(
      pixel,
      new Rectangle((int)start.X, (int)start.Y, (int)length, thickness),
      null,
      color,
      angle,
      Vector2.Zero,
      Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
      0f
    );
  }

  private void DrawCircle(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Graphics.Texture2D pixel, Vector2 center, float radius, Color color, int thickness)
  {
    // Draw circle as a filled circle (approximation with multiple lines)
    int segments = 16;
    Vector2 prevPoint = center + new Vector2(radius, 0);
    
    for (int i = 1; i <= segments; i++)
    {
      float angle = (i / (float)segments) * MathF.PI * 2f;
      Vector2 nextPoint = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
      DrawLine(spriteBatch, pixel, prevPoint, nextPoint, color, thickness);
      prevPoint = nextPoint;
    }
  }

  private void DrawArrow(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Graphics.Texture2D pixel, Vector2 start, Vector2 end, Color color, int thickness)
  {
    // Draw line
    DrawLine(spriteBatch, pixel, start, end, color, thickness);
    
    // Draw arrowhead
    Vector2 direction = Vector2.Normalize(end - start);
    Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
    float arrowSize = 8f;
    
    Vector2 arrowTip1 = end - direction * arrowSize + perpendicular * arrowSize * 0.5f;
    Vector2 arrowTip2 = end - direction * arrowSize - perpendicular * arrowSize * 0.5f;
    
    DrawLine(spriteBatch, pixel, end, arrowTip1, color, thickness);
    DrawLine(spriteBatch, pixel, end, arrowTip2, color, thickness);
  }

  private Microsoft.Xna.Framework.Graphics.SpriteFont? GetDebugFont(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
  {
    return _debugFont;
  }
}

