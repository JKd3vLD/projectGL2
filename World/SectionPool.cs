using System;
using System.Collections.Generic;
using System.Linq;

namespace GL2Engine.World;

/// <summary>
/// Pool of sections filtered by tier, biome signature, and pacing tag.
/// Maintains history to prevent repeats until pool is exhausted.
/// </summary>
public class SectionPool
{
  private readonly Dictionary<string, List<string>> _historyLists; // Key: (Tier,BiomeSignature,PacingTag)
  private readonly List<SectionDef> _allSections;

  public SectionPool(List<SectionDef> allSections)
  {
    _allSections = allSections ?? new List<SectionDef>();
    _historyLists = new Dictionary<string, List<string>>();
  }

  /// <summary>
  /// Gets available sections matching the criteria, excluding those in history.
  /// </summary>
  public List<SectionDef> GetAvailableSections(int tier, BiomeSignature signature, PacingTag pacing)
  {
    string historyKey = GetHistoryKey(tier, signature, pacing);
    
    var available = _allSections.Where(s =>
      s.TierMin <= tier && s.TierMax >= tier &&
      MatchesBiomeSignature(s, signature) &&
      s.PacingTag == pacing &&
      !IsInHistory(historyKey, s.Id)
    ).ToList();

    return available;
  }

  /// <summary>
  /// Marks a section as used in the history list.
  /// </summary>
  public void MarkUsed(int tier, BiomeSignature signature, PacingTag pacing, string sectionId)
  {
    string historyKey = GetHistoryKey(tier, signature, pacing);
    
    if (!_historyLists.ContainsKey(historyKey))
    {
      _historyLists[historyKey] = new List<string>();
    }

    if (!_historyLists[historyKey].Contains(sectionId))
    {
      _historyLists[historyKey].Add(sectionId);
    }
  }

  /// <summary>
  /// Checks if pool is exhausted and resets history if so.
  /// </summary>
  public bool ResetIfExhausted(int tier, BiomeSignature signature, PacingTag pacing)
  {
    string historyKey = GetHistoryKey(tier, signature, pacing);
    
    if (!_historyLists.ContainsKey(historyKey))
      return false;

    var available = GetAvailableSections(tier, signature, pacing);
    
    if (available.Count == 0)
    {
      // Pool exhausted, reset history and apply recolor/retexture
      _historyLists[historyKey].Clear();
      return true; // Indicates recolor/retexture should be applied
    }

    return false;
  }

  /// <summary>
  /// Clears history for a specific key (for testing/debugging).
  /// </summary>
  public void ClearHistory(int tier, BiomeSignature signature, PacingTag pacing)
  {
    string historyKey = GetHistoryKey(tier, signature, pacing);
    _historyLists.Remove(historyKey);
  }

  private string GetHistoryKey(int tier, BiomeSignature signature, PacingTag pacing)
  {
    return $"{tier}_{signature}_{pacing}";
  }

  private bool IsInHistory(string historyKey, string sectionId)
  {
    if (!_historyLists.ContainsKey(historyKey))
      return false;
    
    return _historyLists[historyKey].Contains(sectionId);
  }

  private bool MatchesBiomeSignature(SectionDef section, BiomeSignature signature)
  {
    // Check if section's biome tags match the signature
    bool hasA = signature.HasA && section.BiomeTags.Contains("A");
    bool hasB = signature.HasB && section.BiomeTags.Contains("B");
    bool hasC = signature.HasC && section.BiomeTags.Contains("C");

    // Section must match at least one biome in the signature
    return hasA || hasB || hasC;
  }
}

