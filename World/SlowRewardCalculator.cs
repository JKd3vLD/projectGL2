using System;
using GL2Engine.Inventory;

namespace GL2Engine.World;

/// <summary>
/// Calculates rewards for SLOW stages based on exploration, quest completion, and collection streaks.
/// </summary>
public class SlowRewardCalculator
{
  private readonly int _maxSidePockets;
  private readonly int _secretQuotaPerStage;
  private readonly int _bonusDoorQuotaPerStage;

  public SlowRewardCalculator(int maxSidePockets, int secretQuotaPerStage, int bonusDoorQuotaPerStage)
  {
    _maxSidePockets = maxSidePockets;
    _secretQuotaPerStage = secretQuotaPerStage;
    _bonusDoorQuotaPerStage = bonusDoorQuotaPerStage;
  }

  private readonly float _flowRewardMultiplierRange;

  public SlowRewardCalculator(int maxSidePockets, int secretQuotaPerStage, int bonusDoorQuotaPerStage, float flowRewardMultiplierRange = 0.5f)
  {
    _maxSidePockets = maxSidePockets;
    _secretQuotaPerStage = secretQuotaPerStage;
    _bonusDoorQuotaPerStage = bonusDoorQuotaPerStage;
    _flowRewardMultiplierRange = flowRewardMultiplierRange;
  }

  /// <summary>
  /// Calculates rewards for a completed SLOW stage.
  /// </summary>
  public SlowRewardResult CalculateRewards(SlowStageMetrics metrics, float flowFinal = 0f)
  {
    var result = new SlowRewardResult
    {
      BaseReward = CalculateBaseReward(metrics),
      ExplorationBonus = CalculateExplorationBonus(metrics),
      QuestBonus = CalculateQuestBonus(metrics),
      CollectionStreakBonus = CalculateCollectionStreakBonus(metrics)
    };

    // Apply Flow Meter multiplier
    float flowMultiplier = 1.0f + (flowFinal * _flowRewardMultiplierRange);
    result.TotalReward = (int)((result.BaseReward + result.ExplorationBonus + result.QuestBonus + result.CollectionStreakBonus) * flowMultiplier);
    result.FlowMultiplier = flowMultiplier;

    return result;
  }

  /// <summary>
  /// Base completion reward (always granted).
  /// </summary>
  private int CalculateBaseReward(SlowStageMetrics metrics)
  {
    // Base reward scales with stage difficulty and exploration potential
    int baseReward = 75; // Higher base for SLOW stages
    baseReward += metrics.DifficultyStars * 15;
    baseReward += metrics.SidePocketsCompleted * 20;
    
    return baseReward;
  }

  /// <summary>
  /// Exploration bonus for finding treasures, secrets, and bonus rooms.
  /// </summary>
  private int CalculateExplorationBonus(SlowStageMetrics metrics)
  {
    int bonus = 0;

    // Treasure found bonus
    bonus += metrics.TreasuresFound * 10;

    // Secrets found bonus (diminishing returns after quota)
    int secretBonus = Math.Min(metrics.SecretsFound, _secretQuotaPerStage) * 15;
    if (metrics.SecretsFound > _secretQuotaPerStage)
    {
      // Diminishing returns: half value for extras
      int extraSecrets = metrics.SecretsFound - _secretQuotaPerStage;
      secretBonus += extraSecrets * 7; // Half value
    }
    bonus += secretBonus;

    // Bonus doors/minigames completed
    int bonusDoorBonus = Math.Min(metrics.BonusDoorsCompleted, _bonusDoorQuotaPerStage) * 25;
    if (metrics.BonusDoorsCompleted > _bonusDoorQuotaPerStage)
    {
      int extraDoors = metrics.BonusDoorsCompleted - _bonusDoorQuotaPerStage;
      bonusDoorBonus += extraDoors * 12; // Diminishing returns
    }
    bonus += bonusDoorBonus;

    return bonus;
  }

  /// <summary>
  /// Quest bonus for collecting quest-tagged items (flowers, bugs, etc.).
  /// </summary>
  private int CalculateQuestBonus(SlowStageMetrics metrics)
  {
    // Quest items give higher value
    return metrics.QuestItemsCollected * 20;
  }

  /// <summary>
  /// Collection streak bonus for finding multiple treasures without taking damage.
  /// </summary>
  private int CalculateCollectionStreakBonus(SlowStageMetrics metrics)
  {
    if (metrics.DamageTaken > 0)
      return 0; // No streak bonus if damage taken

    // Bonus scales with collection streak length
    int streakLength = metrics.CollectionStreak;
    if (streakLength < 3)
      return 0; // Need at least 3 for streak bonus

    // Exponential bonus for longer streaks
    return streakLength * streakLength * 5; // 45, 80, 125, etc.
  }
}

/// <summary>
/// Metrics for a completed SLOW stage.
/// </summary>
public struct SlowStageMetrics
{
  public int TreasuresFound; // Number of treasure chests found
  public int SecretsFound; // Number of secrets discovered
  public int BonusDoorsCompleted; // Number of bonus doors/minigames completed
  public int QuestItemsCollected; // Number of quest-tagged items collected
  public int CollectionStreak; // Consecutive treasures found without damage
  public int DamageTaken; // Total damage taken
  public int SidePocketsCompleted; // Number of side exploration areas completed
  public int DifficultyStars; // Stage difficulty (1-5)
  public bool CarryObjectiveCompleted; // Whether carry-object mission was completed
  public float FlowFinal; // Final Flow Meter value (0..1) from FlowSystem
}

/// <summary>
/// Reward result for a SLOW stage.
/// </summary>
public struct SlowRewardResult
{
  public int BaseReward;
  public int ExplorationBonus;
  public int QuestBonus;
  public int CollectionStreakBonus;
  public float FlowMultiplier; // Flow Meter multiplier applied
  public int TotalReward;
}

