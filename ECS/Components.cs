using Microsoft.Xna.Framework;

namespace GL2Engine.ECS;

// Component structs - all value types for SoA storage

public struct Position
{
  public Vector2 Value;
}

public struct Velocity
{
  public Vector2 Value;
}

public struct Collider
{
  public Vector2 Size; // Half-extents for AABB
  public ColliderType Type;
}

public enum ColliderType
{
  AABB,
  Capsule
}

public struct PlayerController
{
  public MovementState State;
  public float CoyoteTime;
  public float JumpBufferTime;
  public bool CanGlide;
  public bool InCartwheelAir;
  public bool HasUsedCartwheelJump;
}

public enum MovementState
{
  Ground,
  Airborne,
  Crouching,
  Cartwheeling,
  Gliding,
  Swimming
}

public struct GroundState
{
  public bool IsGrounded;
  public Vector2 GroundNormal;
  public float GroundAngle; // degrees
  public bool OnSlope;
  public bool CanSlide;
}

public struct Renderable
{
  public int MeshId; // Reference to 3D mesh/model
  public float Z; // Render layer (Z depth)
  public RenderLayer Layer; // Render layer group
  public float ParallaxFactor; // Parallax factor for this layer (0 = no parallax, 1 = full parallax)
}

/// <summary>
/// Render layer groups for depth sorting.
/// </summary>
public enum RenderLayer
{
  BackgroundLayer, // Far parallax
  MidLayer,        // Gameplay plane
  ForegroundLayer  // Occluders + vignette
}

public struct TeamUp
{
  public Entity PartnerEntity;
  public bool IsCarrying;
  public bool IsCarried;
}

// New components for future features
public struct Animation
{
  public int ModelId;
  public AnimationState State;
}

public struct AnimationState
{
  public int CurrentClip;
  public float Time;
  public bool Looping;
}

public struct MovingPlatform
{
  public Vector2 StartPos;
  public Vector2 EndPos;
  public float Speed;
  public float CurrentT;
  public bool PingPong;
  public bool IsConveyorBelt; // Conveyor belts don't carry player
}

public struct PlatformRider
{
  public Entity PlatformEntity;
}

public struct Enemy
{
  public EnemyType Type;
  public int Health;
  public EnemyState State;
}

public enum EnemyType
{
  Basic,
  Flying,
  Ground
}

public enum EnemyState
{
  Idle,
  Patrolling,
  Dead
}

public struct EnemyAI
{
  public Vector2[] Waypoints;
  public float DetectionRadius;
  public float PatrolSpeed;
  public int CurrentWaypoint;
}

public struct Health
{
  public int Current;
  public int Max;
}

public struct Invulnerability
{
  public float RemainingTime;
}

public struct WaterVolume
{
  public Vector2 Position;
  public Vector2 Size;
  public float Buoyancy;
  public float Drag;
}

public struct Swimming
{
  public bool IsInWater;
  public Entity WaterEntity;
}

// Animal Buddy Components
public enum AnimalBuddyType
{
  // DKC2
  Rattly,    // Rattlesnake - high jump, bounce attack
  Squawks,   // Parrot - flight, egg attack, carry
  Glimmer,   // Anglerfish - underwater, light
  Squitter,  // Spider - web platforms, web attack
  Clapper,   // Seal - fast swimming
  // DKC1
  Rambi,     // Rhinoceros - charge attack, break walls
  Enguarde,  // Swordfish - underwater attack
  Winky,     // Frog - high jump, bounce on enemies
  Expresso,  // Ostrich - glide, fast run
  // DKC3
  Ellie,     // Elephant - water spray, carry barrels
  Nibbla,    // Fish - underwater buddy
  Quawks,    // Purple Parrot - carry, merge with Squawks
  // Unused/New
  Hooter,    // Owl - flight, night vision
  Miney      // Mole - dig, underground
}

public enum AnimalBuddyState
{
  Idle,           // Buddy waiting in level
  Mounted,        // Player riding buddy
  Transforming,   // Player transforming into buddy
  Transformed     // Player is now the buddy
}

