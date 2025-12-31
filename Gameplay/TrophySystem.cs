using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GL2Engine.ECS;
using GL2Engine.World;
using GL2Engine.UI;

namespace GL2Engine.Gameplay;

/// <summary>
/// Manages Trophy Stamps (immutable snapshots of stage/tier completion).
/// </summary>
public class TrophySystem
{
  private readonly GameWorld _world;
  private readonly Dictionary<string, StageTrophyStamp> _stageStamps = new Dictionary<string, StageTrophyStamp>();
  private readonly Dictionary<int, TierMasteryTrophy> _tierTrophies = new Dictionary<int, TierMasteryTrophy>();
  private TrophyTuning _tuning;

  public TrophySystem(GameWorld world)
  {
    _world = world;
    LoadTuning();
  }

  private void LoadTuning()
  {
    try
    {
      var json = File.ReadAllText("GL2Project/Tuning/TrophyTuning.json");
      var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
      _tuning = JsonSerializer.Deserialize<TrophyTuning>(json, options) ?? new TrophyTuning();
    }
    catch
    {
      _tuning = new TrophyTuning();
    }
  }

  /// <summary>
  /// Create a stage trophy stamp from completion metrics.
  /// </summary>
  public StageTrophyStamp CreateStageStamp(string stageId, StageCompletionMetrics metrics)
  {
    // Get Flow final value
    float flowFinal = 0f;
    if (_world.FlowComponents.Has(_world.PlayerEntity))
    {
      ref var flow = ref _world.FlowComponents.Get(_world.PlayerEntity);
      flowFinal = flow.Flow / 1.0f; // Normalize (assuming FlowMax = 1.0)
    }

    // Calculate Flow Grade
    FlowGrade flowGrade = CalculateFlowGrade(flowFinal);

    // Calculate Time Tier (FAST only)
    int timeTier = 4; // Default: worst tier
    if (metrics.PacingTag == PacingTag.FAST && metrics.FinishTime > 0f)
    {
      timeTier = CalculateTimeTier(metrics.FinishTime);
    }

    var stamp = new StageTrophyStamp
    {
      TierIndex = metrics.TierIndex,
      StageId = stageId,
      PacingTag = metrics.PacingTag,
      SeedBadgeHash = metrics.SeedBadgeHash,
      TimeSeconds = metrics.FinishTime,
      TimeTier = timeTier,
      FlowFinal = flowFinal,
      FlowGrade = flowGrade,
      SecretsFound = metrics.SecretsFound,
      BonusCompleted = metrics.BonusCompleted,
      CarryDelivered = metrics.CarryDelivered,
      DamageTaken = metrics.DamageTaken,
      Deaths = metrics.Deaths,
      RewardQuality = metrics.RewardQuality
    };

    _stageStamps[stageId] = stamp;
    return stamp;
  }

  /// <summary>
  /// Create a tier mastery trophy from stage stamps.
  /// </summary>
  public TierMasteryTrophy CreateTierTrophy(int tierIndex, BiomeSignature biomeSignature, SeedBadge seedBadge, List<StageTrophyStamp> stageStamps, bool masteryComplete)
  {
    var trophy = new TierMasteryTrophy
    {
      TierIndex = tierIndex,
      BiomeSignature = biomeSignature,
      SeedBadge = seedBadge,
      StageStamps = stageStamps.ToArray(),
      MasteryComplete = masteryComplete,
      CompletionTime = DateTime.Now
    };

    _tierTrophies[tierIndex] = trophy;
    return trophy;
  }

  private FlowGrade CalculateFlowGrade(float flowFinal)
  {
    // Normalize to [0, 1]
    flowFinal = Math.Clamp(flowFinal, 0f, 1f);

    // Compare thresholds (lower bound)
    if (flowFinal < _tuning.FlowGradeThresholds[0])
      return FlowGrade.D;
    if (flowFinal < _tuning.FlowGradeThresholds[1])
      return FlowGrade.C;
    if (flowFinal < _tuning.FlowGradeThresholds[2])
      return FlowGrade.B;
    if (flowFinal < _tuning.FlowGradeThresholds[3])
      return FlowGrade.A;
    return FlowGrade.S;
  }

  private int CalculateTimeTier(float finishTime)
  {
    // Use thresholds from StageGenerationTuning
    float[] thresholds = { 30f, 60f, 90f, 120f }; // Default thresholds

    for (int i = 0; i < thresholds.Length; i++)
    {
      if (finishTime <= thresholds[i])
      {
        return i; // 0 = best tier
      }
    }
    return thresholds.Length; // Worst tier
  }

  /// <summary>
  /// Get stage stamp for a stage ID.
  /// </summary>
  public StageTrophyStamp? GetStageStamp(string stageId)
  {
    return _stageStamps.TryGetValue(stageId, out var stamp) ? stamp : null;
  }

  /// <summary>
  /// Get tier trophy for a tier index.
  /// </summary>
  public TierMasteryTrophy? GetTierTrophy(int tierIndex)
  {
    return _tierTrophies.TryGetValue(tierIndex, out var trophy) ? trophy : null;
  }
}

/// <summary>
/// Stage trophy stamp (immutable snapshot).
/// </summary>
public struct StageTrophyStamp
{
  public int TierIndex;
  public string StageId;
  public PacingTag PacingTag;
  public ulong SeedBadgeHash;
  public float TimeSeconds;
  public int TimeTier;
  public float FlowFinal;
  public FlowGrade FlowGrade;
  public int SecretsFound;
  public int BonusCompleted;
  public int CarryDelivered;
  public int DamageTaken;
  public int Deaths;
  public int RewardQuality;
}

/// <summary>
/// Flow Grade (D/C/B/A/S).
/// </summary>
public enum FlowGrade
{
  D,
  C,
  B,
  A,
  S
}

/// <summary>
/// Stage completion metrics for trophy creation.
/// </summary>
public struct StageCompletionMetrics
{
  public int TierIndex;
  public PacingTag PacingTag;
  public ulong SeedBadgeHash;
  public float FinishTime;
  public int SecretsFound;
  public int BonusCompleted;
  public int CarryDelivered;
  public int DamageTaken;
  public int Deaths;
  public int RewardQuality;
}

/// <summary>
/// Tier mastery trophy (extends TierMastery.cs).
/// </summary>
public class TierMasteryTrophy
{
  public int TierIndex;
  public BiomeSignature BiomeSignature;
  public SeedBadge SeedBadge;
  public StageTrophyStamp[] StageStamps;
  public bool MasteryComplete;
  public DateTime CompletionTime;
}

/// <summary>
/// Trophy tuning parameters.
/// </summary>
public class TrophyTuning
{
  public float[] FlowGradeThresholds { get; set; } = { 0.25f, 0.5f, 0.75f, 0.9f }; // D/C/B/A/S
  public float[] TimeTierThresholdsFast { get; set; } = { 30f, 60f, 90f, 120f };
  public TrophyRetentionOnGameOver TrophyRetentionOnGameOver { get; set; } = TrophyRetentionOnGameOver.KeepAll;
}

public enum TrophyRetentionOnGameOver
{
  KeepAll,
  ClearOnGameOver
}

