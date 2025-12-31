using System.IO;
using Microsoft.Xna.Framework.Graphics;
using GL2Engine.Engine;
using GL2Engine.Physics2D;
using GL2Engine.Gameplay;
using GL2Engine.Render;
using GL2Engine.Content;

namespace GL2Engine.ECS;

/// <summary>
/// GameWorld singleton - owns ECS storages, RNG streams, event buses, content handles, global config.
/// </summary>
public class GameWorld
{
  // Entity management
  private int _nextEntityId = 0;
  private int[] _entityGenerations;
  private const int MaxEntities = 4096;

  // Component stores (SoA)
  public ComponentStore<Position> Positions { get; private set; }
  public ComponentStore<Velocity> Velocities { get; private set; }
  public ComponentStore<Collider> Colliders { get; private set; }
  public ComponentStore<PlayerController> PlayerControllers { get; private set; }
  public ComponentStore<GroundState> GroundStates { get; private set; }
  public ComponentStore<Renderable> Renderables { get; private set; }
  public ComponentStore<TeamUp> TeamUps { get; private set; }
  public ComponentStore<Animation> Animations { get; private set; }
  public ComponentStore<MovingPlatform> MovingPlatforms { get; private set; }
  public ComponentStore<PlatformRider> PlatformRiders { get; private set; }
  public ComponentStore<Enemy> Enemies { get; private set; }
  public ComponentStore<EnemyAI> EnemyAIs { get; private set; }
  public ComponentStore<Health> Healths { get; private set; }
  public ComponentStore<Invulnerability> Invulnerabilities { get; private set; }
  public ComponentStore<WaterVolume> WaterVolumes { get; private set; }
  public ComponentStore<Swimming> Swimmings { get; private set; }
  public ComponentStore<AnimalBuddy> AnimalBuddies { get; private set; }
  public ComponentStore<AnimalBuddyMount> AnimalBuddyMounts { get; private set; }
  public ComponentStore<AnimalBuddyTransformation> AnimalBuddyTransformations { get; private set; }
  public ComponentStore<WebPlatform> WebPlatforms { get; private set; }
  public ComponentStore<LightSource> LightSources { get; private set; }
  public ComponentStore<Tool> Tools { get; private set; }
  public ComponentStore<ToolUsage> ToolUsages { get; private set; }
  public ComponentStore<CameraVolumeComponent> CameraVolumes { get; private set; }
  public ComponentStore<ItemPickup> ItemPickups { get; private set; }
  public ComponentStore<CurrencyComponent> Currencies { get; private set; }
  public ComponentStore<XPCollectibleComponent> XPCollectibles { get; private set; }
  public ComponentStore<XPDropComponent> XPDrops { get; private set; }
  public ComponentStore<FlagComponent> Flags { get; private set; }
  public ComponentStore<FlowComponent> FlowComponents { get; private set; }
  public ComponentStore<TechniqueProcState> TechniqueProcStates { get; private set; }
  public ComponentStore<ActiveModifiersComponent> ActiveModifiers { get; private set; }

  // Systems
  private PhysicsSystem _physicsSystem;
  private MovementSystem _movementSystem;
  private PlayerControllerSystem _playerControllerSystem;
  private TeamUpSystem _teamUpSystem;
  private RenderSystem _renderSystem;
  private AnimationSystem? _animationSystem;
  private EnemySystem? _enemySystem;
  private WaterSystem? _waterSystem;
  private AnimalBuddySystem? _animalBuddySystem;
  private GL2Engine.Render.CameraSystem? _cameraSystem;
  private CurrencySystem? _currencySystem;
  private FlagSystem? _flagSystem;
  private FlowSystem? _flowSystem;
  private RelicSystem? _relicSystem;
  private ModifierSystem? _modifierSystem;
  private TrophySystem? _trophySystem;

  // Collision world (shared with PhysicsSystem)
  public CollisionWorld CollisionWorld { get; private set; }

  // Event bus
  public EventBus Events { get; private set; }

