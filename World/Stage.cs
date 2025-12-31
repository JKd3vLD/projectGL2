namespace GL2Engine.World;

/// <summary>
/// Stage structure representing a playable level.
/// </summary>
public class Stage
{
  public string Id { get; set; } = "";
  public string Name { get; set; } = "";
  public BiomeSignature Signature { get; set; }
  public string LevelDataPath { get; set; } = "";
  
  // Mastery unlock requirements
  public MasteryRequirements MasteryRequirements { get; set; }
  
  // Generation seed (for procedural elements)
  public ulong GenerationSeed { get; set; }
  
  // Stage plan (assembled from sections)
  public StagePlan? StagePlan { get; set; }
  
  // Pacing tag (FAST/SLOW)
  public PacingTag PacingTag { get; set; }
  
  // Reward profile
  public RewardProfile RewardProfile { get; set; }
}

/// <summary>
/// Mastery unlock checklist (stub for now).
/// </summary>
public struct MasteryRequirements
{
  public bool[] LettersCollected { get; set; } // TODO: Define letter system
  public bool[] ArtifactsCollected { get; set; } // TODO: Define artifact system
  public bool KeyPassObtained { get; set; } // TODO: Define key-pass system
  
  public bool IsComplete()
  {
    // TODO: Implement actual completion check
    return false;
  }
}

