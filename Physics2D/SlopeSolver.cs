using System;
using Microsoft.Xna.Framework;
using GL2Engine.ECS;
using GL2Engine.Content;

namespace GL2Engine.Physics2D;

/// <summary>
/// Slope collision solver with line segment intersection.
/// </summary>
public static class SlopeSolver
{
  /// <summary>
  /// Represents a slope segment in world space.
  /// </summary>
  public struct SlopeSegment
  {
    public Vector2 Start;
    public Vector2 End;
    public Vector2 Normal;
    public float AngleDegrees;
    public bool SlideEligible;
  }

  /// <summary>
  /// Check if a point (with radius) intersects a slope segment.
  /// </summary>
  public static bool IntersectsSlope(Vector2 point, float radius, SlopeSegment slope, out Vector2 contactPoint, out Vector2 normal)
  {
    contactPoint = Vector2.Zero;
    normal = slope.Normal;

    // Project point onto slope line
    Vector2 slopeDir = slope.End - slope.Start;
    float slopeLen = slopeDir.Length();
    if (slopeLen < 0.001f)
      return false;

    Vector2 slopeNormalized = slopeDir / slopeLen;
    Vector2 toPoint = point - slope.Start;
    float projection = Vector2.Dot(toPoint, slopeNormalized);

    // Clamp to segment bounds
    projection = MathF.Max(0, MathF.Min(slopeLen, projection));
    Vector2 closestPoint = slope.Start + slopeNormalized * projection;

    // Check distance from point to closest point on segment
    float distSq = Vector2.DistanceSquared(point, closestPoint);
    if (distSq > radius * radius)
      return false;

    contactPoint = closestPoint;
    return true;
  }

  /// <summary>
  /// Resolve collision with slope - project player onto slope surface.
  /// </summary>
  public static void ResolveCollision(ref Position position, ref Velocity velocity, ref GroundState groundState, SlopeSegment slope, float playerRadius)
  {
    if (IntersectsSlope(position.Value, playerRadius, slope, out Vector2 contactPoint, out Vector2 normal))
    {
      // Project player onto slope surface
      float distToSlope = Vector2.Distance(position.Value, contactPoint);
      if (distToSlope < playerRadius)
      {
        // Move player to sit on slope
        position.Value = contactPoint + normal * playerRadius;
        
        // Cancel velocity perpendicular to slope
        float perpVel = Vector2.Dot(velocity.Value, normal);
        velocity.Value -= normal * perpVel;

        // Update ground state
        groundState.IsGrounded = true;
        groundState.GroundNormal = normal;
        groundState.GroundAngle = slope.AngleDegrees;
        groundState.OnSlope = true;
        groundState.CanSlide = slope.SlideEligible;
      }
    }
  }

  /// <summary>
  /// Create slope segment from block definition and world position.
  /// </summary>
  public static SlopeSegment CreateSlopeSegment(BlockDefinition blockDef, Vector2 worldPos)
  {
    if (blockDef.Slope == null)
      throw new ArgumentException("Block definition must have slope data");

    var slope = blockDef.Slope.Value;
    return new SlopeSegment
    {
      Start = worldPos + slope.StartPoint,
      End = worldPos + slope.EndPoint,
      Normal = ComputeSlopeNormal(slope.StartPoint, slope.EndPoint),
      AngleDegrees = slope.AngleDegrees,
      SlideEligible = slope.SlideEligible
    };
  }

  private static Vector2 ComputeSlopeNormal(Vector2 start, Vector2 end)
  {
    Vector2 dir = end - start;
    Vector2 normal = new Vector2(-dir.Y, dir.X);
    normal.Normalize();
    // Ensure normal points up
    if (normal.Y > 0)
      normal = -normal;
    return normal;
  }
}

