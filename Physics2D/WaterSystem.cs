using Microsoft.Xna.Framework;
using GL2Engine.ECS;

namespace GL2Engine.Physics2D;

/// <summary>
/// Water system - handles water physics and swimming mechanics.
/// </summary>
public class WaterSystem
{
  private GameWorld _world;

  public WaterSystem(GameWorld world)
  {
    _world = world;
  }

  public void Update(float dt)
  {
    // Check all entities for water collision
    for (int i = 0; i < _world.Positions.Count; i++)
    {
      var entity = _world.Positions.GetEntity(i);
      if (!entity.IsValid) continue;

      var position = _world.Positions.GetByIndex(i);
      var collider = _world.Colliders.Has(entity) ? _world.Colliders.Get(entity) : new Collider();

      // Check against all water volumes
      bool inWater = false;
      Entity waterEntity = Entity.Invalid;

      for (int j = 0; j < _world.WaterVolumes.Count; j++)
      {
        var waterVolEntity = _world.WaterVolumes.GetEntity(j);
        if (!waterVolEntity.IsValid || !_world.Positions.Has(waterVolEntity)) continue;

        var waterVol = _world.WaterVolumes.GetByIndex(j);
        var waterPos = _world.Positions.Get(waterVolEntity);

        // Check if entity is inside water AABB
        if (IsPointInAABB(position.Value, waterPos.Value, waterVol.Size))
        {
          inWater = true;
          waterEntity = waterVolEntity;
          
          // Apply buoyancy
          if (_world.Velocities.Has(entity))
          {
            var velocity = _world.Velocities.Get(entity);
            velocity.Value.Y -= waterVol.Buoyancy * dt;
            _world.Velocities.Get(entity) = velocity;
          }

          // Apply water drag
          if (_world.Velocities.Has(entity))
          {
            var velocity = _world.Velocities.Get(entity);
            velocity.Value *= (1.0f - waterVol.Drag * dt);
            _world.Velocities.Get(entity) = velocity;
          }
          break;
        }
      }

      // Update swimming component
      if (inWater)
      {
        if (!_world.Swimmings.Has(entity))
        {
          _world.Swimmings.Add(entity, new Swimming { IsInWater = true, WaterEntity = waterEntity });
        }
        else
        {
          var swimming = _world.Swimmings.Get(entity);
          swimming.IsInWater = true;
          swimming.WaterEntity = waterEntity;
          _world.Swimmings.Get(entity) = swimming;
        }
      }
      else
      {
        if (_world.Swimmings.Has(entity))
        {
          var swimming = _world.Swimmings.Get(entity);
          swimming.IsInWater = false;
          _world.Swimmings.Get(entity) = swimming;
        }
      }
    }
  }

  private bool IsPointInAABB(Vector2 point, Vector2 aabbCenter, Vector2 aabbSize)
  {
    Vector2 halfSize = aabbSize * 0.5f;
    return point.X >= aabbCenter.X - halfSize.X && point.X <= aabbCenter.X + halfSize.X && point.Y >= aabbCenter.Y - halfSize.Y && point.Y <= aabbCenter.Y + halfSize.Y;
  }
}

