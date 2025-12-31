using Microsoft.Xna.Framework;
using GL2Engine.ECS;

namespace GL2Engine.Gameplay;

/// <summary>
/// Balloon/spring tool - jump extension tool, consumable/charge-based.
/// Does NOT modify core movement constants.
/// </summary>
public static class BalloonTool
{
  private const float JumpBoostForce = 300f;
  private const float BalloonCooldown = 1.0f;
  private const float BalloonDuration = 0.3f; // How long boost is applied

  /// <summary>
  /// Attempts to use balloon tool. Returns true if balloon was activated.
  /// </summary>
  public static bool TryUse(GameWorld world, Entity playerEntity, ref Tool tool, ref ToolUsage usage)
  {
    // Check charges
    if (tool.Charges <= 0 && tool.MaxCharges > 0)
      return false;

    // Check cooldown
    if (tool.CooldownTimer > 0)
      return false;

    // Check zone if zone-scoped
    if (tool.IsZoneScoped && tool.ZoneId != null)
    {
      // TODO: Check if player is in allowed zone
      // For now, allow usage
    }

    // Check if player is airborne (balloon only works in air)
    if (!world.Velocities.Has(playerEntity))
      return false;

    var groundState = world.GroundStates.Has(playerEntity)
      ? world.GroundStates.Get(playerEntity)
      : new GroundState { IsGrounded = true };

    if (groundState.IsGrounded)
      return false; // Balloon only works when airborne

    // Activate balloon
    usage.ActiveTool = ToolType.Balloon;
    usage.IsActive = true;
    usage.UsageTimer = 0f;

    // Apply jump boost (does NOT modify base movement constants)
    ref var vel = ref world.Velocities.Get(playerEntity);
    vel.Value = new Vector2(vel.Value.X, vel.Value.Y - JumpBoostForce);

    // Consume charge if limited
    if (tool.MaxCharges > 0)
    {
      tool.Charges--;
    }

    // Set cooldown
    tool.CooldownTimer = BalloonCooldown;

    return true;
  }

  /// <summary>
  /// Updates balloon tool usage.
  /// </summary>
  public static void Update(GameWorld world, Entity playerEntity, float dt, ref ToolUsage usage)
  {
    if (!usage.IsActive || usage.ActiveTool != ToolType.Balloon)
      return;

    usage.UsageTimer += dt;

    // Apply continuous upward force during duration
    if (usage.UsageTimer < BalloonDuration && world.Velocities.Has(playerEntity))
    {
      ref var vel = ref world.Velocities.Get(playerEntity);
      // Apply additional upward force (does NOT modify base movement constants)
      vel.Value = new Vector2(vel.Value.X, vel.Value.Y - JumpBoostForce * 0.5f * dt);
    }
    else
    {
      // Duration expired, stop balloon
      usage.IsActive = false;
    }
  }
}

