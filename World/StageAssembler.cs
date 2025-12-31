using System;
using System.Collections.Generic;
using System.Linq;
using GL2Engine.Engine;

namespace GL2Engine.World;

/// <summary>
/// Assembles stages from handcrafted sections based on pacing, tier, and biome signature.
/// </summary>
public class StageAssembler
{
  private readonly SectionPool _sectionPool;
  private readonly Rng _rng;

  public StageAssembler(SectionPool sectionPool, Rng rng)
  {
    _sectionPool = sectionPool;
    _rng = rng;
  }

  /// <summary>
  /// Assembles a stage plan from sections matching the criteria.
  /// </summary>
  public StagePlan AssembleStage(int tier, BiomeSignature signature, PacingTag pacing, RewardProfile rewardProfile, GL2Engine.Gameplay.ModifierSystem? modifierSystem = null)
  {
    var plan = new StagePlan
    {
      PacingTag = pacing,
      RewardProfile = rewardProfile
    };

    // Check if pool is exhausted and reset if needed
    bool needsRecolor = _sectionPool.ResetIfExhausted(tier, signature, pacing);
    // TODO: Apply recolor/retexture flag to plan if needed

    // Get available sections
    var availableSections = _sectionPool.GetAvailableSections(tier, signature, pacing);
    
    // Filter sections based on active modifiers
    if (modifierSystem != null)
    {
      availableSections = availableSections.Where(s => modifierSystem.IsSectionCompatible(s)).ToList();
    }
    
    if (availableSections.Count == 0)
    {
      throw new InvalidOperationException($"No sections available for tier {tier}, signature {signature}, pacing {pacing}");
    }

    // Select sections based on pacing rules
    List<SectionDef> selectedSections;
    if (pacing == PacingTag.FAST)
    {
      selectedSections = SelectFastSections(availableSections, tier);
    }
    else
    {
      selectedSections = SelectSlowSections(availableSections, tier, out var sidePockets);
      plan.SidePockets = sidePockets;
    }

    // Apply difficulty ramp
    ApplyDifficultyRamp(selectedSections, plan);

    // Enforce connector compatibility
    EnforceConnectorCompatibility(selectedSections);

    // Mark sections as used
    foreach (var section in selectedSections)
    {
      _sectionPool.MarkUsed(tier, signature, pacing, section.Id);
    }

    plan.Sections = selectedSections;

    // Place flags procedurally
    PlaceFlags(plan);

    return plan;
  }

  /// <summary>
  /// Selects sections for a FAST stage (3-6 sections, prefer SHORT/MED).
  /// </summary>
  private List<SectionDef> SelectFastSections(List<SectionDef> available, int tier)
  {
    int sectionCount = _rng.NextInt(3, 7); // 3-6 sections

    // Filter by preferred length classes
    var preferred = available.Where(s => 
      s.LengthClass == LengthClass.SHORT || s.LengthClass == LengthClass.MED
    ).ToList();

    var candidates = preferred.Count > 0 ? preferred : available;
    
    // Select sections ensuring variety
    var selected = new List<SectionDef>();
    var usedIds = new HashSet<string>();

    for (int i = 0; i < sectionCount && candidates.Count > 0; i++)
    {
      // Filter out already selected
      var remaining = candidates.Where(s => !usedIds.Contains(s.Id)).ToList();
      if (remaining.Count == 0)
        break;

      int index = _rng.NextInt(0, remaining.Count);
      var section = remaining[index];
      selected.Add(section);
      usedIds.Add(section.Id);
    }

    return selected;
  }

  /// <summary>
  /// Selects sections for a SLOW stage (2-4 sections, prefer MED/LONG) + side pockets.
  /// </summary>
  private List<SectionDef> SelectSlowSections(List<SectionDef> available, int tier, out List<SectionDef> sidePockets)
  {
    int sectionCount = _rng.NextInt(2, 5); // 2-4 sections

    // Filter by preferred length classes
    var preferred = available.Where(s => 
      s.LengthClass == LengthClass.MED || s.LengthClass == LengthClass.LONG
    ).ToList();

    var candidates = preferred.Count > 0 ? preferred : available;
    
    // Select main sections
    var selected = new List<SectionDef>();
    var usedIds = new HashSet<string>();

    for (int i = 0; i < sectionCount && candidates.Count > 0; i++)
    {
      var remaining = candidates.Where(s => !usedIds.Contains(s.Id)).ToList();
      if (remaining.Count == 0)
        break;

      int index = _rng.NextInt(0, remaining.Count);
      var section = remaining[index];
      selected.Add(section);
      usedIds.Add(section.Id);
    }

    // Select side pockets (exploration sections)
    sidePockets = new List<SectionDef>();
    int maxSidePockets = 3;
    var sidePocketCandidates = available.Where(s => 
      !usedIds.Contains(s.Id) &&
      (s.InteractionTags & (InteractionTags.BonusDoor | InteractionTags.MinigamePortal | InteractionTags.OpenExploration)) != InteractionTags.None
    ).ToList();

    int sidePocketCount = Math.Min(maxSidePockets, _rng.NextInt(1, sidePocketCandidates.Count + 1));
    for (int i = 0; i < sidePocketCount && sidePocketCandidates.Count > 0; i++)
    {
      int index = _rng.NextInt(0, sidePocketCandidates.Count);
      sidePockets.Add(sidePocketCandidates[index]);
      sidePocketCandidates.RemoveAt(index);
    }

    return selected;
  }

