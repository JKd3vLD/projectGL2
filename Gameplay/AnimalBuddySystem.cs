using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GL2Engine.ECS;

namespace GL2Engine.Gameplay;

/// <summary>
/// Animal Buddy system - handles mount, transformation, and buddy-specific controls.
/// </summary>
public class AnimalBuddySystem
{
  private GameWorld _world;
  private bool _wasMountPressed = false;
  private bool _wasTransformPressed = false;

  // Control schema tuning for each buddy
  private readonly Dictionary<AnimalBuddyType, BuddyTuning> _buddyTuning = new();

  public AnimalBuddySystem(GameWorld world)
  {
    _world = world;
    InitializeBuddyTuning();
  }

  private void InitializeBuddyTuning()
  {
    // Rattly - high jump, bounce attack
    _buddyTuning[AnimalBuddyType.Rattly] = new BuddyTuning
    {
      JumpMultiplier = 3.0f,
      SpeedMultiplier = 1.5f,
      CanGlide = false,
      CanClimbWalls = false,
      IsUnderwaterOnly = false
    };

    // Squawks - flight, egg attack
    _buddyTuning[AnimalBuddyType.Squawks] = new BuddyTuning
    {
      JumpMultiplier = 0.5f, // Lower jump, uses flight instead
      SpeedMultiplier = 0.8f,
      CanGlide = false,
      CanFly = true,
      CanClimbWalls = false,
      IsUnderwaterOnly = false
    };

    // Glimmer - underwater, light
    _buddyTuning[AnimalBuddyType.Glimmer] = new BuddyTuning
    {
      JumpMultiplier = 1.0f,
      SpeedMultiplier = 1.2f,
      CanGlide = false,
      IsUnderwaterOnly = true,
      HasLightSource = true
    };

    // Squitter - web platforms, web attack, wall climb
    _buddyTuning[AnimalBuddyType.Squitter] = new BuddyTuning
    {
      JumpMultiplier = 1.0f,
      SpeedMultiplier = 1.0f,
      CanGlide = false,
      CanClimbWalls = true,
      IsUnderwaterOnly = false
    };

    // Clapper - fast swimming
    _buddyTuning[AnimalBuddyType.Clapper] = new BuddyTuning
    {
      JumpMultiplier = 1.0f,
      SpeedMultiplier = 2.0f,
      CanGlide = false,
      IsUnderwaterOnly = true
    };

    // Rambi - charge attack
    _buddyTuning[AnimalBuddyType.Rambi] = new BuddyTuning
    {
      JumpMultiplier = 1.0f,
      SpeedMultiplier = 2.0f,
      CanGlide = false,
      CanCharge = true,
      IsUnderwaterOnly = false
    };

    // Enguarde - underwater attack
    _buddyTuning[AnimalBuddyType.Enguarde] = new BuddyTuning
    {
      JumpMultiplier = 1.0f,
      SpeedMultiplier = 1.5f,
      CanGlide = false,
      IsUnderwaterOnly = true,
      HasDashAttack = true
    };

    // Winky - high jump
    _buddyTuning[AnimalBuddyType.Winky] = new BuddyTuning
    {
      JumpMultiplier = 2.5f,
      SpeedMultiplier = 1.0f,
      CanGlide = false,
      IsUnderwaterOnly = false
    };

    // Expresso - glide, fast run
    _buddyTuning[AnimalBuddyType.Expresso] = new BuddyTuning
    {
      JumpMultiplier = 1.0f,
      SpeedMultiplier = 2.5f,
      CanGlide = true,
      IsUnderwaterOnly = false
    };

    // Ellie - water spray
    _buddyTuning[AnimalBuddyType.Ellie] = new BuddyTuning
    {
      JumpMultiplier = 1.0f,
      SpeedMultiplier = 1.0f,
      CanGlide = false,
      IsUnderwaterOnly = false,
      CanSprayWater = true
    };

    // Nibbla - underwater
    _buddyTuning[AnimalBuddyType.Nibbla] = new BuddyTuning
    {
      JumpMultiplier = 1.0f,
      SpeedMultiplier = 1.3f,
      CanGlide = false,
      IsUnderwaterOnly = true
    };

    // Quawks - same as Squawks (will merge)
    _buddyTuning[AnimalBuddyType.Quawks] = _buddyTuning[AnimalBuddyType.Squawks];

    // Hooter - flight, night vision
    _buddyTuning[AnimalBuddyType.Hooter] = new BuddyTuning
    {
      JumpMultiplier = 0.5f,
      SpeedMultiplier = 1.0f,
      CanGlide = false,
      CanFly = true,
      HasNightVision = true,
      IsUnderwaterOnly = false
    };

    // Miney - dig
    _buddyTuning[AnimalBuddyType.Miney] = new BuddyTuning
    {
      JumpMultiplier = 1.0f,
      SpeedMultiplier = 0.8f,
      CanGlide = false,
      CanDig = true,
      IsUnderwaterOnly = false
    };
  }

