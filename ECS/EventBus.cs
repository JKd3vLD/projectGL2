using System;
using Microsoft.Xna.Framework;

namespace GL2Engine.ECS;

/// <summary>
/// Typed ring buffer event bus. No delegate allocations in hot path.
/// </summary>
public class EventBus
{
  private const int RingBufferSize = 256;

  // Ring buffers for each event type
  private PlayerJumped[] _jumpedEvents = new PlayerJumped[RingBufferSize];
  private PlayerLanded[] _landedEvents = new PlayerLanded[RingBufferSize];
  private PlayerDamaged[] _damagedEvents = new PlayerDamaged[RingBufferSize];

  private int _jumpedHead = 0;
  private int _jumpedTail = 0;
  private int _landedHead = 0;
  private int _landedTail = 0;
  private int _damagedHead = 0;
  private int _damagedTail = 0;
  private FlowEvent[] _flowEvents = new FlowEvent[RingBufferSize];
  private int _flowHead = 0;
  private int _flowTail = 0;

  public void Push(PlayerJumped evt)
  {
    int next = (_jumpedHead + 1) % RingBufferSize;
    if (next == _jumpedTail) return; // Buffer full
    _jumpedEvents[_jumpedHead] = evt;
    _jumpedHead = next;
  }

  public void Push(PlayerLanded evt)
  {
    int next = (_landedHead + 1) % RingBufferSize;
    if (next == _landedTail) return;
    _landedEvents[_landedHead] = evt;
    _landedHead = next;
  }

  public void Push(PlayerDamaged evt)
  {
    int next = (_damagedHead + 1) % RingBufferSize;
    if (next == _damagedTail) return;
    _damagedEvents[_damagedHead] = evt;
    _damagedHead = next;
  }

  public void Push(FlowEvent evt)
  {
    int next = (_flowHead + 1) % RingBufferSize;
    if (next == _flowTail) return; // Buffer full
    _flowEvents[_flowHead] = evt;
    _flowHead = next;
  }

  public void Process()
  {
    // Process all events in ring buffers
    // Systems can subscribe by checking these during their update
    // For now, just clear the buffers (events processed by systems directly)
    _jumpedTail = _jumpedHead;
    _landedTail = _landedHead;
    _damagedTail = _damagedHead;
    _flowTail = _flowHead;
  }

  public bool TryPopJumped(out PlayerJumped evt)
  {
    if (_jumpedTail == _jumpedHead)
    {
      evt = default;
      return false;
    }
    evt = _jumpedEvents[_jumpedTail];
    _jumpedTail = (_jumpedTail + 1) % RingBufferSize;
    return true;
  }

  public bool TryPopLanded(out PlayerLanded evt)
  {
    if (_landedTail == _landedHead)
    {
      evt = default;
      return false;
    }
    evt = _landedEvents[_landedTail];
    _landedTail = (_landedTail + 1) % RingBufferSize;
    return true;
  }

  public bool TryPopFlow(out FlowEvent evt)
  {
    if (_flowTail == _flowHead)
    {
      evt = default;
      return false;
    }
    evt = _flowEvents[_flowTail];
    _flowTail = (_flowTail + 1) % RingBufferSize;
    return true;
  }
}

// Event structs (value types, no allocations)
public struct PlayerJumped
{
  public Entity Entity;
  public float Velocity;
}

public struct PlayerLanded
{
  public Entity Entity;
  public Vector2 Position;
}

public struct PlayerDamaged
{
  public Entity Entity;
  public int Damage;
}

// Flow Event for Flow Meter system
public struct FlowEvent
{
  public FlowEventType EventType;
  public float Delta; // Flow change amount
  public int Value; // Optional integer value (for time tiers, etc.)
  public Entity Entity; // Entity that triggered the event (usually player)
}

public enum FlowEventType
{
  TimeTierHit,      // FAST: Completed section within time tier
  Chain,            // FAST: Clean traversal chain
  SecretFound,      // SLOW: Discovered secret room/bonus door
  CarryDelivered,   // SLOW: Completed carry objective
  BonusComplete,    // SLOW: Cleared bonus room
  PuzzleClear,      // SLOW: Solved puzzle gate
  DamageTaken,      // Penalty: Took damage
  IdleTick,         // Penalty: Idle time tick
  BacktrackTick     // Penalty: FAST only, backtracking
}