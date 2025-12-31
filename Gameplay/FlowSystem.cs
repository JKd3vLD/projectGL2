using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using GL2Engine.ECS;
using GL2Engine.World;

namespace GL2Engine.Gameplay;

/// <summary>
/// Manages stage-local Flow Meter that fills from pacing-appropriate actions.
/// Never modifies movement parameters, only affects rewards via multiplier.
/// </summary>
public class FlowSystem
{
  private readonly GameWorld _world;
  private FlowTuning _tuning;

  public FlowSystem(GameWorld world)
  {
    _world = world;
    LoadTuning();
  }

  private void LoadTuning()
  {
    try
    {
      var json = File.ReadAllText("GL2Project/Tuning/FlowTuning.json");
      var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
      _tuning = JsonSerializer.Deserialize<FlowTuning>(json, options) ?? new FlowTuning();
    }
    catch
    {
      _tuning = new FlowTuning(); // Use defaults
    }
  }

  /// <summary>
  /// Initialize Flow Component for a stage.
  /// </summary>
  public void InitializeStage(Entity playerEntity, PacingTag pacingTag)
  {
    _gameTime = 0f; // Reset game time for new stage
    
    if (!_world.FlowComponents.Has(playerEntity))
    {
      _world.FlowComponents.Add(playerEntity, new FlowComponent
      {
        Flow = 0f,
        DecayTimer = 0f,
        LastProgressTime = 0f,
        ComboCount = 0,
        FlowMode = pacingTag == PacingTag.FAST ? FlowMode.FAST : FlowMode.SLOW
      });
    }
    else
    {
      // Reset for new stage
      ref var flow = ref _world.FlowComponents.Get(playerEntity);
      flow.Flow = 0f;
      flow.DecayTimer = 0f;
      flow.LastProgressTime = 0f;
      flow.ComboCount = 0;
      flow.FlowMode = pacingTag == PacingTag.FAST ? FlowMode.FAST : FlowMode.SLOW;
    }
  }

  private float _gameTime = 0f;

  /// <summary>
  /// Update Flow Meter: consume events, apply decay.
  /// </summary>
  public void Update(float dt)
  {
    if (!_world.FlowComponents.Has(_world.PlayerEntity))
      return;

    _gameTime += dt;
    ref var flow = ref _world.FlowComponents.Get(_world.PlayerEntity);
    float currentTime = _gameTime;

    // Consume FlowEvents
    while (_world.Events.TryPopFlow(out var flowEvent))
    {
      ProcessFlowEvent(ref flow, flowEvent, currentTime);
    }

    // Apply decay
    ApplyDecay(ref flow, dt, currentTime);

    // Update decay timer
    if (currentTime - flow.LastProgressTime > _tuning.IdleGraceSeconds)
    {
      flow.DecayTimer += dt;
    }
    else
    {
      flow.DecayTimer = 0f; // Reset if progress made
    }
  }

  private void ProcessFlowEvent(ref FlowComponent flow, FlowEvent evt, float currentTime)
  {
    // Check eligibility (pacing tag match)
    bool isFast = flow.FlowMode == FlowMode.FAST;
    bool isSlow = flow.FlowMode == FlowMode.SLOW;

    // Apply Flow delta based on event type
    float delta = 0f;

    switch (evt.EventType)
    {
      case FlowEventType.TimeTierHit:
        if (isFast)
        {
          delta = _tuning.TimeTierFlowGain * (evt.Value + 1); // Higher tier = more Flow
        }
        break;

      case FlowEventType.Chain:
        if (isFast)
        {
          delta = _tuning.ChainFlowGain;
          flow.ComboCount++;
        }
        break;

      case FlowEventType.SecretFound:
        if (isSlow)
        {
          delta = _tuning.SecretFlowGain;
        }
        break;

      case FlowEventType.CarryDelivered:
        if (isSlow)
        {
          delta = _tuning.CarryDeliveryFlowGain;
        }
        break;

      case FlowEventType.BonusComplete:
        if (isSlow)
        {
          delta = _tuning.BonusCompleteFlowGain;
        }
        break;

      case FlowEventType.PuzzleClear:
        if (isSlow)
        {
          delta = _tuning.PuzzleClearFlowGain;
        }
        break;

      case FlowEventType.DamageTaken:
        delta = -_tuning.DamageFlowPenalty;
        flow.ComboCount = 0; // Reset chain
        break;

      case FlowEventType.IdleTick:
        // Decay handled separately
        break;

      case FlowEventType.BacktrackTick:
        if (isFast)
        {
          delta = -_tuning.BacktrackPenaltyRate * 0.016f; // Per frame (assuming 60fps base)
        }
        break;
    }

    // Apply delta
    if (delta != 0f)
    {
      flow.Flow += delta;
      flow.Flow = Math.Clamp(flow.Flow, 0f, _tuning.FlowMax);
      flow.LastProgressTime = currentTime;
      flow.DecayTimer = 0f; // Reset decay timer on progress
    }
  }