  public void Update(float dt)
  {
    var keyboard = Keyboard.GetState();
    bool mountPressed = keyboard.IsKeyDown(Keys.E) && !_wasMountPressed;
    bool transformPressed = keyboard.IsKeyDown(Keys.T) && !_wasTransformPressed;

    _wasMountPressed = keyboard.IsKeyDown(Keys.E);
    _wasTransformPressed = keyboard.IsKeyDown(Keys.T);

    // Process mount/transform for player
    if (_world.PlayerEntity.IsValid)
    {
      ProcessMountInput(mountPressed);
      ProcessTransformationInput(transformPressed, dt);
      ProcessBuddyControls(dt);
    }

    // Update web platforms (Squitter)
    UpdateWebPlatforms(dt);
  }

  private void ProcessMountInput(bool mountPressed)
  {
    if (!_world.PlayerEntity.IsValid) return;

    var mount = _world.AnimalBuddyMounts.Has(_world.PlayerEntity) 
      ? _world.AnimalBuddyMounts.Get(_world.PlayerEntity) 
      : new AnimalBuddyMount { IsMounted = false };

    if (mountPressed)
    {
      if (mount.IsMounted)
      {
        // Dismount
        if (mount.BuddyEntity.IsValid && _world.AnimalBuddies.Has(mount.BuddyEntity))
        {
          var buddy = _world.AnimalBuddies.Get(mount.BuddyEntity);
          buddy.State = AnimalBuddyState.Idle;
          buddy.RiderEntity = Entity.Invalid;
          _world.AnimalBuddies.Get(mount.BuddyEntity) = buddy;
        }
        mount.IsMounted = false;
        mount.BuddyEntity = Entity.Invalid;
      }
      else
      {
        // Try to mount nearby buddy
        var playerPos = _world.Positions.Get(_world.PlayerEntity);
        const float MountDistance = 32.0f;

        for (int i = 0; i < _world.AnimalBuddies.Count; i++)
        {
          var buddyEntity = _world.AnimalBuddies.GetEntity(i);
          if (!buddyEntity.IsValid || !_world.Positions.Has(buddyEntity)) continue;

          var buddy = _world.AnimalBuddies.GetByIndex(i);
          if (buddy.State != AnimalBuddyState.Idle) continue;

          var buddyPos = _world.Positions.Get(buddyEntity);
          float distance = Vector2.Distance(playerPos.Value, buddyPos.Value);

          if (distance < MountDistance)
          {
            // Mount the buddy
            mount.IsMounted = true;
            mount.BuddyEntity = buddyEntity;
            buddy.State = AnimalBuddyState.Mounted;
            buddy.RiderEntity = _world.PlayerEntity;
            _world.AnimalBuddies.GetByIndex(i) = buddy;
            break;
          }
        }
      }

      if (_world.AnimalBuddyMounts.Has(_world.PlayerEntity))
        _world.AnimalBuddyMounts.Get(_world.PlayerEntity) = mount;
      else
        _world.AnimalBuddyMounts.Add(_world.PlayerEntity, mount);
    }
  }

