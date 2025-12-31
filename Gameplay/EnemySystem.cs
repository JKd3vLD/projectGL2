using System;
using Microsoft.Xna.Framework;
using GL2Engine.ECS;

namespace GL2Engine.Gameplay;

/// <summary>
/// Enemy system - handles enemy AI and collision with player.
/// </summary>
public class EnemySystem
{
  private GameWorld _world;

  public EnemySystem(GameWorld world)
  {
    _world = world;
  }

  public void Update(float dt)
  {
    // Update enemy AI
    for (int i = 0; i < _world.Enemies.Count; i++)
    {
      var entity = _world.Enemies.GetEntity(i);
      if (!entity.IsValid) continue;

      var enemy = _world.Enemies.GetByIndex(i);
      if (enemy.State == EnemyState.Dead)
        continue;

      // Simple patrol AI
      if (_world.EnemyAIs.Has(entity))
      {
        var ai = _world.EnemyAIs.Get(entity);
        var position = _world.Positions.Get(entity);
        var velocity = _world.Velocities.Has(entity) ? _world.Velocities.Get(entity) : new Velocity();

        // Move in single direction (simple patrol)
        if (ai.Waypoints.Length > 0)
        {
          var targetWaypoint = ai.Waypoints[ai.CurrentWaypoint];
          Vector2 direction = targetWaypoint - position.Value;
          float distance = direction.Length();

          if (distance < 5.0f)
          {
            // Reached waypoint, move to next or reverse
            ai.CurrentWaypoint = (ai.CurrentWaypoint + 1) % ai.Waypoints.Length;
            _world.EnemyAIs.Get(entity) = ai;
          }
          else
          {
            direction.Normalize();
            velocity.Value = direction * ai.PatrolSpeed;
            _world.Velocities.Get(entity) = velocity;
          }
        }
        else
        {
          // No waypoints - stay in place
          velocity.Value = Vector2.Zero;
          if (_world.Velocities.Has(entity))
            _world.Velocities.Get(entity) = velocity;
        }
      }

      // Check collision with player
      if (_world.Positions.Has(_world.PlayerEntity))
      {
        var playerPos = _world.Positions.Get(_world.PlayerEntity);
        var playerCollider = _world.Colliders.Get(_world.PlayerEntity);
        var enemyPos = _world.Positions.Get(entity);
        var enemyCollider = _world.Colliders.Has(entity) ? _world.Colliders.Get(entity) : new Collider();

        float distance = Vector2.Distance(playerPos.Value, enemyPos.Value);
        float collisionDist = playerCollider.Size.X + enemyCollider.Size.X;

        if (distance < collisionDist)
        {
          // Player hit enemy - damage player
          if (_world.Healths.Has(_world.PlayerEntity))
          {
            var playerHealth = _world.Healths.Get(_world.PlayerEntity);
            if (!_world.Invulnerabilities.Has(_world.PlayerEntity) || _world.Invulnerabilities.Get(_world.PlayerEntity).RemainingTime <= 0)
            {
              playerHealth.Current = Math.Max(0, playerHealth.Current - 1);
              _world.Healths.Get(_world.PlayerEntity) = playerHealth;

              // Add invulnerability
              _world.Invulnerabilities.Add(_world.PlayerEntity, new Invulnerability { RemainingTime = 2.0f });
              
              // Lock controls briefly (hitstun) - handled in PlayerController
              _world.Events.Push(new PlayerDamaged { Entity = _world.PlayerEntity, Damage = 1 });
            }
          }

          // Enemy dies in one hit
          enemy.State = EnemyState.Dead;
          _world.Enemies.GetByIndex(i) = enemy;
        }
      }
    }

    // Update invulnerability timers
    for (int i = 0; i < _world.Invulnerabilities.Count; i++)
    {
      var entity = _world.Invulnerabilities.GetEntity(i);
      if (!entity.IsValid) continue;

      var invuln = _world.Invulnerabilities.GetByIndex(i);
      invuln.RemainingTime -= dt;
      if (invuln.RemainingTime <= 0)
      {
        _world.Invulnerabilities.Remove(entity);
      }
      else
      {
        _world.Invulnerabilities.GetByIndex(i) = invuln;
      }
    }
  }
}

