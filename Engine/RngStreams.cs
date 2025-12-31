namespace GL2Engine.Engine;

/// <summary>
/// Separate RNG streams for different game systems. Each stream is deterministic and tier-scoped.
/// </summary>
public class RngStreams
{
  private Rng _worldGen;
  private Rng _reward;
  private Rng _bonus;
  private Rng _collectible;

  public RngStreams(ulong baseSeed)
  {
    // Each stream uses base seed + stream-specific offset to ensure independence
    _worldGen = new Rng((uint)(baseSeed + 0x100000000UL));
    _reward = new Rng((uint)(baseSeed + 0x200000000UL));
    _bonus = new Rng((uint)(baseSeed + 0x300000000UL));
    _collectible = new Rng((uint)(baseSeed + 0x400000000UL));
  }

  public Rng WorldGen => _worldGen;
  public Rng Reward => _reward;
  public Rng Bonus => _bonus;
  public Rng Collectible => _collectible;

  /// <summary>
  /// Resets all streams to their initial state (for testing/debugging).
  /// </summary>
  public void Reset(ulong baseSeed)
  {
    _worldGen = new Rng((uint)(baseSeed + 0x100000000UL));
    _reward = new Rng((uint)(baseSeed + 0x200000000UL));
    _bonus = new Rng((uint)(baseSeed + 0x300000000UL));
    _collectible = new Rng((uint)(baseSeed + 0x400000000UL));
  }
}

