namespace GL2Engine.World;

/// <summary>
/// Biome definition for world generation.
/// </summary>
public class Biome
{
  public string Id { get; set; } = "";
  public string Name { get; set; } = "";
  public string Description { get; set; } = "";
  
  // Visual/atmospheric properties
  public string ThemeColor { get; set; } = "#FFFFFF";
  public string BackgroundLayer { get; set; } = "";
  
  // Generation properties
  public float DifficultyMultiplier { get; set; } = 1.0f;
  public string[] AvailableStages { get; set; } = Array.Empty<string>();
}

