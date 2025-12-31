using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GL2Engine.Inventory;
using GL2Engine.World;
using GL2Engine.Content;

namespace GL2Engine.Content;

/// <summary>
/// Content pack loader. Scans /Mods/ directory and loads packs in alphabetical order.
/// </summary>
public class ModLoader
{
  private List<ContentPack> _loadedPacks = new List<ContentPack>();

  /// <summary>
  /// Loads all content packs from the Mods directory.
  /// </summary>
  public void LoadAllPacks(string modsDirectory = "GL2Project/Mods")
  {
    _loadedPacks.Clear();

    if (!Directory.Exists(modsDirectory))
    {
      Console.WriteLine($"Mods directory not found: {modsDirectory}");
      return;
    }

    // Get all pack directories, sorted alphabetically for deterministic loading
    var packDirs = Directory.GetDirectories(modsDirectory)
      .OrderBy(d => Path.GetFileName(d))
      .ToArray();

    foreach (var packDir in packDirs)
    {
      try
      {
        var pack = LoadPack(packDir);
        if (pack != null)
        {
          _loadedPacks.Add(pack);
          Console.WriteLine($"Loaded pack: {pack.Name}");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to load pack from {packDir}: {ex.Message}");
        // Continue loading other packs
      }
    }
  }

  /// <summary>
  /// Loads a single content pack from a directory.
  /// </summary>
  private ContentPack? LoadPack(string packDirectory)
  {
    var packName = Path.GetFileName(packDirectory);
    var pack = new ContentPack { Name = packName };

    // Load items.json
    var itemsPath = Path.Combine(packDirectory, "items.json");
    if (File.Exists(itemsPath))
    {
      pack.Items = LoadItems(itemsPath);
    }

    // Load blocks.json
    var blocksPath = Path.Combine(packDirectory, "blocks.json");
    if (File.Exists(blocksPath))
    {
      pack.Blocks = LoadBlocks(blocksPath);
    }

    // Load biomes.json
    var biomesPath = Path.Combine(packDirectory, "biomes.json");
    if (File.Exists(biomesPath))
    {
      pack.Biomes = LoadBiomes(biomesPath);
    }

    // Load rooms.json
    var roomsPath = Path.Combine(packDirectory, "rooms.json");
    if (File.Exists(roomsPath))
    {
      pack.Rooms = LoadRooms(roomsPath);
    }

    return pack;
  }

  private List<ItemDef> LoadItems(string path)
  {
    try
    {
      var json = File.ReadAllText(path);
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true
      };
      var items = JsonSerializer.Deserialize<List<ItemDefJson>>(json, options);
      return items?.Select(i => i.ToItemDef()).ToList() ?? new List<ItemDef>();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Failed to load items from {path}: {ex.Message}");
      return new List<ItemDef>();
    }
  }

  private List<BlockDefinition> LoadBlocks(string path)
  {
    // TODO: Implement block loading
    return new List<BlockDefinition>();
  }

  private List<Biome> LoadBiomes(string path)
  {
    // TODO: Implement biome loading
    return new List<Biome>();
  }

  private List<RoomDef> LoadRooms(string path)
  {
    // TODO: Implement room loading
    return new List<RoomDef>();
  }

  /// <summary>
  /// Gets all loaded packs.
  /// </summary>
  public List<ContentPack> GetLoadedPacks()
  {
    return new List<ContentPack>(_loadedPacks);
  }

  /// <summary>
  /// Merges all loaded packs into a single content database.
  /// Later packs override earlier ones (alphabetical order).
  /// </summary>
  public ContentDatabase MergePacks()
  {
    var db = new ContentDatabase();

    foreach (var pack in _loadedPacks)
    {
      // Merge items (later packs override)
      foreach (var item in pack.Items)
      {
        db.Items[item.Id] = item;
      }

      // Merge blocks
      foreach (var block in pack.Blocks)
      {
        db.Blocks[block.Id] = block;
      }

      // Merge biomes
      foreach (var biome in pack.Biomes)
      {
        db.Biomes[biome.Id] = biome;
      }

      // Merge rooms
      foreach (var room in pack.Rooms)
      {
        db.Rooms[room.Id] = room;
      }
    }

    return db;
  }
}

/// <summary>
/// JSON representation of ItemDef for serialization.
/// </summary>
public struct ItemDefJson
{
  public string Id { get; set; }
  public string Type { get; set; }
  public int MaxStack { get; set; }
  public string[]? Tags { get; set; }
  public string Rarity { get; set; }
  public string? UseAction { get; set; }
  public bool IsRunScoped { get; set; }

  public ItemDef ToItemDef()
  {
    var category = Enum.Parse<ItemCategory>(Type, true);
    var rarity = Enum.Parse<ItemRarity>(Rarity, true);

    ulong tagsBitmask = 0;
    if (Tags != null)
    {
      foreach (var tagStr in Tags)
      {
        if (Enum.TryParse<ItemTag>(tagStr, true, out var tag))
        {
          tagsBitmask = ItemTagHelper.AddTag(tagsBitmask, tag);
        }
      }
    }

    return new ItemDef(Id, category, MaxStack)
    {
      TagsBitmask = tagsBitmask,
      Rarity = rarity,
      UseAction = UseAction,
      IsRunScoped = IsRunScoped
    };
  }
}

