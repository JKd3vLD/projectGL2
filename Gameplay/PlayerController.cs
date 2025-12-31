using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GL2Engine.ECS;
using System.Text.Json;
using static GL2Engine.Gameplay.ModifierSystem; // For InputAction enum

namespace GL2Engine.Gameplay;

/// <summary>
/// Complete movement controller with all DKC2 features.
/// </summary>
public class PlayerControllerSystem
{
  private GameWorld _world;
  private MovementTuning _tuning;
  private bool _wasCartwheelPressed = false;

  public PlayerControllerSystem(GameWorld world)
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
      
      // Initialize tuning structs
      var groundTuning = new GroundTuning();
      var jumpTuning = new JumpTuning();
      float gravityValue = 0f;
      float terminalVelocityValue = 0f;
      var glideTuning = new GlideTuning();
      var cartwheelTuning = new CartwheelTuning();
      
      if (root.TryGetProperty("ground", out var ground))
      {
        if (ground.TryGetProperty("walkSpeed", out var walkSpeed))
          groundTuning = groundTuning with { WalkSpeed = walkSpeed.GetProperty("value").GetSingle() };
        if (ground.TryGetProperty("runSpeed", out var runSpeed))
          groundTuning = groundTuning with { RunSpeed = runSpeed.GetProperty("value").GetSingle() };
        if (ground.TryGetProperty("acceleration", out var accel))
          groundTuning = groundTuning with { Acceleration = accel.GetProperty("value").GetSingle() };
        if (ground.TryGetProperty("friction", out var friction))
          groundTuning = groundTuning with { Friction = friction.GetProperty("value").GetSingle() };
        if (ground.TryGetProperty("crouchSpeed", out var crouchSpeed))
          groundTuning = groundTuning with { CrouchSpeed = crouchSpeed.GetProperty("value").GetSingle() };
        if (ground.TryGetProperty("jump", out var jump))
        {
          if (jump.TryGetProperty("initialVelocity", out var initVel))
            jumpTuning = jumpTuning with { InitialVelocity = initVel.GetProperty("value").GetSingle() };
          if (jump.TryGetProperty("variableJumpCutVelocity", out var cutVel))
            jumpTuning = jumpTuning with { VariableJumpCutVelocity = cutVel.GetProperty("value").GetSingle() };
          if (jump.TryGetProperty("coyoteTime", out var coyote))
            jumpTuning = jumpTuning with { CoyoteTime = coyote.GetProperty("value").GetSingle() };
          if (jump.TryGetProperty("jumpBuffer", out var buffer))
            jumpTuning = jumpTuning with { JumpBuffer = buffer.GetProperty("value").GetSingle() };
          groundTuning = groundTuning with { Jump = jumpTuning };
        }
      }
      
      if (root.TryGetProperty("gravity", out var gravity))
        gravityValue = gravity.GetProperty("value").GetSingle();
      if (root.TryGetProperty("terminalVelocity", out var termVel))
        terminalVelocityValue = termVel.GetProperty("value").GetSingle();
      if (root.TryGetProperty("glide", out var glide))
      {
        if (glide.TryGetProperty("clampVelocity", out var clampVel))
          glideTuning = glideTuning with { ClampVelocity = clampVel.GetProperty("value").GetSingle() };
        if (glide.TryGetProperty("retriggerable", out var retrigger))
          glideTuning = glideTuning with { Retriggerable = retrigger.GetBoolean() };
      }
      if (root.TryGetProperty("cartwheel", out var cartwheel))
      {
        if (cartwheel.TryGetProperty("groundSpeed", out var cartSpeed))
          cartwheelTuning = cartwheelTuning with { GroundSpeed = cartSpeed.GetProperty("value").GetSingle() };
      }
      