  /// <summary>
  /// Applies difficulty ramp (Teach→Test→Twist→Finale) to sections.
  /// </summary>
  private void ApplyDifficultyRamp(List<SectionDef> sections, StagePlan plan)
  {
    if (sections.Count == 0)
      return;

    plan.DifficultyRamp = new List<DifficultyRamp>();

    // Assign ramp positions based on section count
    for (int i = 0; i < sections.Count; i++)
    {
      DifficultyRamp ramp;
      float progress = (float)i / Math.Max(1, sections.Count - 1);

      if (progress < 0.25f)
        ramp = DifficultyRamp.Teach;
      else if (progress < 0.5f)
        ramp = DifficultyRamp.Test;
      else if (progress < 0.75f)
        ramp = DifficultyRamp.Twist;
      else
        ramp = DifficultyRamp.Finale;

      plan.DifficultyRamp.Add(ramp);
    }

    // Ensure no two consecutive 5-star sections
    for (int i = 0; i < sections.Count - 1; i++)
    {
      if (sections[i].DifficultyStars == 5 && sections[i + 1].DifficultyStars == 5)
      {
        // Reduce second section's difficulty if possible
        if (sections[i + 1].DifficultyStars > 1)
        {
          // Note: Can't modify struct directly, would need to replace in list
          // For now, this is a constraint that should be handled in selection
        }
      }
    }
  }

  /// <summary>
  /// Ensures connector compatibility between adjacent sections.
  /// </summary>
  private void EnforceConnectorCompatibility(List<SectionDef> sections)
  {
    // For now, assume all sections use "ground" connectors
    // TODO: Implement proper connector matching logic
    for (int i = 0; i < sections.Count - 1; i++)
    {
      var current = sections[i];
      var next = sections[i + 1];

      // Check if connectors match
      bool hasCompatibleConnector = false;
      foreach (var outConnector in current.ConnectorsOut)
      {
        if (next.ConnectorsIn.Contains(outConnector))
        {
          hasCompatibleConnector = true;
          break;
        }
      }

      if (!hasCompatibleConnector && current.ConnectorsOut.Count > 0 && next.ConnectorsIn.Count > 0)
      {
        // Add default ground connector if missing
        if (!current.ConnectorsOut.Contains("ground"))
        {
          current.ConnectorsOut.Add("ground");
        }
        if (!next.ConnectorsIn.Contains("ground"))
        {
          next.ConnectorsIn.Add("ground");
        }
      }
    }
  }

  /// <summary>
  /// Places flags procedurally: start, middle (if LONG sections), end.
  /// </summary>
  private void PlaceFlags(StagePlan plan)
  {
    if (plan.Sections.Count == 0)
      return;

    // Start flag at beginning of first section
    plan.Flags.Add(new FlagPosition
    {
      Type = FlagType.Start,
      Position = new Microsoft.Xna.Framework.Vector2(0, 0), // Will be set from section data
      SectionIndex = 0,
      IsConsumable = false
    });

    // Middle flags: end of LONG sections (if stage has 3+ sections)
    if (plan.Sections.Count >= 3)
    {
      for (int i = 0; i < plan.Sections.Count - 1; i++)
      {
        if (plan.Sections[i].LengthClass == LengthClass.LONG)
        {
          plan.Flags.Add(new FlagPosition
          {
            Type = FlagType.Middle,
            Position = new Microsoft.Xna.Framework.Vector2(0, 0), // Will be set from section data
            SectionIndex = i,
            IsConsumable = true // Middle flags are consumable
          });
        }
      }
    }

    // End flag at end of last section
    plan.Flags.Add(new FlagPosition
    {
      Type = FlagType.End,
      Position = new Microsoft.Xna.Framework.Vector2(0, 0), // Will be set from section data
      SectionIndex = plan.Sections.Count - 1,
      IsConsumable = false
    });
  }
}