  private void ApplyDecay(ref FlowComponent flow, float dt, float currentTime)
  {
    if (flow.DecayTimer <= _tuning.IdleGraceSeconds)
      return; // No decay during grace period

    float decayRate = _tuning.FlowDecayPerSecond;
    
    // Adjust decay rate based on mode
    if (flow.FlowMode == FlowMode.FAST)
    {
      decayRate = _tuning.FlowDecayPerSecondFast;
    }
    else
    {
      decayRate = _tuning.FlowDecayPerSecondSlow;
    }

    // Accelerate decay if idle continues
    float idleExcess = flow.DecayTimer - _tuning.IdleGraceSeconds;
    if (idleExcess > 0f)
    {
      decayRate *= (1.0f + idleExcess * _tuning.IdleAccelerationFactor);
    }

    flow.Flow -= decayRate * dt;
    flow.Flow = Math.Clamp(flow.Flow, 0f, _tuning.FlowMax);
  }

  /// <summary>
  /// Get current Flow value (0..1).
  /// </summary>
  public float GetFlow(Entity playerEntity)
  {
    if (!_world.FlowComponents.Has(playerEntity))
      return 0f;

    ref var flow = ref _world.FlowComponents.Get(playerEntity);
    return flow.Flow / _tuning.FlowMax; // Normalize to [0, 1]
  }

  /// <summary>
  /// Get Flow contribution to Run Value (RV) for threshold progress.
  /// </summary>
  public int GetFlowContribution(Entity playerEntity)
  {
    float flow = GetFlow(playerEntity);
    return (int)(flow * _tuning.FlowThresholdProgressMultiplier);
  }

  /// <summary>
  /// Calculate reward multiplier from Flow Meter.
  /// </summary>
  public float GetRewardMultiplier(Entity playerEntity)
  {
    float flow = GetFlow(playerEntity);
    return 1.0f + (flow * _tuning.FlowRewardMultiplierRange);
  }
}

/// <summary>
/// Flow tuning parameters loaded from JSON.
/// </summary>
public class FlowTuning
{
  public float FlowMax { get; set; } = 1.0f;
  public float FlowDecayPerSecond { get; set; } = 0.12f;
  public float FlowDecayPerSecondFast { get; set; } = 0.15f;
  public float FlowDecayPerSecondSlow { get; set; } = 0.08f;
  public float IdleGraceSeconds { get; set; } = 1.0f;
  public float IdleAccelerationFactor { get; set; } = 0.1f;
  public float DamageFlowPenalty { get; set; } = 0.25f;
  public float BacktrackPenaltyRate { get; set; } = 0.15f;
  public float SecretFlowGain { get; set; } = 0.10f;
  public float CarryDeliveryFlowGain { get; set; } = 0.15f;
  public float BonusCompleteFlowGain { get; set; } = 0.12f;
  public float PuzzleClearFlowGain { get; set; } = 0.08f;
  public float TimeTierFlowGain { get; set; } = 0.05f;
  public float ChainFlowGain { get; set; } = 0.03f;
  public float FlowRewardMultiplierRange { get; set; } = 0.5f;
  public float FlowThresholdProgressMultiplier { get; set; } = 10f;
}