  private void ProcessTransformationInput(bool transformPressed, float dt)
  {
    if (!_world.PlayerEntity.IsValid) return;

    // Check for transformation barrel collision (handled in PhysicsSystem)
    // Transformation happens when player touches buddy barrel
    // Transform back when hitting stop sign or leaving area

    var transform = _world.AnimalBuddyTransformations.Has(_world.PlayerEntity)
      ? _world.AnimalBuddyTransformations.Get(_world.PlayerEntity)
      : new AnimalBuddyTransformation { IsTransformed = false };

    if (transform.IsTransformed && transform.TransformationTimer > 0)
    {
      transform.TransformationTimer -= dt;
      if (transform.TransformationTimer <= 0)
      {
        // Timer expired, transform back
        transform.IsTransformed = false;
      }
      _world.AnimalBuddyTransformations.Get(_world.PlayerEntity) = transform;
    }
  }

  private void ProcessBuddyControls(float dt)
  {
    if (!_world.PlayerEntity.IsValid) return;

    var keyboard = Keyboard.GetState();
    var mount = _world.AnimalBuddyMounts.Has(_world.PlayerEntity)
      ? _world.AnimalBuddyMounts.Get(_world.PlayerEntity)
      : new AnimalBuddyMount { IsMounted = false };

    var transform = _world.AnimalBuddyTransformations.Has(_world.PlayerEntity)
      ? _world.AnimalBuddyTransformations.Get(_world.PlayerEntity)
      : new AnimalBuddyTransformation { IsTransformed = false };

    AnimalBuddyType? activeBuddyType = null;

    if (mount.IsMounted && mount.BuddyEntity.IsValid && _world.AnimalBuddies.Has(mount.BuddyEntity))
    {
      var buddy = _world.AnimalBuddies.Get(mount.BuddyEntity);
      activeBuddyType = buddy.Type;
      ApplyBuddyControls(buddy.Type, mount.BuddyEntity, keyboard, dt);
    }
    else if (transform.IsTransformed)
    {
      activeBuddyType = transform.TransformedType;
      ApplyBuddyControls(transform.TransformedType, _world.PlayerEntity, keyboard, dt);
    }
  }

