using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GL2Engine.ECS;
using GL2Engine.Content;

namespace GL2Engine.Physics2D;

/// <summary>
/// Physics2D system - handles collision detection and resolution.
/// </summary>
public class PhysicsSystem
{
  private GameWorld _world;
  private CollisionWorld _collisionWorld;

  public PhysicsSystem(GameWorld world)
  {
    _world = world;
    _collisionWorld = world.CollisionWorld;
  }

  public void Update(float dt)
  {
    // Update moving platforms first
    UpdateMovingPlatforms(dt);

    // Update positions from velocities
    for (int i = 0; i < _world.Velocities.Count; i++)
    {
      var entity = _world.Velocities.GetEntity(i);
      if (!entity.IsValid || !_world.Positions.Has(entity)) continue;

      var velocity = _world.Velocities.GetByIndex(i);
      var position = _world.Positions.Get(entity);

      // Check if riding a platform
      if (_world.PlatformRiders.Has(entity))
      {
        var rider = _world.PlatformRiders.Get(entity);
        if (rider.PlatformEntity.IsValid && _world.MovingPlatforms.Has(rider.PlatformEntity))
        {
          var platform = _world.MovingPlatforms.Get(rider.PlatformEntity);
          var platformPos = _world.Positions.Get(rider.PlatformEntity);
          
          // Inherit platform velocity
          var platformVel = _world.Velocities.Has(rider.PlatformEntity) ? _world.Velocities.Get(rider.PlatformEntity) : new Velocity();
          velocity.Value += platformVel.Value;
        }
      }

      // Integrate velocity
      position.Value += velocity.Value * dt;
      _world.Positions.Get(entity) = position;
    }

    // Collision detection and resolution
    ResolveCollisions();
  }

  private void UpdateMovingPlatforms(float dt)
  {
    for (int i = 0; i < _world.MovingPlatforms.Count; i++)
    {
      var entity = _world.MovingPlatforms.GetEntity(i);
      if (!entity.IsValid || !_world.Positions.Has(entity)) continue;

      var platform = _world.MovingPlatforms.GetByIndex(i);
      var position = _world.Positions.Get(entity);
      var velocity = _world.Velocities.Has(entity) ? _world.Velocities.Get(entity) : new Velocity();

      // Update platform interpolation
      platform.CurrentT += platform.Speed * dt;
      if (platform.CurrentT > 1.0f)
      {
        if (platform.PingPong)
        {
          platform.CurrentT = 2.0f - platform.CurrentT;
          platform.Speed = -platform.Speed;
        }
        else
        {
          platform.CurrentT = 0.0f;
        }
      }

      // Interpolate position
      Vector2 currentPos = Vector2.Lerp(platform.StartPos, platform.EndPos, platform.CurrentT);
      velocity.Value = (currentPos - position.Value) / dt;

      _world.MovingPlatforms.GetByIndex(i) = platform;
      _world.Positions.Get(entity) = new Position { Value = currentPos };
      if (_world.Velocities.Has(entity))
        _world.Velocities.Get(entity) = velocity;
    }
  }

  private void ResolveCollisions()
  {
    // Ground collision with slopes support
    const float GroundY = 300.0f; // Temporary ground level
    const float SlopeThreshold = 30.0f; // degrees
    const float AutoSlideThreshold = 70.0f; // degrees

    for (int i = 0; i < _world.Colliders.Count; i++)
    {
      var entity = _world.Colliders.GetEntity(i);
      if (!entity.IsValid || !_world.Positions.Has(entity)) continue;

      var position = _world.Positions.Get(entity);
      var collider = _world.Colliders.GetByIndex(i);
      var velocity = _world.Velocities.Has(entity) ? _world.Velocities.Get(entity) : new Velocity();
      var groundState = _world.GroundStates.Has(entity) ? _world.GroundStates.Get(entity) : new GroundState();

      // Check for ground collision
      float bottom = position.Value.Y + collider.Size.Y;
      bool wasGrounded = groundState.IsGrounded;

      // Check slope segments first
      bool hitSlope = false;
      foreach (var slopeSeg in _world.CollisionWorld.SlopeSegments)
      {
        if (SlopeSolver.IntersectsSlope(position.Value, collider.Size.Y, slopeSeg, out Vector2 contactPoint, out Vector2 normal))
        {
          SlopeSolver.ResolveCollision(ref position, ref velocity, ref groundState, slopeSeg, collider.Size.Y);
          hitSlope = true;
          break;
        }
      }

      // Fallback to simple ground plane
      if (!hitSlope && bottom >= GroundY && velocity.Value.Y >= 0)
      {
        position.Value.Y = GroundY - collider.Size.Y;
        velocity.Value.Y = 0;
        groundState.IsGrounded = true;
        groundState.GroundNormal = new Vector2(0, -1);
        groundState.GroundAngle = 0;
        groundState.OnSlope = false;
        groundState.CanSlide = false;
      }

      // Check moving platforms
      for (int j = 0; j < _world.MovingPlatforms.Count; j++)
      {
        var platformEntity = _world.MovingPlatforms.GetEntity(j);
        if (!platformEntity.IsValid || !_world.Positions.Has(platformEntity)) continue;

        var platformPos = _world.Positions.Get(platformEntity);
        var platformCollider = _world.Colliders.Has(platformEntity) ? _world.Colliders.Get(platformEntity) : new Collider();

        // Simple AABB collision
        if (CheckAABBCollision(position.Value, collider.Size, platformPos.Value, platformCollider.Size))
        {
          // Player rides platform
          if (!_world.PlatformRiders.Has(entity))
          {
            _world.PlatformRiders.Add(entity, new PlatformRider { PlatformEntity = platformEntity });
          }
          
          // Adjust position to sit on platform
          float platformTop = platformPos.Value.Y - platformCollider.Size.Y;
          if (position.Value.Y + collider.Size.Y > platformTop && velocity.Value.Y >= 0)
          {
            position.Value.Y = platformTop - collider.Size.Y;
            velocity.Value.Y = 0;
            groundState.IsGrounded = true;
          }
        }
      }

      if (!hitSlope && bottom < GroundY)
      {
        groundState.IsGrounded = false;
      }

      _world.Positions.Get(entity) = position;
      if (_world.Velocities.Has(entity))
        _world.Velocities.Get(entity) = velocity;
      if (_world.GroundStates.Has(entity))
        _world.GroundStates.Get(entity) = groundState;

      // Fire landed event if player and just landed
      if (!wasGrounded && groundState.IsGrounded && _world.PlayerControllers.Has(entity))
      {
        _world.Events.Push(new PlayerLanded { Entity = entity, Position = position.Value });
      }
    }
  }

  private bool CheckAABBCollision(Vector2 pos1, Vector2 size1, Vector2 pos2, Vector2 size2)
  {
    return pos1.X - size1.X < pos2.X + size2.X && pos1.X + size1.X > pos2.X - size2.X && pos1.Y - size1.Y < pos2.Y + size2.Y && pos1.Y + size1.Y > pos2.Y - size2.Y;
  }
}

/// <summary>
/// Collision world - stores static collision geometry.
/// </summary>
public class CollisionWorld
{
  public List<SlopeSolver.SlopeSegment> SlopeSegments { get; } = [];
  public List<Vector2> StaticAABBs { get; } = []; // Store as center + half-extents pairs

  public void AddSlopeSegment(SlopeSolver.SlopeSegment segment)
  {
    SlopeSegments.Add(segment);
  }

  public void AddStaticAABB(Vector2 center, Vector2 halfExtents)
  {
    StaticAABBs.Add(center);
    StaticAABBs.Add(halfExtents);
  }
}