  // RNG
  public Rng Rng { get; private set; }
  public RngStreams? RngStreams { get; private set; }

  // Player entities
  public Entity PlayerEntity { get; private set; }
  public Entity PartnerEntity { get; private set; }

  public GameWorld()
  {
    _entityGenerations = new int[MaxEntities];

    // Initialize component stores
    Positions = new ComponentStore<Position>(256);
    Velocities = new ComponentStore<Velocity>(256);
    Colliders = new ComponentStore<Collider>(256);
    PlayerControllers = new ComponentStore<PlayerController>(16);
    GroundStates = new ComponentStore<GroundState>(256);
    Renderables = new ComponentStore<Renderable>(256);
    TeamUps = new ComponentStore<TeamUp>(16);
    Animations = new ComponentStore<Animation>(64);
    MovingPlatforms = new ComponentStore<MovingPlatform>(32);
    PlatformRiders = new ComponentStore<PlatformRider>(32);
    Enemies = new ComponentStore<Enemy>(64);
    EnemyAIs = new ComponentStore<EnemyAI>(64);
    Healths = new ComponentStore<Health>(128);
    Invulnerabilities = new ComponentStore<Invulnerability>(128);
    WaterVolumes = new ComponentStore<WaterVolume>(16);
    Swimmings = new ComponentStore<Swimming>(32);
    AnimalBuddies = new ComponentStore<AnimalBuddy>(32);
    AnimalBuddyMounts = new ComponentStore<AnimalBuddyMount>(16);
    AnimalBuddyTransformations = new ComponentStore<AnimalBuddyTransformation>(16);
    WebPlatforms = new ComponentStore<WebPlatform>(64);
    LightSources = new ComponentStore<LightSource>(16);
    Tools = new ComponentStore<Tool>(16);
    ToolUsages = new ComponentStore<ToolUsage>(16);
    CameraVolumes = new ComponentStore<CameraVolumeComponent>(32);
    ItemPickups = new ComponentStore<ItemPickup>(64);
    Currencies = new ComponentStore<CurrencyComponent>(16);
    XPCollectibles = new ComponentStore<XPCollectibleComponent>(256);
    XPDrops = new ComponentStore<XPDropComponent>(32);
    Flags = new ComponentStore<FlagComponent>(16);
    FlowComponents = new ComponentStore<FlowComponent>(16);
    TechniqueProcStates = new ComponentStore<TechniqueProcState>(16);
    ActiveModifiers = new ComponentStore<ActiveModifiersComponent>(16);

    // Initialize systems
    Events = new EventBus();
    Rng = new Rng(12345); // Seed for determinism (legacy, use RngStreams for new code)

    CollisionWorld = new CollisionWorld();
    _physicsSystem = new PhysicsSystem(this);
    _movementSystem = new MovementSystem(this);
    _playerControllerSystem = new PlayerControllerSystem(this);
    _teamUpSystem = new TeamUpSystem(this);
    _renderSystem = new RenderSystem(this);
    _animationSystem = new AnimationSystem(this);
    _enemySystem = new EnemySystem(this);
    _waterSystem = new WaterSystem(this);
    _animalBuddySystem = new AnimalBuddySystem(this);
    _cameraSystem = new GL2Engine.Render.CameraSystem(this);
    _currencySystem = new CurrencySystem(this, 100); // Default threshold: 100 XP
    _flagSystem = new FlagSystem(this);
    _flowSystem = new FlowSystem(this);
    _relicSystem = new RelicSystem(this);
    _modifierSystem = new ModifierSystem(this);
    _trophySystem = new TrophySystem(this);
  }

  public void InitializeRender(GraphicsDevice device)
  {
    _renderSystem.SetDevice(device);
  }

  public GL2Engine.Render.CameraSystem? GetCameraSystem() => _cameraSystem;
  public CurrencySystem? GetCurrencySystem() => _currencySystem;
  public FlagSystem? GetFlagSystem() => _flagSystem;
  public FlowSystem? GetFlowSystem() => _flowSystem;
  public RelicSystem? GetRelicSystem() => _relicSystem;
  public ModifierSystem? GetModifierSystem() => _modifierSystem;
  public TrophySystem? GetTrophySystem() => _trophySystem;

