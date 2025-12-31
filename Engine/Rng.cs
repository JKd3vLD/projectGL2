namespace GL2Engine.Engine;

/// <summary>
/// Deterministic RNG streams with tier-scoped seed resolver.
/// </summary>
public class Rng
{
  private uint _state1;
  private uint _state2;
  private uint _state3;

  public Rng(uint seed)
  {
    _state1 = seed;
    _state2 = seed * 1103515245u + 12345u;
    _state3 = seed * 1664525u + 1013904223u;
  }

  public uint Next()
  {
    // Simple LCG-based RNG (deterministic)
    _state1 = _state1 * 1103515245u + 12345u;
    _state2 = _state2 * 1664525u + 1013904223u;
    _state3 = _state3 * 214013u + 2531011u;
    
    return _state1 ^ _state2 ^ _state3;
  }

  public float NextFloat()
  {
    return (Next() & 0x7FFFFFFF) / 2147483648.0f;
  }

  public int NextInt(int min, int max)
  {
    return min + (int)(Next() % (uint)(max - min));
  }
}
