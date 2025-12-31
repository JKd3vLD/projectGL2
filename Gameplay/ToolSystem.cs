using GL2Engine.ECS;

namespace GL2Engine.Gameplay;

/// <summary>
/// Tool system that manages tool usage and cooldowns.
/// Runs after PlayerControllerSystem, before PhysicsSystem.
/// </summary>
public class ToolSystem
{
  private GameWorld _world;

  public ToolSystem(GameWorld world)
  {
    _world = world;
  }

  public void Update(float dt)
  {
    // TODO: Query entities with Tool and ToolUsage components
    // For now, this is a stub system

    // Update tool cooldowns
    // Update active tool usage (grapple movement, balloon force, etc.)
  }
}

