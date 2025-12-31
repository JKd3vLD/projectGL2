using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GL2Engine.ECS;
using System.Text.Json;

namespace GL2Engine.Gameplay;

/// <summary>
/// Movement system - handles player input and movement state.
/// Legacy system, PlayerControllerSystem is the main one.
/// </summary>
public class MovementSystem
{
  private GameWorld _world;
  private MovementTuning _tuning; // Not readonly - modified during LoadTuning

  public MovementSystem(GameWorld world)
  {
    _world = world;
    LoadTuning();
  }

  private void LoadTuning()
  {
    try
    {
      var json = System.IO.File.ReadAllText("GL2Project/Tuning/MovementTuning.json");
      var jsonDoc = JsonDocument.Parse(json);
      var root = jsonDoc.RootElement;
      
      var groundTuning = new GroundTuning();
      var jumpTuning = new JumpTuning();
      
      if (root.TryGetProperty("ground", out var ground))
      {
        if (ground.TryGetProperty("walkSpeed", out var walkSpeed))
          groundTuning = groundTuning with { WalkSpeed = walkSpeed.GetProperty("value").GetSingle() };
        if (ground.TryGetProperty("acceleration", out var accel))
          groundTuning = groundTuning with { Acceleration = accel.GetProperty("value").GetSingle() };
        if (ground.TryGetProperty("friction", out var friction))
          groundTuning = groundTuning with { Friction = friction.GetProperty("value").GetSingle() };
        if (ground.TryGetProperty("jump", out var jump))
        {
          if (jump.TryGetProperty("initialVelocity", out var initVel))
            jumpTuning = jumpTuning with { InitialVelocity = initVel.GetProperty("value").GetSingle() };
          if (jump.TryGetProperty("coyoteTime", out var coyote))
            jumpTuning = jumpTuning with { CoyoteTime = coyote.GetProperty("value").GetSingle() };
          if (jump.TryGetProperty("jumpBuffer", out var buffer))
            jumpTuning = jumpTuning with { JumpBuffer = buffer.GetProperty("value").GetSingle() };
          groundTuning = groundTuning with { Jump = jumpTuning };
        }
      }
      
      float gravityValue = root.TryGetProperty("gravity", out var gravity) ? gravity.GetProperty("value").GetSingle() : 0f;
      float terminalVelocityValue = root.TryGetProperty("terminalVelocity", out var termVel) ? termVel.GetProperty("value").GetSingle() : 0f;
      
      // Build tuning struct using with expressions
      _tuning = new MovementTuning();
      _tuning = _tuning with { Ground = groundTuning };
      _tuning = _tuning with { Gravity = gravityValue };
      _tuning = _tuning with { TerminalVelocity = terminalVelocityValue };
    }
    catch
    {
      _tuning = new MovementTuning();
    }
  }

  public void Update(float dt)
  {
    // This system is kept for compatibility but PlayerControllerSystem is the main one
  }
}
