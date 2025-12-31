using System;
using System.Collections.Generic;
using GL2Engine.Engine;

namespace GL2Engine.World;

/// <summary>
/// Tier package containing 7 stages generated from 3 biomes.
/// </summary>
public class TierPackage
{
  public int TierIndex { get; set; }
  public Biome[] Biomes { get; set; } = new Biome[3];
  public Stage[] Stages { get; set; } = new Stage[7];
  public ulong WorldGenSeed { get; set; }

  /// <summary>
  /// Stage indices:
  /// 0: Pure A
  /// 1: Pure B
  /// 2: Pure C
  /// 3: Mixed AB
  /// 4: Mixed BC
  /// 5: Mixed CA
  /// 6: Mastery ABC
  /// </summary>
  public Stage GetStage(int index)
  {
    if (index < 0 || index >= Stages.Length)
      throw new ArgumentOutOfRangeException(nameof(index));
    return Stages[index];
  }

  public BiomeSignature GetStageSignature(int stageIndex)
  {
    return stageIndex switch
    {
      0 => new BiomeSignature { HasA = true, HasB = false, HasC = false },
      1 => new BiomeSignature { HasA = false, HasB = true, HasC = false },
      2 => new BiomeSignature { HasA = false, HasB = false, HasC = true },
      3 => new BiomeSignature { HasA = true, HasB = true, HasC = false },
      4 => new BiomeSignature { HasA = false, HasB = true, HasC = true },
      5 => new BiomeSignature { HasA = true, HasB = false, HasC = true },
      6 => new BiomeSignature { HasA = true, HasB = true, HasC = true },
      _ => throw new ArgumentOutOfRangeException(nameof(stageIndex))
    };
  }
}

/// <summary>
/// Biome signature indicating which biomes are present in a stage.
/// </summary>
public struct BiomeSignature
{
  public bool HasA;
  public bool HasB;
  public bool HasC;

  public override string ToString()
  {
    var parts = new List<string>();
    if (HasA) parts.Add("A");
    if (HasB) parts.Add("B");
    if (HasC) parts.Add("C");
    return string.Join("", parts);
  }
}

