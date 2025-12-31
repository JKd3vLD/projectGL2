using Microsoft.Xna.Framework;
using GL2Engine.ECS;

namespace GL2Engine.Gameplay;

/// <summary>
/// Grapple hook tool - Terraria-inspired, contextual prop with charges/zone constraints.
/// Does NOT modify core movement constants.
/// </summary>
public static class GrappleTool
{
  private const float GrappleRange = 200f;
  private const float GrappleSpeed = 400f;
  private const float GrappleCooldown = 0.5f;

  /// <summary>
  /// Attempts to use grapple tool. Returns true if grapple was activated.
  /// </summary>
  public static bool TryUse(GameWorld world, Entity playerEntity, Vector2 direction, ref Tool tool, ref ToolUsage usage)
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

    // Find grapple target in direction
    if (!world.Positions.Has(playerEntity))
      return false;

    var playerPos = world.Positions.Get(playerEntity).Value;
    var targetPos = FindGrappleTarget(world, playerPos, direction);

    if (targetPos.HasValue)
    {
      // Activate grapple
      usage.ActiveTool = ToolType.Grapple;
      usage.TargetPosition = targetPos.Value;
      usage.IsActive = true;
      usage.UsageTimer = 0f;

      // Consume charge if limited
      if (tool.MaxCharges > 0)
      {
        tool.Charges--;
      }

      // Set cooldown
      tool.CooldownTimer = GrappleCooldown;

      return true;
    }

    return false;
  }

  /// <summary>
  /// Updates grapple tool usage.
  /// </summary>
  public static void Update(GameWorld world, Entity playerEntity, float dt, ref ToolUsage usage)
  {
    if (!usage.IsActive || usage.ActiveTool != ToolType.Grapple)
      return;

    if (!world.Positions.Has(playerEntity) || !world.Velocities.Has(playerEntity))
      return;

    var playerPos = world.Positions.Get(playerEntity).Value;
    var playerVel = world.Velocities.Get(playerEntity).Value;

    // Move player toward target
    Vector2 toTarget = usage.TargetPosition - playerPos;
    float distance = toTarget.Length();

    if (distance < 10f)
    {
      // Reached target, stop grapple
      usage.IsActive = false;
      return;
    }

    // Apply grapple velocity
    Vector2 grappleDir = Vector2.Normalize(toTarget);
    Vector2 grappleVel = grappleDir * GrappleSpeed;

    // Blend with existing velocity
    playerVel = Vector2.Lerp(playerVel, grappleVel, 0.3f);
    
    // Update velocity component (player should always have velocity)
    if (world.Velocities.Has(playerEntity))
    {
      ref var vel = ref world.Velocities.Get(playerEntity);
      vel.Value = playerVel;
    }
    else
    {
      world.Velocities.Add(playerEntity, new Velocity { Value = playerVel });
    }

    usage.UsageTimer += dt;

    // Timeout after 2 seconds
    if (usage.UsageTimer > 2f)
    {
      usage.IsActive = false;
    }
  }

  private static Vector2? FindGrappleTarget(GameWorld world, Vector2 fromPos, Vector2 direction)
  {
    // Simple raycast to find grappleable surface
    // TODO: Implement proper collision raycast
    // For now, return a point in the direction
    
    direction.Normalize();
    Vector2 target = fromPos + direction * GrappleRange;

    // Check if target is valid (not in solid)
    // TODO: Check collision world
    return target;
  }
}

