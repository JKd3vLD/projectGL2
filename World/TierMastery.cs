using GL2Engine.UI;

namespace GL2Engine.World;

/// <summary>
/// Tier mastery tracking and unlock system.
/// </summary>
public class TierMastery
{
  /// <summary>
  /// Checks if a tier is mastered (all mastery requirements met).
  /// </summary>
  public static bool IsTierMastered(TierPackage package, MasteryRequirements requirements)
  {
    return requirements.IsComplete();
  }

  /// <summary>
  /// Creates a seed badge from seed selection UI for tier mastery trophy.
  /// </summary>
  public static SeedBadge CreateSeedBadge(SeedSelectionUI seedUI)
  {
    return seedUI.LockSeed();
  }
}

