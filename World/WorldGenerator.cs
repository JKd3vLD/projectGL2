using System;
using System.Collections.Generic;
using System.Linq;
using GL2Engine.Engine;
using GL2Engine.Content;

namespace GL2Engine.World;

/// <summary>
/// Generates TierPackages with 3 biomes and 7 stages.
/// </summary>
public static class WorldGenerator
{
  private static List<string> _stageHistory = new List<string>();
  private static SectionPool? _sectionPool;
  private static StageAssembler? _stageAssembler;

  /// <summary>
  /// Initializes the world generator with loaded sections.
  /// </summary>
  public static void Initialize(ModLoader modLoader, Rng worldGenRng)
  {
    // Load all sections
    var allSections = SectionLoader.LoadAllSections(modLoader, "GL2Project/Mods");
    _sectionPool = new SectionPool(allSections);
    _stageAssembler = new StageAssembler(_sectionPool, worldGenRng);
  }

  /// <summary>
  /// Generates a TierPackage for the given tier index using the WorldGen RNG stream.
  /// </summary>
  public static TierPackage GenerateTierPackage(int tierIndex, Rng worldGenStream, PacingTag? defaultPacing = null, ModLoader? modLoader = null)
  {
    // Initialize section system if modLoader is provided
    if (modLoader != null && _sectionPool == null)
    {
      Initialize(modLoader, worldGenStream);
    }

    var package = new TierPackage
    {
      TierIndex = tierIndex,
      WorldGenSeed = (ulong)worldGenStream.Next() | ((ulong)worldGenStream.Next() << 32)
    };

    // Select 3 biomes (for now, hardcoded - will be loaded from content packs later)
    package.Biomes[0] = new Biome { Id = "biome_a", Name = "Biome A", AvailableStages = new[] { "stage_a_1", "stage_a_2" } };
    package.Biomes[1] = new Biome { Id = "biome_b", Name = "Biome B", AvailableStages = new[] { "stage_b_1", "stage_b_2" } };
    package.Biomes[2] = new Biome { Id = "biome_c", Name = "Biome C", AvailableStages = new[] { "stage_c_1", "stage_c_2" } };

    // Generate 7 stages
    package.Stages[0] = GenerateStage("pure_a", package.Biomes[0], new BiomeSignature { HasA = true }, worldGenStream, tierIndex, defaultPacing);
    package.Stages[1] = GenerateStage("pure_b", package.Biomes[1], new BiomeSignature { HasB = true }, worldGenStream, tierIndex, defaultPacing);
    package.Stages[2] = GenerateStage("pure_c", package.Biomes[2], new BiomeSignature { HasC = true }, worldGenStream, tierIndex, defaultPacing);
    package.Stages[3] = GenerateStage("mixed_ab", package.Biomes[0], new BiomeSignature { HasA = true, HasB = true }, worldGenStream, tierIndex, defaultPacing);
    package.Stages[4] = GenerateStage("mixed_bc", package.Biomes[1], new BiomeSignature { HasB = true, HasC = true }, worldGenStream, tierIndex, defaultPacing);
    package.Stages[5] = GenerateStage("mixed_ca", package.Biomes[2], new BiomeSignature { HasC = true, HasA = true }, worldGenStream, tierIndex, defaultPacing);
    package.Stages[6] = GenerateStage("mastery_abc", package.Biomes[0], new BiomeSignature { HasA = true, HasB = true, HasC = true }, worldGenStream, tierIndex, defaultPacing);

    return package;
  }

  private static Stage GenerateStage(string stageId, Biome primaryBiome, BiomeSignature signature, Rng rng, int tierIndex, PacingTag? pacing = null)
  {
    // Determine pacing (default to FAST if not specified)
    PacingTag stagePacing = pacing ?? PacingTag.FAST;
    
    // Determine reward profile based on stage type
    RewardProfile rewardProfile = DetermineRewardProfile(stageId, stagePacing);

    // Use StageAssembler if available, otherwise fall back to legacy system
    if (_stageAssembler != null && _sectionPool != null)
    {
      try
      {
        var stagePlan = _stageAssembler.AssembleStage(
          tier: tierIndex,
          signature: signature,
          pacing: stagePacing,
          rewardProfile: rewardProfile
        );

        return new Stage
        {
          Id = stageId,
          Name = stageId,
          Signature = signature,
          LevelDataPath = "", // Will be generated from stage plan
          GenerationSeed = (ulong)rng.Next() | ((ulong)rng.Next() << 32),
          StagePlan = stagePlan,
          PacingTag = stagePacing,
          RewardProfile = rewardProfile,
          MasteryRequirements = new MasteryRequirements
          {
            LettersCollected = new bool[0], // TODO: Define letter count per stage = 4
            ArtifactsCollected = new bool[0], // TODO: Define artifact count per stage = 1
            KeyPassObtained = false
          }
        };
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to assemble stage {stageId}: {ex.Message}");
        // Fall through to legacy system
      }
    }

    // Legacy fallback: use old system
    var availableStages = primaryBiome.AvailableStages;
    var unplayedStages = availableStages.Where(s => !_stageHistory.Contains(s)).ToArray();
    
    string selectedStage;
    if (unplayedStages.Length > 0)
    {
      selectedStage = unplayedStages[rng.NextInt(0, unplayedStages.Length)];
    }
    else
    {
      _stageHistory.Clear();
      selectedStage = availableStages[rng.NextInt(0, availableStages.Length)];
    }
    
    _stageHistory.Add(selectedStage);

    return new Stage
    {
      Id = stageId,
      Name = selectedStage,
      Signature = signature,
      LevelDataPath = $"Levels/{selectedStage}.json",
      GenerationSeed = (ulong)rng.Next() | ((ulong)rng.Next() << 32),
      PacingTag = stagePacing,
      RewardProfile = rewardProfile,
      MasteryRequirements = new MasteryRequirements
      {
        LettersCollected = new bool[0],
        ArtifactsCollected = new bool[0],
        KeyPassObtained = false
      }
    };
  }

  private static RewardProfile DetermineRewardProfile(string stageId, PacingTag pacing)
  {
    if (pacing == PacingTag.FAST)
      return RewardProfile.SPEED;
    
    // SLOW stages can have different profiles
    if (stageId.Contains("mastery"))
      return RewardProfile.MIXED;
    
    return RewardProfile.TREASURE;
  }


  /// <summary>
  /// Clears stage history (for testing/debugging).
  /// </summary>
  public static void ClearHistory()
  {
    _stageHistory.Clear();
  }
}