public struct AnimalBuddy
{
  public AnimalBuddyType Type;
  public AnimalBuddyState State;
  public Entity RiderEntity;      // Entity riding (if mounted)
  public float CooldownTimer;     // Ability cooldowns
}

public struct AnimalBuddyMount
{
  public Entity BuddyEntity;      // Which buddy is mounted
  public bool IsMounted;
}

public struct AnimalBuddyTransformation
{
  public AnimalBuddyType TransformedType;
  public bool IsTransformed;
  public float TransformationTimer; // Duration remaining (0 = infinite until stop sign)
}

// Web platform created by Squitter
public struct WebPlatform
{
  public float Lifetime;          // Time until platform disappears
  public bool IsActive;
}

// Light source from Glimmer
public struct LightSource
{
  public float Radius;
  public float Intensity;
}

// Camera volume for room bounds
public struct CameraVolumeComponent
{
  public Microsoft.Xna.Framework.Rectangle Bounds;
  public Microsoft.Xna.Framework.Vector2 CameraOffset;
  public bool AllowVerticalFollow;
  public int VolumeId;
}

// Inventory component
public struct InventoryComponent
{
  public GL2Engine.Inventory.Inventory Inventory;
}

// Item pickup component
public struct ItemPickup
{
  public string ItemId;
  public int Count;
  public float PickupRadius;
  public bool IsCollected;
}

// Tool component for movement tools (grapple, balloon, etc.)
public struct Tool
{
  public ToolType Type;
  public int Charges; // Current charges remaining
  public int MaxCharges; // Maximum charges
  public float CooldownTimer; // Cooldown remaining
  public float CooldownDuration; // Cooldown duration
  public bool IsZoneScoped; // If true, tool only works in specific zones
  public string? ZoneId; // Zone ID if zone-scoped
}

public enum ToolType
{
  Grapple,
  Balloon,
  Spring
}

// Tool usage state
public struct ToolUsage
{
  public ToolType ActiveTool;
  public Microsoft.Xna.Framework.Vector2 TargetPosition; // For grapple
  public float UsageTimer; // How long tool has been active
  public bool IsActive;
}

// Currency/XP system components
public struct CurrencyComponent
{
  public int CurrentXP;
  public int TotalCollected; // Lifetime total
  public int NextRewardThreshold;
  public int RewardThresholdInterval; // Default: 100
}

public struct XPCollectibleComponent
{
  public int Value; // XP amount
  public bool IsCollected;
}

public struct XPDropComponent
{
  public int Value; // XP amount in drop
  public bool IsCollected;
  public float Lifetime; // How long drop has existed
}

// Flag/checkpoint component
public struct FlagComponent
{
  public int FlagId; // Unique per stage
  public int FlagType; // 0=Start, 1=Middle, 2=End (maps to GL2Engine.World.FlagType enum)
  public bool IsConsumable; // Can be consumed for rewards
  public bool IsConsumed; // Has been consumed
}

// Flow Meter component (stage-local)
public struct FlowComponent
{
  public float Flow; // 0..1, normalized to FlowMax for UI
  public float DecayTimer; // Seconds since last progress
  public float LastProgressTime; // Timestamp of last Flow gain
  public ushort ComboCount; // Optional, for chain tracking
  public FlowMode FlowMode; // FAST or SLOW
}

public enum FlowMode
{
  FAST,
  SLOW
}

// Technique Mod proc state component
public struct TechniqueProcState
{
  public float[] CooldownTimers; // One per equipped Technique Mod (max 10)
  public bool[] PerSectionTriggered; // Flags for "already triggered this section" (max 10)
  public int[] ChargeCounts; // For charge-based mods (max 10)
  public int EquippedCount; // Number of equipped mods
}

// Active modifiers component (run/stage-scoped)
public struct ActiveModifiersComponent
{
  public int[] ModifierIds; // Fixed-size array (max 10)
  public int ModifierCount; // Active count
  public float RewardMultiplier; // Aggregate multiplier
}