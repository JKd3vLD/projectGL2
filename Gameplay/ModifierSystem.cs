using System;
using System.Collections.Generic;
using System.Linq;
using GL2Engine.ECS;
using GL2Engine.World;
using static GL2Engine.World.SectionDef; // For TraversalMode, InteractionTags

namespace GL2Engine.Gameplay;

/// <summary>
/// Manages Rule Modifiers (Assist + Challenge framework).
/// </summary>
public class ModifierSystem
{
  private readonly GameWorld _world;
  private readonly List<RuleModifierDef> _activeModifiers = new List<RuleModifierDef>();
  private ModifierTuning _tuning;

  public ModifierSystem(GameWorld world)
  {
    _world = world;
    LoadTuning();
  }

  private void LoadTuning()
  {
    try
    {
      var json = System.IO.File.ReadAllText("GL2Project/Tuning/ModifierTuning.json");
      var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
      _tuning = System.Text.Json.JsonSerializer.Deserialize<ModifierTuning>(json, options) ?? new ModifierTuning();
    }
    catch
    {
      _tuning = new ModifierTuning();
    }
  }

  /// <summary>
  /// Activate a modifier.
  /// </summary>
  public bool ActivateModifier(RuleModifierDef modifier)
  {
    if (_activeModifiers.Count >= _tuning.ModifierMaxActive)
      return false;

    // Check compatibility with existing modifiers
    foreach (var existing in _activeModifiers)
    {
      if (AreIncompatible(modifier, existing))
        return false;
    }

    _activeModifiers.Add(modifier);
    UpdateActiveModifiersComponent();
    return true;
  }

  /// <summary>
  /// Deactivate a modifier.
  /// </summary>
  public void DeactivateModifier(string modifierId)
  {
    _activeModifiers.RemoveAll(m => m.Id == modifierId);
    UpdateActiveModifiersComponent();
  }

  private void UpdateActiveModifiersComponent()
  {
    if (!_world.ActiveModifiers.Has(_world.PlayerEntity))
    {
      _world.ActiveModifiers.Add(_world.PlayerEntity, new ActiveModifiersComponent
      {
        ModifierIds = new int[10],
        ModifierCount = 0,
        RewardMultiplier = 1.0f
      });
    }

    ref var active = ref _world.ActiveModifiers.Get(_world.PlayerEntity);
    active.ModifierCount = Math.Min(_activeModifiers.Count, 10);

    // Calculate aggregate multiplier
    float multiplier = 1.0f;
    foreach (var mod in _activeModifiers)
    {
      multiplier *= mod.RewardMultiplier;
    }
    active.RewardMultiplier = Math.Min(multiplier, _tuning.MaxAggregateMultiplier);

    // Store modifier IDs (hash-based for now)
    for (int i = 0; i < active.ModifierCount; i++)
    {
      active.ModifierIds[i] = _activeModifiers[i].Id.GetHashCode();
    }
  }

  /// <summary>
  /// Check if input is allowed (not disabled by modifiers).
  /// </summary>
  public bool CheckInput(InputAction action)
  {
    foreach (var modifier in _activeModifiers)
    {
      if ((modifier.DisabledInputs & (1UL << (int)action)) != 0)
        return false;
    }
    return true;
  }

  /// <summary>
  /// Filter sections based on active modifiers.
  /// </summary>
  public bool IsSectionCompatible(SectionDef section)
  {
    foreach (var modifier in _activeModifiers)
    {
      var filtersNullable = modifier.PoolFilters;
      if (filtersNullable.HasValue)
      {
        var filters = filtersNullable.Value;
        // Check if section requires disabled InteractionTags
        if ((section.InteractionTags & filters.DisallowedInteractionTags) != 0)
          return false;

        // Check if section requires disabled TraversalMode
        if (filters.DisallowedTraversalModes != null &&
            filters.DisallowedTraversalModes.Contains(section.TraversalMode))
          return false;
      }
    }
    return true;
  }

  private bool AreIncompatible(RuleModifierDef a, RuleModifierDef b)
  {
    // Example: "No Jump" + "No Glide" = incompatible
    if (a.Id == "no_jump" && b.Id == "no_glide")
      return true;
    if (a.Id == "no_glide" && b.Id == "no_jump")
      return true;
    return false;
  }

  public void Update(float dt)
  {
    // Update stage-scoped modifiers (reset on stage exit)
    // TODO: Implement stage-scoped modifier reset
  }

  /// <summary>
  /// Get aggregate reward multiplier.
  /// </summary>
  public float GetRewardMultiplier()
  {
    if (!_world.ActiveModifiers.Has(_world.PlayerEntity))
      return 1.0f;

    ref var active = ref _world.ActiveModifiers.Get(_world.PlayerEntity);
    return active.RewardMultiplier;
  }
}

/// <summary>
/// Rule modifier definition.
/// </summary>
public struct RuleModifierDef
{
  public string Id;
  public string Name;
  public ModifierScope Scope;
  public ModifierFlags Flags;
  public ulong DisabledInputs; // Bitset of disabled input actions
  public float RewardMultiplier;
  public ModifierPoolFilters? PoolFilters;
}

public enum ModifierScope
{
  Run,   // Persists until game over
  Stage  // Resets on stage exit
}

[Flags]
public enum ModifierFlags
{
  None = 0,
  Assist = 1 << 0,
  Challenge = 1 << 1
}

public struct ModifierPoolFilters
{
  public InteractionTags DisallowedInteractionTags;
  public List<TraversalMode>? DisallowedTraversalModes;
}

public enum InputAction
{
  Jump,
  Glide,
  Crouch,
  Cartwheel,
  Left,
  Right,
  Up,
  Down
}

/// <summary>
/// Modifier tuning parameters.
/// </summary>
public class ModifierTuning
{
  public float AssistRewardMultiplier { get; set; } = 0.75f;
  public float ChallengeRewardMultiplier { get; set; } = 1.25f;
  public int ModifierMaxActive { get; set; } = 3;
  public float MaxAggregateMultiplier { get; set; } = 2.0f;
}


