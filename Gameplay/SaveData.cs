using System;
using GL2Engine.World;
using GL2Engine.UI;

namespace GL2Engine.Gameplay;

/// <summary>
/// Save/load structure for game state.
/// </summary>
public class SaveData
{
  public int HighestTierReached { get; set; } = 1;
  public int CurrentTier { get; set; } = 1;
  public int Lives { get; set; } = 3;
  public SeedBadge[] TierMasteryTrophies { get; set; } = Array.Empty<SeedBadge>();

  /// <summary>
  /// Gets the seed badge for a tier, if it exists.
  /// </summary>
  public SeedBadge? GetTierMasteryTrophy(int tierIndex)
  {
    foreach (var trophy in TierMasteryTrophies)
    {
      if (trophy.TierIndex == tierIndex)
        return trophy;
    }
    return null;
  }

  /// <summary>
  /// Sets the seed badge for a tier (on mastery completion).
  /// </summary>
  public void SetTierMasteryTrophy(SeedBadge badge)
  {
    // Find existing trophy or add new one
    for (int i = 0; i < TierMasteryTrophies.Length; i++)
    {
      if (TierMasteryTrophies[i].TierIndex == badge.TierIndex)
      {
        TierMasteryTrophies[i] = badge;
        return;
      }
    }

    // Add new trophy
    var newTrophies = new SeedBadge[TierMasteryTrophies.Length + 1];
    Array.Copy(TierMasteryTrophies, newTrophies, TierMasteryTrophies.Length);
    newTrophies[TierMasteryTrophies.Length] = badge;
    TierMasteryTrophies = newTrophies;
  }
}

