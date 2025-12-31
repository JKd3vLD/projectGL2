using Microsoft.Xna.Framework;
using GL2Engine.ECS;

namespace GL2Engine.Render;

/// <summary>
/// Camera controller based on DKC2 assembly analysis.
/// Uses DKC2's actual camera mechanics: look-ahead offsets, bounds averaging, and smoothing.
/// </summary>
public class CameraController
{
  private Vector2 _targetPosition;
  private Vector2 _currentPosition;
  private CameraVolume? _activeVolume;

  // DKC2 camera values (from assembly analysis)
  // Horizontal look-ahead offset: $0100 = 256 pixels (SNES subpixels)
  // Vertical look-ahead offset: $00E0 = 224 pixels (SNES subpixels)
  // Converted to our coordinate system (scaled for 120Hz vs 60Hz)
  private const float HorizontalLookAheadOffset = 256f; // $0100 in DKC2
  private const float VerticalLookAheadOffset = 224f;   // $00E0 in DKC2
  
  // DKC2 uses averaging (LSR = divide by 2) for smooth bounds transitions
  // Camera smoothing: averaging between current and target bounds
  private const float SmoothingFactor = 0.5f; // DKC2 uses divide by 2 (LSR A)
  
  // DKC2 viewport size (SNES): 256x224
  // Our virtual resolution: 320x240, but we'll use DKC2's aspect ratio feel
  private const float ViewportWidth = 320f;
  private const float ViewportHeight = 240f;
  
  // DKC2 camera bounds checking uses these offsets from player position
  private const float HorizontalDeadzone = 128f; // Half of horizontal offset
  private const float VerticalDeadzone = 112f;   // Half of vertical offset

  public CameraController()
  {
    _targetPosition = Vector2.Zero;
    _currentPosition = Vector2.Zero;
  }

  /// <summary>
  /// Updates camera position based on DKC2's camera system.
  /// </summary>
  public void Update(Camera camera, GameWorld world, float dt)
  {
    if (!world.Positions.Has(world.PlayerEntity))
      return;

    var playerPos = world.Positions.Get(world.PlayerEntity).Value;
    var playerVel = world.Velocities.Has(world.PlayerEntity) 
      ? world.Velocities.Get(world.PlayerEntity).Value 
      : Vector2.Zero;
    
    var groundState = world.GroundStates.Has(world.PlayerEntity)
      ? world.GroundStates.Get(world.PlayerEntity)
      : new GroundState { IsGrounded = false };

    var playerController = world.PlayerControllers.Has(world.PlayerEntity)
      ? world.PlayerControllers.Get(world.PlayerEntity)
      : new PlayerController { State = MovementState.Ground };

    // Calculate camera target using DKC2's look-ahead system
    _targetPosition = CalculateCameraTargetDKC2(
      playerPos, 
      playerVel, 
      playerController.State,
      groundState.IsGrounded,
      camera
    );

    // Apply camera volume constraints (DKC2-style bounds clamping)
    if (_activeVolume != null)
    {
      _targetPosition = ApplyVolumeConstraintsDKC2(_targetPosition, _activeVolume.Value, camera);
    }

    // DKC2 uses averaging for smooth camera movement
    // Instead of exponential damping, it averages between current and target
    _currentPosition = Vector2.Lerp(_currentPosition, _targetPosition, SmoothingFactor);

    // Update camera position
    camera.Position = _currentPosition;
  }

