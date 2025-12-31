using GL2Engine.World;
using GL2Engine.Engine;

namespace GL2Engine.Gameplay;

/// <summary>
/// Game Over system handling keep/lose mapping and tier regeneration.
/// </summary>
public class GameOverSystem
{
  private SaveData _saveData;

  public GameOverSystem(SaveData saveData)
  {
    _saveData = saveData;
  }

  /// <summary>
  /// Handles game over. Applies keep/lose mapping and regenerates to TierStart.
  /// </summary>
  public void HandleGameOver(GL2Engine.Inventory.Inventory inventory, GL2Engine.Inventory.ItemCatalog catalog)
  {
    // Apply keep/lose mapping (stub for now)
    ApplyKeepLoseMapping(inventory, catalog);

    // Calculate TierStart = max(1, HighestTierReached - 3)
    int tierStart = System.Math.Max(1, _saveData.HighestTierReached - 3);

    // Regenerate with RANDOM seeds (unless seed locks are explicitly set)
    // TODO: Implement tier regeneration with random seeds

    // Reset lives to 3
    _saveData.Lives = 3;
  }

  private void ApplyKeepLoseMapping(GL2Engine.Inventory.Inventory inventory, GL2Engine.Inventory.ItemCatalog catalog)
  {
    // Stub: Clear run-scoped items
    inventory.ClearRunScopedItems(catalog);

    // TODO: Implement full keep/lose mapping based on game rules
  }
}

