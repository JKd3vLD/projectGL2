using Microsoft.Xna.Framework;
using GL2Engine.ECS;

namespace GL2Engine.Render;

/// <summary>
/// Camera system that updates camera position based on player and camera volumes.
/// Runs after MovementSystem, before RenderSystem.
/// </summary>
public class CameraSystem
{
  private CameraController _controller;
  private GameWorld _world;
  private Camera? _camera;

  public CameraSystem(GameWorld world)
  {
    _world = world;
    _controller = new CameraController();
  }

  public void SetCamera(Camera camera)
  {
    _camera = camera;
  }

  public void Update(float dt)
  {
    if (_camera == null)
      return;

    // Find active camera volume based on player position
    CameraVolume? activeVolume = FindActiveVolume();

    _controller.SetActiveVolume(activeVolume);
    _controller.Update(_camera, _world, dt);
  }

  private CameraVolume? FindActiveVolume()
  {
    if (!_world.Positions.Has(_world.PlayerEntity))
      return null;

    var playerPos = _world.Positions.Get(_world.PlayerEntity).Value;

    // Query all CameraVolumeComponent entities to find the volume containing the player
    // DKC2-style: first volume that contains the player position wins
    // In case of overlapping volumes, the first one found takes precedence
    foreach (var entity in _world.CameraVolumes.GetActiveEntities())
    {
      var volumeComponent = _world.CameraVolumes.Get(entity);
      
      // Check if player position is within this volume's bounds
      if (volumeComponent.Bounds.Contains(playerPos))
      {
        // Convert CameraVolumeComponent to CameraVolume
        return new CameraVolume
        {
          Bounds = volumeComponent.Bounds,
          CameraOffset = volumeComponent.CameraOffset,
          AllowVerticalFollow = volumeComponent.AllowVerticalFollow,
          VolumeId = volumeComponent.VolumeId
        };
      }
    }

    // No volume found containing the player
    return null;
  }

  public CameraController GetController() => _controller;
}