  /// <summary>
  /// Calculates camera target using DKC2's look-ahead mechanics.
  /// DKC2 uses fixed offsets ($0100 horizontal, $00E0 vertical) based on player position and facing.
  /// </summary>
  private Vector2 CalculateCameraTargetDKC2(Vector2 playerPos, Vector2 playerVel, MovementState state, bool isGrounded, Camera camera)
  {
    // Base target: player position offset by viewport center
    Vector2 target = new Vector2(
      playerPos.X - ViewportWidth * 0.5f,
      playerPos.Y - ViewportHeight * 0.5f
    );

    // DKC2 look-ahead: uses fixed offsets based on player facing and movement
    float lookAheadX = 0f;
    float lookAheadY = 0f;

    // Determine player facing direction
    int facingDirection = playerVel.X > 0.1f ? 1 : (playerVel.X < -0.1f ? -1 : 0);
    if (facingDirection == 0)
    {
      // Use last facing direction or default to right
      facingDirection = 1;
    }

    // DKC2 applies look-ahead based on movement state
    switch (state)
    {
      case MovementState.Cartwheeling:
        // Fast movement: use full horizontal offset
        lookAheadX = facingDirection * HorizontalLookAheadOffset * 0.5f; // Scaled for our resolution
        break;
      
      case MovementState.Ground:
        // Ground movement: apply look-ahead if moving fast enough
        if (Math.Abs(playerVel.X) > 50f) // Threshold for look-ahead activation
        {
          lookAheadX = facingDirection * HorizontalLookAheadOffset * 0.3f;
        }
        break;
      
      case MovementState.Airborne:
        // Airborne: reduced horizontal look-ahead, vertical follows player more closely
        if (Math.Abs(playerVel.X) > 50f)
        {
          lookAheadX = facingDirection * HorizontalLookAheadOffset * 0.2f;
        }
        // Vertical look-ahead only when falling fast
        if (playerVel.Y > 100f)
        {
          lookAheadY = VerticalLookAheadOffset * 0.2f;
        }
        break;
      
      case MovementState.Crouching:
        // Crouching: minimal look-ahead
        if (Math.Abs(playerVel.X) > 30f)
        {
          lookAheadX = facingDirection * HorizontalLookAheadOffset * 0.15f;
        }
        break;
    }

    target.X += lookAheadX;
    target.Y += lookAheadY;

    return target;
  }

  /// <summary>
  /// Applies DKC2-style volume constraints.
  /// DKC2 clamps camera to bounds and uses averaging for smooth transitions.
  /// </summary>
  private Vector2 ApplyVolumeConstraintsDKC2(Vector2 target, CameraVolume volume, Camera camera)
  {
    // Calculate bounds with viewport size
    float minX = volume.Bounds.Left;
    float maxX = volume.Bounds.Right - ViewportWidth;
    float minY = volume.Bounds.Top;
    float maxY = volume.Bounds.Bottom - ViewportHeight;

    // DKC2 clamps camera position to bounds
    target.X = MathHelper.Clamp(target.X, minX, maxX);
    target.Y = MathHelper.Clamp(target.Y, minY, maxY);

    // Apply volume offset (DKC2 uses camera offsets for special areas)
    target += volume.CameraOffset;

    // If vertical follow is disabled, keep camera Y locked (DKC2 behavior for small rooms)
    if (!volume.AllowVerticalFollow)
    {
      // Keep camera Y at a fixed position relative to volume
      target.Y = volume.Bounds.Top + (volume.Bounds.Height - ViewportHeight) * 0.5f;
    }

    return target;
  }

  public void SetActiveVolume(CameraVolume? volume)
  {
    _activeVolume = volume;
  }

  public CameraVolume? GetActiveVolume() => _activeVolume;
  
  /// <summary>
  /// Gets the current camera position (after smoothing).
  /// </summary>
  public Vector2 GetCurrentPosition() => _currentPosition;
  
  /// <summary>
  /// Gets the target camera position (before smoothing).
  /// </summary>
  public Vector2 GetTargetPosition() => _targetPosition;
  
  /// <summary>
  /// Jumps camera to a position immediately (for transitions, respawns, etc.)
  /// </summary>
  public void JumpTo(Vector2 position)
  {
    _currentPosition = position;
    _targetPosition = position;
  }
}

/// <summary>
/// Camera volume defining bounds and behavior for a room/area.
/// </summary>
public struct CameraVolume
{
  public Rectangle Bounds;
  public Vector2 CameraOffset;
  public bool AllowVerticalFollow;
  public int VolumeId; // For debug/identification
}

