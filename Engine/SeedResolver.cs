using System;
using System.Security.Cryptography;
using System.Text;

namespace GL2Engine.Engine;

/// <summary>
/// Tier-scoped seed resolver. Resolves deterministic seeds from tier index, category codes, and symbol IDs.
/// </summary>
public static class SeedResolver
{
  private const uint HashVersion = 1; // Increment when seed resolution algorithm changes

  /// <summary>
  /// Resolves a deterministic seed from tier-scoped parameters.
  /// Same tier + codes + symbols = same seed. Different tiers with same codes = different seeds.
  /// </summary>
  public static ulong ResolveSeed(uint hashVersion, int tierIndex, int categoryId, int[] symbolIds)
  {
    // Combine all inputs into a hash input
    var hashInput = new StringBuilder();
    hashInput.Append($"v{hashVersion}_t{tierIndex}_c{categoryId}_");
    
    // Sort symbol IDs for consistent ordering
    var sortedSymbols = new int[symbolIds.Length];
    Array.Copy(symbolIds, sortedSymbols, symbolIds.Length);
    Array.Sort(sortedSymbols);
    
    foreach (var symbolId in sortedSymbols)
    {
      hashInput.Append($"s{symbolId}_");
    }

    // Hash the input string to get deterministic seed
    var inputBytes = Encoding.UTF8.GetBytes(hashInput.ToString());
    var hashBytes = SHA256.HashData(inputBytes);
    
    // Convert first 8 bytes to ulong
    ulong seed = 0;
    for (int i = 0; i < 8; i++)
    {
      seed |= ((ulong)hashBytes[i]) << (i * 8);
    }

    return seed;
  }

  /// <summary>
  /// Convenience method using current hash version.
  /// </summary>
  public static ulong ResolveSeed(int tierIndex, int categoryId, int[] symbolIds)
  {
    return ResolveSeed(HashVersion, tierIndex, categoryId, symbolIds);
  }
}

