using System;
using GL2Engine.Engine;

namespace GL2Engine.UI;

/// <summary>
/// Seed selection UI stub for tier upgrade. Allows player to select seed category codes and symbols.
/// </summary>
public class SeedSelectionUI
{
  private int _tierIndex;
  private int _categoryId;
  private int[] _symbolIds;

  public SeedSelectionUI(int tierIndex)
  {
    _tierIndex = tierIndex;
    _categoryId = 0;
    _symbolIds = Array.Empty<int>();
  }

  /// <summary>
  /// Sets the category ID for seed resolution.
  /// </summary>
  public void SetCategoryId(int categoryId)
  {
    _categoryId = categoryId;
  }

  /// <summary>
  /// Sets the symbol IDs for seed resolution.
  /// </summary>
  public void SetSymbolIds(int[] symbolIds)
  {
    _symbolIds = new int[symbolIds.Length];
    Array.Copy(symbolIds, _symbolIds, symbolIds.Length);
  }

  /// <summary>
  /// Resolves the seed based on current selection.
  /// </summary>
  public ulong ResolveSeed()
  {
    return SeedResolver.ResolveSeed(_tierIndex, _categoryId, _symbolIds);
  }

  /// <summary>
  /// Locks the current seed selection (for tier mastery trophy).
  /// </summary>
  public SeedBadge LockSeed()
  {
    return new SeedBadge
    {
      TierIndex = _tierIndex,
      CategoryId = _categoryId,
      SymbolIds = _symbolIds,
      ResolvedSeed = ResolveSeed()
    };
  }

  // TODO: Actual UI rendering/input handling will be implemented later
  // For now, this is a data structure that can be used by systems
}

/// <summary>
/// Seed badge stored on Tier Mastery Trophy.
/// </summary>
public struct SeedBadge
{
  public int TierIndex;
  public int CategoryId;
  public int[] SymbolIds;
  public ulong ResolvedSeed;
}