      // Build final tuning struct using object initializer (structs are no longer readonly)
      _tuning = new MovementTuning
      {
        Ground = groundTuning,
        Gravity = gravityValue,
        TerminalVelocity = terminalVelocityValue,
        Glide = glideTuning,
        Cartwheel = cartwheelTuning
      };
    }
    catch
    {
      _tuning = new MovementTuning();
    }
  }

  public void Update(float dt)
  {
    var keyboard = Keyboard.GetState();

    for (int i = 0; i < _world.PlayerControllers.Count; i++)
    {
      var entity = _world.PlayerControllers.GetEntity(i);
      if (!entity.IsValid) continue;

      // Check if player is mounted or transformed - if so, skip normal movement
      bool isMounted = _world.AnimalBuddyMounts.Has(entity) && _world.AnimalBuddyMounts.Get(entity).IsMounted;
      bool isTransformed = _world.AnimalBuddyTransformations.Has(entity) && _world.AnimalBuddyTransformations.Get(entity).IsTransformed;
      
      if (isMounted || isTransformed)
        continue; // AnimalBuddySystem handles movement

      var controller = _world.PlayerControllers.GetByIndex(i);
      var velocity = _world.Velocities.Has(entity) ? _world.Velocities.Get(entity) : new Velocity();
      var groundState = _world.GroundStates.Has(entity) ? _world.GroundStates.Get(entity) : new GroundState();

      ProcessMovement(keyboard, ref controller, ref velocity, ref groundState, dt);

      _world.PlayerControllers.GetByIndex(i) = controller;
      if (_world.Velocities.Has(entity))
        _world.Velocities.Get(entity) = velocity;
      if (_world.GroundStates.Has(entity))
        _world.GroundStates.Get(entity) = groundState;
    }

    _wasCartwheelPressed = keyboard.IsKeyDown(Keys.E);
  }

  private void ProcessMovement(KeyboardState keyboard, ref PlayerController controller, ref Velocity velocity, ref GroundState groundState, float dt)
  {
    // Check modifier restrictions
    var modifierSystem = _world.GetModifierSystem();
    bool canJump = modifierSystem?.CheckInput(InputAction.Jump) ?? true;
    bool canGlide = modifierSystem?.CheckInput(InputAction.Glide) ?? true;
    bool canCartwheel = modifierSystem?.CheckInput(InputAction.Cartwheel) ?? true;
    bool canMoveLeft = modifierSystem?.CheckInput(InputAction.Left) ?? true;
    bool canMoveRight = modifierSystem?.CheckInput(InputAction.Right) ?? true;

    bool left = (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) && canMoveLeft;
    bool right = (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) && canMoveRight;
    bool down = keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down);
    bool jumpPressed = keyboard.IsKeyDown(Keys.Space) && canJump;
    bool jumpReleased = !keyboard.IsKeyDown(Keys.Space) || !canJump;
    bool cartwheelPressed = keyboard.IsKeyDown(Keys.E) && !_wasCartwheelPressed && canCartwheel; // E key for cartwheel

    // Update coyote time and jump buffer
    if (groundState.IsGrounded)
    {
      controller.CoyoteTime = _tuning.Ground.Jump.CoyoteTime;
      if (jumpPressed)
        controller.JumpBufferTime = _tuning.Ground.Jump.JumpBuffer;
      else
        controller.JumpBufferTime = MathF.Max(0, controller.JumpBufferTime - dt);
    }
    else
    {
      controller.CoyoteTime = MathF.Max(0, controller.CoyoteTime - dt);
      controller.JumpBufferTime = MathF.Max(0, controller.JumpBufferTime - dt);
    }

    // Cartwheel ground input
    if (groundState.IsGrounded && cartwheelPressed && controller.State != MovementState.Cartwheeling)
    {
      controller.State = MovementState.Cartwheeling;
      // Set velocity in facing direction (or right if no input)
      float facing = right ? 1.0f : (left ? -1.0f : 1.0f);
      velocity.Value.X = _tuning.Cartwheel.GroundSpeed * facing;
    }

    // Cartwheel logic
    if (controller.State == MovementState.Cartwheeling)
    {
      if (!groundState.IsGrounded)
      {
        controller.State = MovementState.Airborne;
        controller.InCartwheelAir = true;
        controller.HasUsedCartwheelJump = false;
      }
      else
      {
        // Maintain cartwheel speed on ground
        float facing = velocity.Value.X >= 0 ? 1.0f : -1.0f;
        velocity.Value.X = _tuning.Cartwheel.GroundSpeed * facing;
      }
    }

    // CartwheelAir: can jump once at any time
    if (controller.InCartwheelAir && jumpPressed && !controller.HasUsedCartwheelJump)
    {
      velocity.Value.Y = _tuning.Ground.Jump.InitialVelocity;
      controller.InCartwheelAir = false;
      controller.HasUsedCartwheelJump = true;
      controller.CanGlide = true; // Glide becomes allowed after cartwheel jump
      controller.State = MovementState.Airborne;
      _world.Events.Push(new PlayerJumped { Entity = _world.PlayerEntity, Velocity = velocity.Value.Y });
    }

    // Ground movement
    if (groundState.IsGrounded && controller.State != MovementState.Cartwheeling && controller.State != MovementState.Gliding && controller.State != MovementState.Swimming)
    {
      // Crouch
      if (down && !left && !right)
      {
        controller.State = MovementState.Crouching;
        velocity.Value.X *= 0.5f; // Slow down when crouching
      }
      else if (controller.State == MovementState.Crouching)
      {
        controller.State = MovementState.Ground;
      }

      // Walk/Run
      if (controller.State == MovementState.Ground)
      {
        float targetSpeed = 0;
        if (left) targetSpeed = -_tuning.Ground.WalkSpeed;
        if (right) targetSpeed = _tuning.Ground.WalkSpeed;

        float accel = _tuning.Ground.Acceleration * dt;
        if (MathF.Abs(targetSpeed) > 0.1f)
        {
          if (targetSpeed > velocity.Value.X)
            velocity.Value.X = MathF.Min(velocity.Value.X + accel, targetSpeed);
          else
            velocity.Value.X = MathF.Max(velocity.Value.X - accel, targetSpeed);
        }
        else
        {
          velocity.Value.X *= _tuning.Ground.Friction;
          if (MathF.Abs(velocity.Value.X) < 1.0f)
            velocity.Value.X = 0;
        }
      }

      // Crouch walk
      if (controller.State == MovementState.Crouching)
      {
        float targetSpeed = 0;
        if (left) targetSpeed = -_tuning.Ground.CrouchSpeed;
        if (right) targetSpeed = _tuning.Ground.CrouchSpeed;

        float accel = _tuning.Ground.Acceleration * 0.5f * dt;
        if (MathF.Abs(targetSpeed) > 0.1f)
        {
          if (targetSpeed > velocity.Value.X)
            velocity.Value.X = MathF.Min(velocity.Value.X + accel, targetSpeed);
          else
            velocity.Value.X = MathF.Max(velocity.Value.X - accel, targetSpeed);
        }
        else
        {
          velocity.Value.X *= _tuning.Ground.Friction;
        }
      }

      // Jump
      if ((jumpPressed && controller.CoyoteTime > 0) || (controller.JumpBufferTime > 0 && groundState.IsGrounded))
      {
        velocity.Value.Y = _tuning.Ground.Jump.InitialVelocity;
        controller.CoyoteTime = 0;
        controller.JumpBufferTime = 0;
        groundState.IsGrounded = false;
        controller.State = MovementState.Airborne;
        controller.CanGlide = true;
        _world.Events.Push(new PlayerJumped { Entity = _world.PlayerEntity, Velocity = velocity.Value.Y });
      }
    }

    // Swimming movement
    if (_world.Swimmings.Has(_world.PlayerEntity))
    {
      var swimming = _world.Swimmings.Get(_world.PlayerEntity);
      if (swimming.IsInWater)
      {
        controller.State = MovementState.Swimming;
        
        // Swimming movement: slower horizontal, jump-like behavior for upward movement
        float swimSpeed = _tuning.Ground.WalkSpeed * 0.6f; // Slower in water
        float swimAccel = _tuning.Ground.Acceleration * 0.4f * dt;
        
        if (left)
          velocity.Value.X = MathF.Max(velocity.Value.X - swimAccel, -swimSpeed);
        if (right)
          velocity.Value.X = MathF.Min(velocity.Value.X + swimAccel, swimSpeed);
        
        // Upward movement requires holding jump (like flying)
        if (jumpPressed)
        {
          velocity.Value.Y = -_tuning.Ground.Jump.InitialVelocity * 0.7f; // Weaker than normal jump
        }
      }
      else if (controller.State == MovementState.Swimming)
      {
        // Exited water
        controller.State = MovementState.Airborne;
      }
    }

    // Airborne movement
    if (!groundState.IsGrounded && controller.State != MovementState.Swimming)
    {
      // Variable jump (cut early)
      if (jumpReleased && velocity.Value.Y < _tuning.Ground.Jump.VariableJumpCutVelocity)
      {
        velocity.Value.Y = _tuning.Ground.Jump.VariableJumpCutVelocity;
      }

      // Glide (cannot start in CartwheelAir, check modifier)
      if (!controller.InCartwheelAir && controller.CanGlide && jumpPressed && canGlide)
      {
        if (controller.State != MovementState.Gliding || _tuning.Glide.Retriggerable)
        {
          controller.State = MovementState.Gliding;
          velocity.Value.Y = MathF.Min(velocity.Value.Y, _tuning.Glide.ClampVelocity);
        }
      }

      // Glide clamp
      if (controller.State == MovementState.Gliding)
      {
        velocity.Value.Y = MathF.Min(velocity.Value.Y, _tuning.Glide.ClampVelocity);
      }

      // Air control
      float airAccel = _tuning.Ground.Acceleration * 0.3f * dt;
      if (left)
        velocity.Value.X = MathF.Max(velocity.Value.X - airAccel, -_tuning.Ground.WalkSpeed);
      if (right)
        velocity.Value.X = MathF.Min(velocity.Value.X + airAccel, _tuning.Ground.WalkSpeed);
    }

    // Apply gravity
    if (!groundState.IsGrounded && controller.State != MovementState.Gliding && controller.State != MovementState.Swimming)
    {
      velocity.Value.Y += _tuning.Gravity * dt;
      velocity.Value.Y = MathF.Min(velocity.Value.Y, _tuning.TerminalVelocity);
    }

    // Reset cartwheel air state on landing
    if (groundState.IsGrounded && controller.InCartwheelAir)
    {
      controller.InCartwheelAir = false;
      controller.HasUsedCartwheelJump = false;
      controller.CanGlide = true;
    }
  }
}

