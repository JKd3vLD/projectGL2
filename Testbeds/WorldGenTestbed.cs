using System;
using GL2Engine.Engine;
using GL2Engine.World;

namespace GL2Engine.Testbeds;

/// <summary>
/// Testbed for TierPackage generation. Generates a package and shows 7 nodes with biome signatures.
/// </summary>
public class WorldGenTestbed
{
  private TierPackage? _currentPackage;
  private RngStreams? _rngStreams;

  public void Initialize(int tierIndex, ulong baseSeed)
  {
    _rngStreams = new RngStreams(baseSeed);
    _currentPackage = WorldGenerator.GenerateTierPackage(tierIndex, _rngStreams.WorldGen);
  }

  public TierPackage? GetPackage() => _currentPackage;

  public void LoadStage(int stageIndex)
  {
    if (_currentPackage == null)
      throw new InvalidOperationException("Testbed not initialized");

    if (stageIndex < 0 || stageIndex >= _currentPackage.Stages.Length)
      throw new ArgumentOutOfRangeException(nameof(stageIndex));

    var stage = _currentPackage.Stages[stageIndex];
    var signature = _currentPackage.GetStageSignature(stageIndex);
    
    // TODO: Actually load the stage level data
    // For now, just log the stage info
    Console.WriteLine($"Loading stage {stageIndex}: {stage.Name} (Signature: {signature})");
    Console.WriteLine($"  Level path: {stage.LevelDataPath}");
    Console.WriteLine($"  Generation seed: {stage.GenerationSeed}");
  }

  public void PrintPackageInfo()
  {
    if (_currentPackage == null)
    {
      Console.WriteLine("No package generated");
      return;
    }

    Console.WriteLine($"Tier {_currentPackage.TierIndex} Package:");
    Console.WriteLine($"  WorldGen Seed: {_currentPackage.WorldGenSeed}");
    Console.WriteLine($"  Biomes: {_currentPackage.Biomes[0].Name}, {_currentPackage.Biomes[1].Name}, {_currentPackage.Biomes[2].Name}");
    Console.WriteLine($"  Stages:");
    for (int i = 0; i < _currentPackage.Stages.Length; i++)
    {
      var stage = _currentPackage.Stages[i];
      var signature = _currentPackage.GetStageSignature(i);
      Console.WriteLine($"    {i}: {stage.Name} [{signature}]");
    }
  }
}

