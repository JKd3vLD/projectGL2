using System;
using System.Collections.Generic;
using System.Linq;
using GL2Engine.ECS;
using GL2Engine.Inventory;
using GL2Engine.World;

namespace GL2Engine.Gameplay;

/// <summary>
/// Manages Technique Mods (run-scoped relics) that add contextual bonuses.
/// </summary>
public class RelicSystem
{
  private readonly GameWorld _world;
  private readonly List<ItemDef> _equippedRelics = new List<ItemDef>();
  private RelicTuning _tuning;

  public RelicSystem(GameWorld world)
  {
    _world = world;
    LoadTuning();
  }

  private void LoadTuning()
  {
    try
    {
      var json = System.IO.File.ReadAllText("GL2Project/Tuning/RelicTuning.json");
      var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
      _tuning = System.Text.Json.JsonSerializer.Deserialize<RelicTuning>(json, options) ?? new RelicTuning();
    }
    catch
    {
      _tuning = new RelicTuning();
    }
  }

  /// <summary>
  /// Equip a relic (add to equipped list).
  /// </summary>
  public void EquipRelic(ItemDef relic)
  {
    if (relic.RelicType == RelicType.TechniqueMod && !_equippedRelics.Contains(relic))
    {
      _equippedRelics.Add(relic);
      InitializeProcState();
    }
  }

  /// <summary>
  /// Unequip a relic.
  /// </summary>
  public void UnequipRelic(string relicId)
  {
    _equippedRelics.RemoveAll(r => r.Id == relicId);
    InitializeProcState();
  }

  private void InitializeProcState()
  {
    if (!_world.TechniqueProcStates.Has(_world.PlayerEntity))
    {
      var procState = new TechniqueProcState
      {
        CooldownTimers = new float[10],
        PerSectionTriggered = new bool[10],
        ChargeCounts = new int[10],
        EquippedCount = 0
      };
      _world.TechniqueProcStates.Add(_world.PlayerEntity, procState);
    }

    ref var procState = ref _world.TechniqueProcStates.Get(_world.PlayerEntity);
    
    // Ensure arrays are initialized
    if (procState.CooldownTimers == null)
      procState.CooldownTimers = new float[10];
    if (procState.PerSectionTriggered == null)
      procState.PerSectionTriggered = new bool[10];
    if (procState.ChargeCounts == null)
      procState.ChargeCounts = new int[10];
    
    procState.EquippedCount = Math.Min(_equippedRelics.Count, 10);
  }

  /// <summary>
  /// Evaluate triggers for equipped Technique Mods.
  /// </summary>
  public void EvaluateTriggers(FlowEventType eventType, object? context = null)
  {
    if (!_world.TechniqueProcStates.Has(_world.PlayerEntity))
      return;

    ref var procState = ref _world.TechniqueProcStates.Get(_world.PlayerEntity);

    for (int i = 0; i < Math.Min(_equippedRelics.Count, 10); i++)
    {
      var relic = _equippedRelics[i];
      if (relic.Triggers == null || !relic.Triggers.Contains(eventType))
        continue;

      // Check cooldown
      if (procState.CooldownTimers[i] > 0f)
        continue;

      // Check per-section cap
      if (procState.PerSectionTriggered[i] && procState.ChargeCounts[i] >= _tuning.MaxProcsPerSection)
        continue;

      // Check eligibility (pacing tag, etc.)
      if (!IsEligible(relic, context))
        continue;

      // Apply effects
      ApplyEffects(relic, i, ref procState, context);

      // Set cooldown and increment count
      procState.CooldownTimers[i] = _tuning.TechniqueProcCooldown;
      procState.ChargeCounts[i]++;
      if (procState.ChargeCounts[i] >= _tuning.MaxProcsPerSection)
      {
        procState.PerSectionTriggered[i] = true;
      }
    }
  }

  private bool IsEligible(ItemDef relic, object? context)
  {
    // Check pacing tag requirement
    if (relic.Effects != null)
    {
      foreach (var effect in relic.Effects)
      {
        if (effect.Constraints.RequiresPacingTag && effect.Constraints.PacingTag.HasValue)
        {
          // TODO: Check current stage pacing tag
          // For now, allow all
        }
      }
    }
    return true;
  }

  private void ApplyEffects(ItemDef relic, int slotIndex, ref TechniqueProcState procState, object? context)
  {
    if (relic.Effects == null)
      return;

    foreach (var effect in relic.Effects)
    {
      switch (effect.Kind)
      {
        case EffectKind.FlowDelta:
          // Emit FlowEvent
          _world.Events.Push(new FlowEvent
          {
            EventType = FlowEventType.Chain, // Use Chain as default
            Delta = effect.Magnitude,
            Entity = _world.PlayerEntity
          });
          break;

        case EffectKind.ThresholdProgressDelta:
          // Add to Run Value (handled by CurrencySystem)
          // TODO: Integrate with CurrencySystem
          break;

        case EffectKind.RewardQualityDelta:
          // Modify reward quality (handled by RewardSystem)
          // TODO: Integrate with RewardSystem
          break;

        case EffectKind.TokenGrant:
          // Grant token (speed tier, bonus token, etc.)
          // TODO: Implement token system
          break;

        case EffectKind.UIHint:
          // Show UI hint (treasure ping, etc.)
          // TODO: Implement UI hint system
          break;
      }
    }
  }

  /// <summary>
  /// Update cooldowns and reset per-section flags on new ground contact.
  /// </summary>
  public void Update(float dt)
  {
    if (!_world.TechniqueProcStates.Has(_world.PlayerEntity))
      return;

    ref var procState = ref _world.TechniqueProcStates.Get(_world.PlayerEntity);

    // Decrement cooldowns
    for (int i = 0; i < procState.EquippedCount; i++)
    {
      if (procState.CooldownTimers[i] > 0f)
      {
        procState.CooldownTimers[i] -= dt;
        if (procState.CooldownTimers[i] < 0f)
          procState.CooldownTimers[i] = 0f;
      }
    }
  }

  /// <summary>
  /// Reset per-section flags (called on new ground contact or new section).
  /// </summary>
  public void ResetPerSectionFlags()
  {
    if (!_world.TechniqueProcStates.Has(_world.PlayerEntity))
      return;

    ref var procState = ref _world.TechniqueProcStates.Get(_world.PlayerEntity);
    for (int i = 0; i < procState.EquippedCount; i++)
    {
      procState.PerSectionTriggered[i] = false;
      procState.ChargeCounts[i] = 0;
    }
  }
}

/// <summary>
/// Relic tuning parameters.
/// </summary>
public class RelicTuning
{
  public float TechniqueProcCooldown { get; set; } = 1.5f;
  public int MaxProcsPerSection { get; set; } = 3;
  public float FlowBonusPerProc { get; set; } = 0.05f;
  public int RewardQualityDelta { get; set; } = 1;
}

