using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GL2Engine.ECS;
using GL2Engine.Inventory;

namespace GL2Engine.Gameplay;

/// <summary>
/// Manages XP/currency collection, death drops, and reward thresholds.
/// Souls-like system: lose half on death, drop at last safe flag position.
/// </summary>
public class CurrencySystem
{
  private GameWorld _world;
  private int _rewardThresholdInterval;
  private Dictionary<Entity, Vector2> _deathDropPositions; // Entity -> drop position

  public CurrencySystem(GameWorld world, int rewardThresholdInterval = 100)
  {
    _world = world;
    _rewardThresholdInterval = rewardThresholdInterval;
    _deathDropPositions = new Dictionary<Entity, Vector2>();
  }

  /// <summary>
  /// Adds XP to the player's currency component.
  /// </summary>
  public void AddXP(Entity playerEntity, int amount)
  {
    if (!_world.Currencies.Has(playerEntity))
      return;

    ref var currency = ref _world.Currencies.Get(playerEntity);
    currency.CurrentXP += amount;
    currency.TotalCollected += amount;

    // Check if reward threshold reached (including Flow Meter Run Value contribution)
    int flowContribution = _world.GetFlowSystem()?.GetFlowContribution(playerEntity) ?? 0;
    int runValue = currency.CurrentXP + flowContribution;
    
    if (runValue >= currency.NextRewardThreshold)
    {
      TriggerRewardSelection(playerEntity, currency);
    }
  }

  /// <summary>
  /// Handles player death: lose half XP, drop at last safe flag position.
  /// </summary>
  public void HandleDeath(Entity playerEntity)
  {
    if (!_world.Currencies.Has(playerEntity))
      return;

    ref var currency = ref _world.Currencies.Get(playerEntity);
    
    if (currency.CurrentXP == 0)
      return; // Nothing to lose

    // Calculate loss (half, rounded down)
    int lostXP = currency.CurrentXP / 2;
    currency.CurrentXP -= lostXP;

    // Find last safe flag position (last flag the player passed)
    Vector2 dropPosition = GetLastSafeFlagPosition(playerEntity);
    
    // Create XP drop entity at that position
    CreateXPDrop(dropPosition, lostXP);
    
    // Store drop position for retrieval
    _deathDropPositions[playerEntity] = dropPosition;
  }

  /// <summary>
  /// Retrieves dropped XP when player reaches the drop position.
  /// </summary>
  public void RetrieveDrop(Entity playerEntity, Vector2 position, float pickupRadius = 50f)
  {
    if (!_deathDropPositions.ContainsKey(playerEntity))
      return;

    Vector2 dropPos = _deathDropPositions[playerEntity];
    
    if (Vector2.Distance(position, dropPos) <= pickupRadius)
    {
      // Find XP drop entity at this position
    foreach (var dropEntity in _world.XPDrops.GetActiveEntities())
    {
      if (!_world.Positions.Has(dropEntity))
        continue;

      var dropPosComp = _world.Positions.Get(dropEntity);
      if (Vector2.Distance(position, dropPosComp.Value) <= pickupRadius)
      {
        ref var drop = ref _world.XPDrops.Get(dropEntity);
        if (!drop.IsCollected)
        {
          // Collect the XP
          AddXP(playerEntity, drop.Value);
          drop.IsCollected = true;
          
          // Remove drop entity after a delay (or immediately)
          // TODO: Add removal logic or mark for removal
          _deathDropPositions.Remove(playerEntity);
          break;
        }
      }
    }
    }
  }

  /// <summary>
  /// Gets the last safe flag position the player passed.
  /// </summary>
  private Vector2 GetLastSafeFlagPosition(Entity playerEntity)
  {
    // Find the last flag the player passed (checkpoint system)
    // For now, return player's current position if no flag found
    if (_world.Positions.Has(playerEntity))
    {
      return _world.Positions.Get(playerEntity).Value;
    }

    return Vector2.Zero;
  }

  /// <summary>
  /// Creates an XP drop entity at the specified position.
  /// </summary>
  private void CreateXPDrop(Vector2 position, int xpValue)
  {
    var dropEntity = _world.CreateEntity();
    
    _world.Positions.Add(dropEntity, new Position { Value = position });
    _world.XPDrops.Add(dropEntity, new XPDropComponent
    {
      Value = xpValue,
      IsCollected = false,
      Lifetime = 0f
    });
    
    // Add visual representation (renderable)
    _world.Renderables.Add(dropEntity, new Renderable
    {
      MeshId = 0,
      Z = 0.5f,
      Layer = RenderLayer.MidLayer,
      ParallaxFactor = 1.0f
    });
  }

  /// <summary>
  /// Triggers reward selection UI when threshold is reached.
  /// </summary>
  private void TriggerRewardSelection(Entity playerEntity, CurrencyComponent currency)
  {
    // Calculate how many rewards to give (if player collected multiple thresholds worth)
    int rewardsToGive = currency.CurrentXP / currency.NextRewardThreshold;
    
    // Update threshold
    currency.NextRewardThreshold += _rewardThresholdInterval * rewardsToGive;
    
    // TODO: Trigger UI event for reward selection
    // _world.Events.Publish(new RewardThresholdReachedEvent { RewardsCount = rewardsToGive });
  }

  /// <summary>
  /// Updates currency system (checks for XP collection, drop retrieval, etc.).
  /// </summary>
  public void Update(float dt)
  {
    // Check for XP collection from collectibles
    foreach (var collectibleEntity in _world.XPCollectibles.GetActiveEntities())
    {
      ref var collectible = ref _world.XPCollectibles.Get(collectibleEntity);
      
      if (collectible.IsCollected)
        continue;

      // Check proximity to player
      if (_world.Positions.Has(collectibleEntity) && _world.Positions.Has(_world.PlayerEntity))
      {
        var collectiblePos = _world.Positions.Get(collectibleEntity).Value;
        var playerPos = _world.Positions.Get(_world.PlayerEntity).Value;
        
        float distance = Vector2.Distance(collectiblePos, playerPos);
        if (distance < 30f) // Pickup radius
        {
          // Collect XP
          AddXP(_world.PlayerEntity, collectible.Value);
          collectible.IsCollected = true;
          
          // TODO: Play collection effect, remove entity after animation
        }
      }
    }

    // Update XP drops lifetime
    foreach (var dropEntity in _world.XPDrops.GetActiveEntities())
    {
      ref var drop = ref _world.XPDrops.Get(dropEntity);
      drop.Lifetime += dt;
      
      // TODO: Remove drop after timeout or when collected
    }

    // Check for drop retrieval
    if (_world.Positions.Has(_world.PlayerEntity))
    {
      var playerPos = _world.Positions.Get(_world.PlayerEntity).Value;
      RetrieveDrop(_world.PlayerEntity, playerPos);
    }
  }
}

