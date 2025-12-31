using System;
using GL2Engine.Inventory;

namespace GL2Engine.World;

/// <summary>
/// Calculates rewards for FAST stages based on speed, flow, and perfect execution.
/// </summary>
public class FastRewardCalculator
{
  private readonly float[] _timeBonusThresholds; // Time thresholds in seconds
  private readonly float _flowBonusIdleThreshold; // Max idle time for flow bonus

  public FastRewardCalculator(float[] timeBonusThresholds, float flowBonusIdleThreshold)
  {
    _timeBonusThresholds = timeBonusThresholds ?? new float[] { 30, 60, 90, 120 };
    _flowBonusIdleThreshold = flowBonusIdleThreshold;
  }

  private readonly float _flowRewardMultiplierRange;

  public FastRewardCalculator(float[] timeBonusThresholds, float flowBonusIdleThreshold, float flowRewardMultiplierRange = 0.5f)
  {
    _timeBonusThresholds = timeBonusThresholds ?? new float[] { 30, 60, 90, 120 };
    _flowBonusIdleThreshold = flowBonusIdleThreshold;
    _flowRewardMultiplierRange = flowRewardMultiplierRange;
  }

  /// <summary>
  /// Calculates rewards for a completed FAST stage.
  /// </summary>
  public FastRewardResult CalculateRewards(FastStageMetrics metrics, float flowFinal = 0f)
  {
    var result = new FastRewardResult
    {
      BaseReward = CalculateBaseReward(metrics),
      SpeedBonus = CalculateSpeedBonus(metrics.FinishTime),
      PerfectLineBonus = CalculatePerfectLineBonus(metrics)
    };

    // Apply Flow Meter multiplier
    float flowMultiplier = 1.0f + (flowFinal * _flowRewardMultiplierRange);
    result.TotalReward = (int)((result.BaseReward + result.SpeedBonus + result.PerfectLineBonus) * flowMultiplier);
    result.FlowMultiplier = flowMultiplier;

    return result;
  }

  /// <summary>
  /// Base completion reward (always granted).
  /// </summary>
  private int CalculateBaseReward(FastStageMetrics metrics)
  {
    // Base reward scales with stage difficulty and length
    int baseReward = 50; // Base amount
    baseReward += metrics.DifficultyStars * 10;
    baseReward += (int)(metrics.EstimatedLength * 0.5f); // Length bonus
    
    return baseReward;
  }

  /// <summary>
  /// Speed bonus based on finish time tiers.
  /// </summary>
  private int CalculateSpeedBonus(float finishTime)
  {
    if (_timeBonusThresholds.Length == 0)
      return 0;

    // Find which tier the finish time falls into
    int tier = 0;
    for (int i = 0; i < _timeBonusThresholds.Length; i++)
    {
      if (finishTime <= _timeBonusThresholds[i])
      {
        tier = _timeBonusThresholds.Length - i; // Higher tier = faster time
        break;
      }
    }

    // Bonus increases exponentially with tier
    return tier * tier * 25; // 25, 100, 225, 400, etc.
  }


  /// <summary>
  /// Perfect line bonus: no damage, no missed gates.
  /// </summary>
  private int CalculatePerfectLineBonus(FastStageMetrics metrics)
  {
    if (metrics.DamageTaken > 0 || metrics.MissedGates > 0)
      return 0;

    // Perfect execution bonus
    return 100;
  }
}

/// <summary>
/// Metrics for a completed FAST stage.
/// </summary>
public struct FastStageMetrics
{
  public float FinishTime; // Total time to complete stage
  public float TotalIdleTime; // Time spent idle/not moving
  public float AverageSpeed; // Average movement speed
  public int DamageTaken; // Total damage taken
  public int MissedGates; // Number of gates/checkpoints missed
  public int DifficultyStars; // Stage difficulty (1-5)
  public float EstimatedLength; // Estimated stage length in seconds
  public float FlowFinal; // Final Flow Meter value (0..1) from FlowSystem
}

/// <summary>
/// Reward result for a FAST stage.
/// </summary>
public struct FastRewardResult
{
  public int BaseReward;
  public int SpeedBonus;
  public int PerfectLineBonus;
  public float FlowMultiplier; // Flow Meter multiplier applied
  public int TotalReward;
}