// Tuning structs
public struct MovementTuning
{
  public GroundTuning Ground;
  public float Gravity;
  public float TerminalVelocity;
  public GlideTuning Glide;
  public CartwheelTuning Cartwheel;

  public MovementTuning()
  {
    Ground = new GroundTuning();
    Gravity = 1200.0f;
    TerminalVelocity = 600.0f;
    Glide = new GlideTuning();
    Cartwheel = new CartwheelTuning();
  }
}

public struct GroundTuning
{
  public float WalkSpeed;
  public float RunSpeed;
  public float Acceleration;
  public float Friction;
  public float CrouchSpeed;
  public JumpTuning Jump;

  public GroundTuning()
  {
    WalkSpeed = 80.0f;
    RunSpeed = 120.0f;
    Acceleration = 600.0f;
    Friction = 0.85f;
    CrouchSpeed = 40.0f;
    Jump = new JumpTuning();
  }
}

public struct JumpTuning
{
  public float InitialVelocity;
  public float VariableJumpCutVelocity;
  public float CoyoteTime;
  public float JumpBuffer;

  public JumpTuning()
  {
    InitialVelocity = -320.0f;
    VariableJumpCutVelocity = -160.0f;
    CoyoteTime = 0.1f;
    JumpBuffer = 0.15f;
  }
}

public struct GlideTuning
{
  public float ClampVelocity;
  public bool Retriggerable;

  public GlideTuning()
  {
    ClampVelocity = 100.0f;
    Retriggerable = true;
  }
}

public struct CartwheelTuning
{
  public float GroundSpeed;

  public CartwheelTuning()
  {
    GroundSpeed = 180.0f;
  }
}
