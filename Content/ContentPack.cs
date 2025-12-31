using System.Collections.Generic;
using GL2Engine.Inventory;
using GL2Engine.World;

namespace GL2Engine.Content;

/// <summary>
/// Content pack structure containing items, blocks, biomes, and rooms.
/// </summary>
public class ContentPack
{
  public string Name { get; set; } = "";
  public string Version { get; set; } = "1.0.0";
  public List<ItemDef> Items { get; set; } = new List<ItemDef>();
  public List<BlockDefinition> Blocks { get; set; } = new List<BlockDefinition>();
  public List<Biome> Biomes { get; set; } = new List<Biome>();
  public List<RoomDef> Rooms { get; set; } = new List<RoomDef>();
}

/// <summary>
/// Merged content database from all loaded packs.
/// </summary>
public class ContentDatabase
{
  public Dictionary<string, ItemDef> Items { get; set; } = new Dictionary<string, ItemDef>();
  public Dictionary<string, BlockDefinition> Blocks { get; set; } = new Dictionary<string, BlockDefinition>();
  public Dictionary<string, Biome> Biomes { get; set; } = new Dictionary<string, Biome>();
  public Dictionary<string, RoomDef> Rooms { get; set; } = new Dictionary<string, RoomDef>();
}

/// <summary>
/// Room definition for level generation.
/// </summary>
public class RoomDef
{
  public string Id { get; set; } = "";
  public string Name { get; set; } = "";
  public string LevelDataPath { get; set; } = "";
  public Microsoft.Xna.Framework.Rectangle Bounds { get; set; }
}