  private void ApplyBuddyControls(AnimalBuddyType type, Entity entity, KeyboardState keyboard, float dt)
  {
    if (!_buddyTuning.TryGetValue(type, out var tuning)) return;
    if (!_world.Positions.Has(entity) || !_world.Velocities.Has(entity)) return;

    var velocity = _world.Velocities.Get(entity);
    var position = _world.Positions.Get(entity);
    var groundState = _world.GroundStates.Has(entity) 
      ? _world.GroundStates.Get(entity) 
      : new GroundState();

    // Get or create buddy component for cooldown tracking
    AnimalBuddy buddy;
    if (_world.AnimalBuddies.Has(entity))
    {
      buddy = _world.AnimalBuddies.Get(entity);
      buddy.CooldownTimer = MathF.Max(0, buddy.CooldownTimer - dt);
    }
    else
    {
      buddy = new AnimalBuddy { Type = type, CooldownTimer = 0 };
    }

    bool left = keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left);
    bool right = keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right);
    bool down = keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down);
    bool jump = keyboard.IsKeyDown(Keys.Space);
    bool attack = keyboard.IsKeyDown(Keys.X); // X for attack/ability

    switch (type)
    {
      case AnimalBuddyType.Rattly:
        HandleRattlyControls(ref velocity, ref groundState, ref buddy, left, right, jump, down, tuning, dt);
        break;
      case AnimalBuddyType.Squawks:
      case AnimalBuddyType.Quawks:
        HandleSquawksControls(ref velocity, ref groundState, ref buddy, left, right, jump, attack, tuning, dt);
        break;
      case AnimalBuddyType.Glimmer:
        HandleGlimmerControls(ref velocity, ref groundState, ref buddy, left, right, jump, tuning, dt);
        break;
      case AnimalBuddyType.Squitter:
        HandleSquitterControls(ref velocity, ref groundState, ref buddy, left, right, jump, down, attack, tuning, dt);
        break;
      case AnimalBuddyType.Clapper:
        HandleClapperControls(ref velocity, ref groundState, ref buddy, left, right, jump, tuning, dt);
        break;
      case AnimalBuddyType.Rambi:
        HandleRambiControls(ref velocity, ref groundState, ref buddy, left, right, jump, attack, tuning, dt);
        break;
      case AnimalBuddyType.Enguarde:
        HandleEnguardeControls(ref velocity, ref groundState, ref buddy, left, right, jump, attack, tuning, dt);
        break;
      case AnimalBuddyType.Winky:
        HandleWinkyControls(ref velocity, ref groundState, ref buddy, left, right, jump, tuning, dt);
        break;
      case AnimalBuddyType.Expresso:
        HandleExpressoControls(ref velocity, ref groundState, ref buddy, left, right, jump, tuning, dt);
        break;
      case AnimalBuddyType.Ellie:
        HandleEllieControls(ref velocity, ref groundState, ref buddy, left, right, jump, attack, tuning, dt);
        break;
      case AnimalBuddyType.Nibbla:
        HandleNibblaControls(ref velocity, ref groundState, ref buddy, left, right, jump, tuning, dt);
        break;
      case AnimalBuddyType.Hooter:
        HandleHooterControls(ref velocity, ref groundState, ref buddy, left, right, jump, tuning, dt);
        break;
      case AnimalBuddyType.Miney:
        HandleMineyControls(ref velocity, ref groundState, ref buddy, left, right, jump, down, attack, tuning, dt);
        break;
    }

    _world.Velocities.Get(entity) = velocity;
    if (_world.GroundStates.Has(entity))
      _world.GroundStates.Get(entity) = groundState;
    if (_world.AnimalBuddies.Has(entity))
      _world.AnimalBuddies.Get(entity) = buddy;
  }

  // Individual buddy control handlers
  private void HandleRattlyControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, bool down, BuddyTuning tuning, float dt)
  {
    // High jump (3x normal)
    if (jump && groundState.IsGrounded)
    {
      velocity.Value.Y = -320.0f * tuning.JumpMultiplier;
      groundState.IsGrounded = false;
    }

    // Bounce attack (down in air)
    if (down && !groundState.IsGrounded && velocity.Value.Y > 0)
    {
      velocity.Value.Y = 400.0f; // Bounce downward
    }

    // Faster ground movement
    float speed = 120.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
  }

  private void HandleSquawksControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, bool attack, BuddyTuning tuning, float dt)
  {
    // Flight mode (hold jump to fly up)
    if (jump && !groundState.IsGrounded)
    {
      velocity.Value.Y = -150.0f; // Fly upward
    }

    // Egg attack
    if (attack && buddy.CooldownTimer <= 0)
    {
      // Create egg projectile (handled elsewhere)
      buddy.CooldownTimer = 0.5f;
    }

    // Slower ground movement
    float speed = 80.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
  }

  private void HandleGlimmerControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, BuddyTuning tuning, float dt)
  {
    // Underwater only - fast swimming
    float speed = 100.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
    if (jump) velocity.Value.Y = -200.0f; // Swim up
  }

  private void HandleSquitterControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, bool down, bool attack, BuddyTuning tuning, float dt)
  {
    // Web platform creation (up + jump)
    if (down && jump && buddy.CooldownTimer <= 0)
    {
      // Create web platform entity (handled elsewhere)
      buddy.CooldownTimer = 1.0f;
    }

    // Web attack
    if (attack && buddy.CooldownTimer <= 0)
    {
      // Create web projectile (handled elsewhere)
      buddy.CooldownTimer = 0.3f;
    }

    // Wall climbing (handled in PhysicsSystem)
    float speed = 80.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
    if (jump && groundState.IsGrounded)
      velocity.Value.Y = -320.0f;
  }

  private void HandleClapperControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, BuddyTuning tuning, float dt)
  {
    // Fast swimming underwater
    float speed = 150.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
    if (jump) velocity.Value.Y = -250.0f; // Swim up
  }

  private void HandleRambiControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, bool attack, BuddyTuning tuning, float dt)
  {
    // Charge attack
    if (attack && buddy.CooldownTimer <= 0)
    {
      velocity.Value.X = (right ? 1 : -1) * 300.0f; // Charge
      buddy.CooldownTimer = 1.0f;
    }

    float speed = 120.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
    if (jump && groundState.IsGrounded)
      velocity.Value.Y = -320.0f;
  }

  private void HandleEnguardeControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, bool attack, BuddyTuning tuning, float dt)
  {
    // Dash attack underwater
    if (attack && buddy.CooldownTimer <= 0)
    {
      Vector2 direction = new Vector2(right ? 1 : (left ? -1 : 0), jump ? -1 : 0);
      if (direction.LengthSquared() > 0)
      {
        direction.Normalize();
        velocity.Value += direction * 400.0f;
      }
      buddy.CooldownTimer = 0.5f;
    }

    float speed = 120.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
    if (jump) velocity.Value.Y = -200.0f;
  }

  private void HandleWinkyControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, BuddyTuning tuning, float dt)
  {
    // High jump
    if (jump && groundState.IsGrounded)
      velocity.Value.Y = -320.0f * tuning.JumpMultiplier;

    float speed = 80.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
  }

  private void HandleExpressoControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, BuddyTuning tuning, float dt)
  {
    // Fast run and glide
    float speed = 150.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
    
    if (jump && groundState.IsGrounded)
      velocity.Value.Y = -320.0f;
    
    // Glide (handled in PlayerController when in air)
  }

  private void HandleEllieControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, bool attack, BuddyTuning tuning, float dt)
  {
    // Water spray attack
    if (attack && buddy.CooldownTimer <= 0)
    {
      // Create water projectile (handled elsewhere)
      buddy.CooldownTimer = 0.8f;
    }

    float speed = 80.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
    if (jump && groundState.IsGrounded)
      velocity.Value.Y = -320.0f;
  }

  private void HandleNibblaControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, BuddyTuning tuning, float dt)
  {
    // Underwater swimming
    float speed = 100.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
    if (jump) velocity.Value.Y = -200.0f;
  }

  private void HandleHooterControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, BuddyTuning tuning, float dt)
  {
    // Flight similar to Squawks
    if (jump && !groundState.IsGrounded)
      velocity.Value.Y = -150.0f;

    float speed = 80.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
  }

  private void HandleMineyControls(ref Velocity velocity, ref GroundState groundState, ref AnimalBuddy buddy,
    bool left, bool right, bool jump, bool down, bool attack, BuddyTuning tuning, float dt)
  {
    // Dig ability (down + attack)
    if (down && attack && groundState.IsGrounded && buddy.CooldownTimer <= 0)
    {
      // Dig into ground (handled elsewhere)
      buddy.CooldownTimer = 2.0f;
    }

    float speed = 60.0f * tuning.SpeedMultiplier;
    if (left) velocity.Value.X = -speed;
    if (right) velocity.Value.X = speed;
    if (jump && groundState.IsGrounded)
      velocity.Value.Y = -320.0f;
  }

  private void UpdateWebPlatforms(float dt)
  {
    for (int i = 0; i < _world.WebPlatforms.Count; i++)
    {
      var entity = _world.WebPlatforms.GetEntity(i);
      if (!entity.IsValid) continue;

      var platform = _world.WebPlatforms.GetByIndex(i);
      platform.Lifetime -= dt;
      
      if (platform.Lifetime <= 0)
      {
        platform.IsActive = false;
        // Remove platform entity (handled elsewhere)
      }

      _world.WebPlatforms.GetByIndex(i) = platform;
    }
  }
}

// Tuning structure for buddy abilities
public struct BuddyTuning
{
  public float JumpMultiplier;
  public float SpeedMultiplier;
  public bool CanGlide;
  public bool CanFly;
  public bool CanClimbWalls;
  public bool IsUnderwaterOnly;
  public bool CanCharge;
  public bool HasDashAttack;
  public bool HasLightSource;
  public bool CanSprayWater;
  public bool HasNightVision;
  public bool CanDig;
}

