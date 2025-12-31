using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GL2Engine.World;

namespace GL2Engine.Content;

/// <summary>
/// Loads handcrafted sections from base game and mod packs.
/// </summary>
public static class SectionLoader
{
  private const string BaseSectionsDirectory = "Sections";
  private const string SectionsJsonFile = "sections.json";

  /// <summary>
  /// Loads all sections from base game directory and mod packs.
  /// </summary>
  public static List<SectionDef> LoadAllSections(ModLoader modLoader, string modsDirectory = "GL2Project/Mods")
  {
    var allSections = new List<SectionDef>();

    // Load base game sections
    LoadSectionsFromDirectory(BaseSectionsDirectory, allSections);

    // Load sections from mod packs
    var loadedPacks = modLoader.GetLoadedPacks();
    foreach (var pack in loadedPacks)
    {
      // Find pack directory by name
      string packDir = Path.Combine(modsDirectory, pack.Name);
      string sectionsPath = Path.Combine(packDir, SectionsJsonFile);
      if (File.Exists(sectionsPath))
      {
        LoadSectionsFromFile(sectionsPath, allSections);
      }
    }

    // Sort by ID for deterministic ordering
    allSections.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));

    return allSections;
  }

  /// <summary>
  /// Loads sections from a directory containing individual JSON files.
  /// </summary>
  private static void LoadSectionsFromDirectory(string directory, List<SectionDef> sections)
  {
    if (!Directory.Exists(directory))
    {
      Console.WriteLine($"Sections directory not found: {directory}");
      return;
    }

    var jsonFiles = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
    
    foreach (var jsonFile in jsonFiles)
    {
      LoadSectionsFromFile(jsonFile, sections);
    }
  }

  /// <summary>
  /// Loads sections from a JSON file (can be single section or array).
  /// </summary>
  private static void LoadSectionsFromFile(string filePath, List<SectionDef> sections)
  {
    try
    {
      var json = File.ReadAllText(filePath);
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true
      };

      // Try to deserialize as array first
      var sectionArray = JsonSerializer.Deserialize<List<SectionDefJson>>(json, options);
      
      if (sectionArray != null)
      {
        foreach (var sectionJson in sectionArray)
        {
          var section = ConvertFromJson(sectionJson, filePath);
          if (!string.IsNullOrEmpty(section.Id))
          {
            sections.Add(section);
          }
        }
      }
      else
      {
        // Try as single object
        var sectionJson = JsonSerializer.Deserialize<SectionDefJson>(json, options);
        if (sectionJson != null)
        {
          var section = ConvertFromJson(sectionJson, filePath);
          if (!string.IsNullOrEmpty(section.Id))
          {
            sections.Add(section);
          }
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error loading sections from {filePath}: {ex.Message}");
    }
  }

  /// <summary>
  /// Converts JSON representation to SectionDef struct.
  /// </summary>
  private static SectionDef ConvertFromJson(SectionDefJson json, string filePath)
  {
    var section = new SectionDef
    {
      Id = json.Id ?? "",
      PacingTag = ParsePacingTag(json.PacingTag),
      BiomeTags = json.BiomeTags ?? new List<string>(),
      TierMin = json.TierMin ?? 1,
      TierMax = json.TierMax ?? 5,
      DifficultyStars = json.DifficultyStars ?? 1,
      LengthClass = ParseLengthClass(json.LengthClass),
      TraversalMode = ParseTraversalMode(json.TraversalMode),
      InteractionTags = ParseInteractionTags(json.InteractionTags ?? new List<string>()),
      ConnectorsIn = json.ConnectorsIn ?? new List<string>(),
      ConnectorsOut = json.ConnectorsOut ?? new List<string>(),
      Quotas = json.Quotas?.ToSectionQuotas() ?? new SectionQuotas(),
      LevelDataPath = json.LevelDataPath ?? ""
    };

    // If LevelDataPath is relative, make it relative to the sections file directory
    if (!string.IsNullOrEmpty(section.LevelDataPath) && !Path.IsPathRooted(section.LevelDataPath))
    {
      string baseDir = Path.GetDirectoryName(filePath) ?? "";
      section.LevelDataPath = Path.Combine(baseDir, section.LevelDataPath);
    }

    return section;
  }

  private static PacingTag ParsePacingTag(string? tag)
  {
    if (string.IsNullOrEmpty(tag))
      return PacingTag.FAST;

    return tag.ToUpperInvariant() switch
    {
      "FAST" => PacingTag.FAST,
      "SLOW" => PacingTag.SLOW,
      _ => PacingTag.FAST
    };
  }

  private static LengthClass ParseLengthClass(string? lengthClass)
  {
    if (string.IsNullOrEmpty(lengthClass))
      return LengthClass.MED;

    return lengthClass.ToUpperInvariant() switch
    {
      "SHORT" => LengthClass.SHORT,
      "MED" => LengthClass.MED,
      "LONG" => LengthClass.LONG,
      _ => LengthClass.MED
    };
  }

  private static TraversalMode ParseTraversalMode(string? mode)
  {
    if (string.IsNullOrEmpty(mode))
      return TraversalMode.RUNLINE;

    return mode.ToUpperInvariant() switch
    {
      "RUNLINE" => TraversalMode.RUNLINE,
      "VERTICAL_ASCENT" => TraversalMode.VERTICAL_ASCENT,
      "VERTICAL_DESCENT" => TraversalMode.VERTICAL_DESCENT,
      "AUTOSCROLL" => TraversalMode.AUTOSCROLL,
      "VEHICLE" => TraversalMode.VEHICLE,
      "CANNON_CHAIN" => TraversalMode.CANNON_CHAIN,
      "OPEN_EXPLORATION" => TraversalMode.OPEN_EXPLORATION,
      _ => TraversalMode.RUNLINE
    };
  }

  private static InteractionTags ParseInteractionTags(List<string> tags)
  {
    InteractionTags result = InteractionTags.None;

    foreach (var tag in tags)
    {
      var tagUpper = tag.ToUpperInvariant();
      result |= tagUpper switch
      {
        "BARRELCANNON" => InteractionTags.BarrelCannon,
        "TEAMUPREQUIRED" => InteractionTags.TeamUpRequired,
        "CARRYPROP" => InteractionTags.CarryProp,
        "ROPES" => InteractionTags.Ropes,
        "BOOSTPOLE" => InteractionTags.BoostPole,
        "WATER" => InteractionTags.Water,
        "RISINGHAZARD" => InteractionTags.RisingHazard,
        "PUZZLEGATE" => InteractionTags.PuzzleGate,
        "MINIGAMEPORTAL" => InteractionTags.MinigamePortal,
        "BONUSDOOR" => InteractionTags.BonusDoor,
        "ENEMIES" => InteractionTags.Enemies,
        "OBSTACLES" => InteractionTags.Obstacles,
        _ => InteractionTags.None
      };
    }

    return result;
  }

  /// <summary>
  /// JSON representation of SectionDef for deserialization.
  /// </summary>
  private class SectionDefJson
  {
    public string? Id { get; set; }
    public string? PacingTag { get; set; }
    public List<string>? BiomeTags { get; set; }
    public int? TierMin { get; set; }
    public int? TierMax { get; set; }
    public int? DifficultyStars { get; set; }
    public string? LengthClass { get; set; }
    public string? TraversalMode { get; set; }
    public List<string>? InteractionTags { get; set; }
    public List<string>? ConnectorsIn { get; set; }
    public List<string>? ConnectorsOut { get; set; }
    public SectionQuotasJson? Quotas { get; set; }
    public string? LevelDataPath { get; set; }
  }

  private class SectionQuotasJson
  {
    public int SecretSlots { get; set; }
    public int BonusDoorSlots { get; set; }
    public int ChestSlots { get; set; }

    public SectionQuotas ToSectionQuotas()
    {
      return new SectionQuotas
      {
        SecretSlots = SecretSlots,
        BonusDoorSlots = BonusDoorSlots,
        ChestSlots = ChestSlots
      };
    }
  }
}

