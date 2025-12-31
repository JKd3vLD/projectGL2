using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GL2Engine.ECS;
using GL2Engine.World;

namespace GL2Engine.Gameplay;

/// <summary>
/// Manages flags (checkpoints) for fast travel, respawn, and consumable rewards.
/// </summary>
public class FlagSystem
{
  private GameWorld _world;
  private Dictionary<int, Entity> _flagEntities; // FlagId -> Entity
  private int _lastPassedFlagId = -1; // Last flag the player passed

  public FlagSystem(GameWorld world)
  {
    _world = world;
    _flagEntities = new Dictionary<int, Entity>();
  }

  /// <summary>
  /// Creates flag entities from a stage plan.
  /// </summary>
  public void CreateFlagsFromPlan(StagePlan plan)
  {
    _flagEntities.Clear();
    _lastPassedFlagId = -1;

    int flagId = 0;
    foreach (var flagPos in plan.Flags)
    {
      var flagEntity = _world.CreateEntity();
      
      _world.Positions.Add(flagEntity, new Position { Value = flagPos.Position });
      _world.Flags.Add(flagEntity, new FlagComponent
      {
        FlagId = flagId,
        FlagType = (int)flagPos.Type, // Store as int to avoid enum conflict
        IsConsumable = flagPos.IsConsumable,
        IsConsumed = false
      });

      // Add visual representation
      _world.Renderables.Add(flagEntity, new Renderable
      {
        MeshId = 0,
        Z = 0.3f,
        Layer = RenderLayer.MidLayer,
        ParallaxFactor = 1.0f
      });

      _flagEntities[flagId] = flagEntity;
      flagId++;
    }
  }

  /// <summary>
  /// Updates flag system: checks for flag passing, fast travel, consumable usage.
  /// </summary>
  public void Update(float dt)
  {
    if (!_world.Positions.Has(_world.PlayerEntity))
      return;

    var playerPos = _world.Positions.Get(_world.PlayerEntity).Value;

    // Check if player passed any flags
    foreach (var kvp in _flagEntities)
    {
      int flagId = kvp.Key;
      Entity flagEntity = kvp.Value;

      if (!_world.Positions.Has(flagEntity) || !_world.Flags.Has(flagEntity))
        continue;

      var flagPos = _world.Positions.Get(flagEntity).Value;
      var flag = _world.Flags.Get(flagEntity);

      float distance = Vector2.Distance(playerPos, flagPos);
      if (distance < 50f) // Flag activation radius
      {
        // Player passed this flag
        if (flagId > _lastPassedFlagId)
        {
          _lastPassedFlagId = flagId;
          // TODO: Play flag activation effect
        }
      }
    }
  }

  /// <summary>
  /// Gets the last safe flag position (for respawn/death drop).
  /// </summary>
  public Vector2 GetLastSafeFlagPosition()
  {
    if (_lastPassedFlagId < 0)
      return Vector2.Zero;

    if (_flagEntities.TryGetValue(_lastPassedFlagId, out Entity flagEntity))
    {
      if (_world.Positions.Has(flagEntity))
      {
        return _world.Positions.Get(flagEntity).Value;
      }
    }

    return Vector2.Zero;
  }

  /// <summary>
  /// Consumes a flag for rewards (Shovel Knight style).
  /// </summary>
  public bool ConsumeFlag(int flagId)
  {
    if (!_flagEntities.TryGetValue(flagId, out Entity flagEntity))
      return false;

    if (!_world.Flags.Has(flagEntity))
      return false;

    ref var flag = ref _world.Flags.Get(flagEntity);
    
    if (!flag.IsConsumable || flag.IsConsumed)
      return false;

    flag.IsConsumed = true;
    
    // TODO: Trigger reward calculation based on flag type and stage pacing
    // TODO: Update visual representation (flag disappears or changes appearance)

    return true;
  }

  /// <summary>
  /// Fast travels player to a flag position.
  /// </summary>
  public bool FastTravelToFlag(int flagId)
  {
    if (!_flagEntities.TryGetValue(flagId, out Entity flagEntity))
      return false;

    if (!_world.Positions.Has(flagEntity))
      return false;

    var flagPos = _world.Positions.Get(flagEntity).Value;
    
    // Move player to flag position
    if (_world.Positions.Has(_world.PlayerEntity))
    {
      ref var playerPos = ref _world.Positions.Get(_world.PlayerEntity);
      playerPos.Value = flagPos;
    }

    return true;
  }

  /// <summary>
  /// Respawns player at last passed flag.
  /// </summary>
  public void RespawnAtLastFlag()
  {
    Vector2 safePos = GetLastSafeFlagPosition();
    
    if (_world.Positions.Has(_world.PlayerEntity))
    {
      ref var playerPos = ref _world.Positions.Get(_world.PlayerEntity);
      playerPos.Value = safePos;
    }

    // Reset velocity
    if (_world.Velocities.Has(_world.PlayerEntity))
    {
      ref var velocity = ref _world.Velocities.Get(_world.PlayerEntity);
      velocity.Value = Vector2.Zero;
    }
  }

  private Color GetFlagColor(FlagType type)
  {
    return type switch
    {
      FlagType.Start => Color.Green,
      FlagType.Middle => Color.Yellow,
      FlagType.End => Color.Red,
      _ => Color.White
    };
  }

  private Color GetFlagColor(int flagTypeInt)
  {
    return GetFlagColor((FlagType)flagTypeInt);
  }
}