  public Entity CreateEntity()
  {
    if (_nextEntityId >= MaxEntities)
      throw new System.Exception("Max entities reached");

    int id = _nextEntityId++;
    _entityGenerations[id]++;
    return new Entity(id, _entityGenerations[id]);
  }

  public void DestroyEntity(Entity entity)
  {
    if (!entity.IsValid || entity.Id >= _entityGenerations.Length)
      return;

    // Remove all components
    if (Positions.Has(entity)) Positions.Remove(entity);
    if (Velocities.Has(entity)) Velocities.Remove(entity);
    if (Colliders.Has(entity)) Colliders.Remove(entity);
    if (PlayerControllers.Has(entity)) PlayerControllers.Remove(entity);
    if (GroundStates.Has(entity)) GroundStates.Remove(entity);
    if (Renderables.Has(entity)) Renderables.Remove(entity);
    if (TeamUps.Has(entity)) TeamUps.Remove(entity);
    if (Animations.Has(entity)) Animations.Remove(entity);
    if (MovingPlatforms.Has(entity)) MovingPlatforms.Remove(entity);
    if (PlatformRiders.Has(entity)) PlatformRiders.Remove(entity);
    if (Enemies.Has(entity)) Enemies.Remove(entity);
    if (EnemyAIs.Has(entity)) EnemyAIs.Remove(entity);
    if (Healths.Has(entity)) Healths.Remove(entity);
    if (Invulnerabilities.Has(entity)) Invulnerabilities.Remove(entity);
    if (WaterVolumes.Has(entity)) WaterVolumes.Remove(entity);
    if (Swimmings.Has(entity)) Swimmings.Remove(entity);
    if (AnimalBuddies.Has(entity)) AnimalBuddies.Remove(entity);
    if (AnimalBuddyMounts.Has(entity)) AnimalBuddyMounts.Remove(entity);
    if (AnimalBuddyTransformations.Has(entity)) AnimalBuddyTransformations.Remove(entity);
    if (WebPlatforms.Has(entity)) WebPlatforms.Remove(entity);
    if (LightSources.Has(entity)) LightSources.Remove(entity);
    if (Tools.Has(entity)) Tools.Remove(entity);
    if (ToolUsages.Has(entity)) ToolUsages.Remove(entity);
    if (CameraVolumes.Has(entity)) CameraVolumes.Remove(entity);
    if (ItemPickups.Has(entity)) ItemPickups.Remove(entity);
    if (Currencies.Has(entity)) Currencies.Remove(entity);
    if (XPCollectibles.Has(entity)) XPCollectibles.Remove(entity);
    if (XPDrops.Has(entity)) XPDrops.Remove(entity);
    if (Flags.Has(entity)) Flags.Remove(entity);
    if (FlowComponents.Has(entity)) FlowComponents.Remove(entity);
    if (TechniqueProcStates.Has(entity)) TechniqueProcStates.Remove(entity);
    if (ActiveModifiers.Has(entity)) ActiveModifiers.Remove(entity);
  }

  public void UpdateFixed(float dt)
  {
    // Fixed-step update order (deterministic)
    _animalBuddySystem?.Update(dt);
    _playerControllerSystem.Update(dt);
    _teamUpSystem.Update(dt);
    _waterSystem?.Update(dt);
    _enemySystem?.Update(dt);
    _animationSystem?.Update(dt);
    _physicsSystem.Update(dt);
    _cameraSystem?.Update(dt);
    _currencySystem?.Update(dt);
    _flagSystem?.Update(dt);
    _flowSystem?.Update(dt);
    _relicSystem?.Update(dt);
    _modifierSystem?.Update(dt);
    
    // Process events (consumes ring buffer)
    Events.Process();
  }

