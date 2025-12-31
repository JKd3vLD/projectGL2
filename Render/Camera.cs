using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GL2Engine.Render;

/// <summary>
/// Orthographic camera with pixel snapping to avoid shimmering.
/// </summary>
public class Camera
{
  private int _width;
  private int _height;
  private Vector2 _position;
  private float _zoom = 1.0f;

  public Camera(int width, int height)
  {
    _width = width;
    _height = height;
    _position = Vector2.Zero;
  }

  public Vector2 Position
  {
    get => _position;
    set => _position = value;
  }

  public float Zoom
  {
    get => _zoom;
    set => _zoom = MathHelper.Clamp(value, 0.1f, 5.0f);
  }

  public Matrix ViewMatrix
  {
    get
    {
      // Pixel-snapped position to avoid shimmering
      Vector2 snappedPos = SnapToPixel(_position);
      return Matrix.CreateTranslation(-snappedPos.X, -snappedPos.Y, 0) * Matrix.CreateScale(_zoom, _zoom, 1);
    }
  }

  public Matrix ProjectionMatrix
  {
    get
    {
      return Matrix.CreateOrthographicOffCenter(0, _width, _height, 0, -1, 1);
    }
  }

  public Matrix ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;

  /// <summary>
  /// Snap world position to pixel grid for stable rendering.
  /// Entities remain at float precision internally.
  /// </summary>
  public Vector2 SnapToPixel(Vector2 worldPos)
  {
    return new Vector2(
      MathF.Floor(worldPos.X) + 0.5f,
      MathF.Floor(worldPos.Y) + 0.5f
    );
  }

  public void Follow(Vector2 target, float lerpSpeed = 1.0f)
  {
    _position = Vector2.Lerp(_position, target, lerpSpeed);
  }

  /// <summary>
  /// Gets the camera viewport bounds in world space.
  /// </summary>
  public Rectangle GetViewportBounds()
  {
    return new Rectangle(
      (int)_position.X,
      (int)_position.Y,
      _width,
      _height
    );
  }
}