  public void Draw(RenderPipeline pipeline)
  {
    _renderSystem.Draw(pipeline);
  }

  public void LoadTestbedLevel()
  {
    // Load level data from JSON
    var levelPath = Path.Combine("GL2Project", "Levels", "Testbed.json");
    if (!File.Exists(levelPath))
    {
      // Try alternative paths
      levelPath = Path.Combine("MonoGameProject", "Levels", "Testbed.json");
      if (!File.Exists(levelPath))
        levelPath = Path.Combine("Levels", "Testbed.json");
    }
    
    LevelData levelData;
    try
    {
      levelData = LevelLoader.LoadLevel(levelPath);
    }
    catch
    {
      levelData = new LevelData { Blocks = [] };
    }

    // Populate collision world from level blocks
    const float BlockSize = 64.0f;
    foreach (var block in levelData.Blocks)
    {
      var blockDef = LevelLoader.GetBlockDefinition(block.BlockId);
      var worldPos = new Microsoft.Xna.Framework.Vector2(
        block.GridX * BlockSize + blockDef.Size.X * 0.5f,
        block.GridY * BlockSize + blockDef.Size.Y * 0.5f
      );

      if (blockDef.CollisionType == CollisionType.Slope && blockDef.Slope.HasValue)
      {
        // Create slope segment
        var slope = blockDef.Slope.Value;
        var slopeSeg = SlopeSolver.CreateSlopeSegment(blockDef, worldPos - blockDef.Size * 0.5f);
        CollisionWorld.AddSlopeSegment(slopeSeg);
      }
      else if (blockDef.CollisionType == CollisionType.AABB)
      {
        // Add static AABB
        CollisionWorld.AddStaticAABB(worldPos, blockDef.Size * 0.5f);
      }
    }

    // Create player entity
    PlayerEntity = CreateEntity();
    Positions.Add(PlayerEntity, new Position { Value = new Microsoft.Xna.Framework.Vector2(100, 200) });
    Velocities.Add(PlayerEntity, new Velocity { Value = Microsoft.Xna.Framework.Vector2.Zero });
    Colliders.Add(PlayerEntity, new Collider { Size = new Microsoft.Xna.Framework.Vector2(16, 24), Type = ColliderType.Capsule });
    Renderables.Add(PlayerEntity, new Renderable 
    { 
      MeshId = 0, 
      Z = 0f, 
      Layer = RenderLayer.MidLayer,
      ParallaxFactor = 1.0f
    });
    PlayerControllers.Add(PlayerEntity, new PlayerController
    {
      State = MovementState.Ground,
      CoyoteTime = 0,
      JumpBufferTime = 0,
      CanGlide = false,
      InCartwheelAir = false,
      HasUsedCartwheelJump = false
    });
    GroundStates.Add(PlayerEntity, new GroundState
    {
      IsGrounded = true,
      GroundNormal = new Microsoft.Xna.Framework.Vector2(0, -1),
      GroundAngle = 0,
      OnSlope = false,
      CanSlide = false
    });
    Healths.Add(PlayerEntity, new Health { Current = 2, Max = 2 });

    // Create partner entity
    PartnerEntity = CreateEntity();
    Positions.Add(PartnerEntity, new Position { Value = new Microsoft.Xna.Framework.Vector2(150, 200) });
    Velocities.Add(PartnerEntity, new Velocity { Value = Microsoft.Xna.Framework.Vector2.Zero });
    Colliders.Add(PartnerEntity, new Collider { Size = new Microsoft.Xna.Framework.Vector2(16, 24), Type = ColliderType.Capsule });
    Renderables.Add(PartnerEntity, new Renderable 
    { 
      MeshId = 0, 
      Z = 0f, 
      Layer = RenderLayer.MidLayer,
      ParallaxFactor = 1.0f
    });
    TeamUps.Add(PlayerEntity, new TeamUp { PartnerEntity = PartnerEntity, IsCarrying = false, IsCarried = false });
    Healths.Add(PartnerEntity, new Health { Current = 2, Max = 2 });
  }
}
