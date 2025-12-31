using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GL2Engine.ECS;
using System.Text.Json;

namespace GL2Engine.Gameplay;

/// <summary>
/// Team-up system - handles partner pickup/throw mechanics.
/// </summary>
public class TeamUpSystem
{
  private GameWorld _world;
  private TeamUpTuning _tuning;

  public TeamUpSystem(GameWorld world)
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
      
      // Initialize tuning values
      float neutralThrowX = 150.0f;
      float neutralThrowY = -200.0f;
      float upThrowX = 0.0f;
      float upThrowY = -350.0f;
      float followSnapDistance = 64.0f;
      
      if (root.TryGetProperty("teamUp", out var teamUp))
      {
        if (teamUp.TryGetProperty("throwVelocity", out var throwVel))
        {
          if (throwVel.TryGetProperty("neutral", out var neutral))
          {
            neutralThrowX = neutral.GetProperty("x").GetSingle();
            neutralThrowY = neutral.GetProperty("y").GetSingle();
          }
          if (throwVel.TryGetProperty("up", out var up))
          {
            upThrowX = up.GetProperty("x").GetSingle();
            upThrowY = up.GetProperty("y").GetSingle();
          }
        }
        if (teamUp.TryGetProperty("followSnapDistance", out var snapDist))
        {
          followSnapDistance = snapDist.GetProperty("value").GetSingle();
        }
      }
      
      // Build tuning struct - assign directly since struct is no longer readonly
      _tuning = new TeamUpTuning
      {
        NeutralThrowX = neutralThrowX,
        NeutralThrowY = neutralThrowY,
        UpThrowX = upThrowX,
        UpThrowY = upThrowY,
        FollowSnapDistance = followSnapDistance
      };
    }
    catch
    {
      _tuning = new TeamUpTuning();
    }
  }

  public void Update(float dt)
  {
    var keyboard = Keyboard.GetState();
    bool pickupKey = keyboard.IsKeyDown(Keys.E);
    bool throwKey = keyboard.IsKeyDown(Keys.Q);
    bool upThrow = keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up);

    // Check all entities with TeamUp component
    for (int i = 0; i < _world.TeamUps.Count; i++)
    {
      var entity = _world.TeamUps.GetEntity(i);
      if (!entity.IsValid) continue;

      var teamUp = _world.TeamUps.GetByIndex(i);
      var playerPos = _world.Positions.Has(entity) ? _world.Positions.Get(entity) : new Position();
      
      if (teamUp.PartnerEntity.IsValid && _world.Positions.Has(teamUp.PartnerEntity))
      {
        var partnerPos = _world.Positions.Get(teamUp.PartnerEntity);
        var partnerVel = _world.Velocities.Has(teamUp.PartnerEntity) ? _world.Velocities.Get(teamUp.PartnerEntity) : new Velocity();
        var partnerGround = _world.GroundStates.Has(teamUp.PartnerEntity) ? _world.GroundStates.Get(teamUp.PartnerEntity) : new GroundState();

        float distance = Vector2.Distance(playerPos.Value, partnerPos.Value);
        const float PickupDistance = 32.0f;

        // Pickup
        if (!teamUp.IsCarrying && pickupKey && distance < PickupDistance && partnerGround.IsGrounded)
        {
          teamUp.IsCarrying = true;
          teamUp.IsCarried = true;
          partnerVel.Value = Vector2.Zero;
          if (_world.Velocities.Has(teamUp.PartnerEntity))
            _world.Velocities.Get(teamUp.PartnerEntity) = partnerVel;
        }

        // Throw
        if (teamUp.IsCarrying && throwKey)
        {
          teamUp.IsCarrying = false;
          teamUp.IsCarried = false;

          if (upThrow)
          {
            partnerVel.Value = new Vector2(_tuning.UpThrowX, _tuning.UpThrowY);
          }
          else
          {
            // Throw in facing direction
            float facing = playerPos.Value.X < partnerPos.Value.X ? 1 : -1;
            partnerVel.Value = new Vector2(_tuning.NeutralThrowX * facing, _tuning.NeutralThrowY);
          }

          if (_world.Velocities.Has(teamUp.PartnerEntity))
            _world.Velocities.Get(teamUp.PartnerEntity) = partnerVel;

          // Up-throw follow snap
          if (upThrow)
          {
            // When partner lands, try to move player near landing point
            // This will be handled when partner lands
          }
        }

        // Update partner position when carrying
        if (teamUp.IsCarrying)
        {
          partnerPos.Value = playerPos.Value + new Vector2(0, -40); // Above player
          _world.Positions.Get(teamUp.PartnerEntity) = partnerPos;
        }

        // Follow snap on landing
        if (!teamUp.IsCarried && partnerGround.IsGrounded && upThrow && distance > _tuning.FollowSnapDistance)
        {
          // Move player closer to partner if valid
          var newPlayerPos = partnerPos.Value + new Vector2(0, 40);
          if (IsValidPosition(newPlayerPos))
          {
            playerPos.Value = newPlayerPos;
            _world.Positions.Get(entity) = playerPos;
          }
        }
      }

      _world.TeamUps.GetByIndex(i) = teamUp;
    }
  }

  private bool IsValidPosition(Vector2 pos)
  {
    // Simple bounds check
    return pos.Y > 0 && pos.Y < 1000 && pos.X > -1000 && pos.X < 1000;
  }
}

public struct TeamUpTuning
{
  public float NeutralThrowX;
  public float NeutralThrowY;
  public float UpThrowX;
  public float UpThrowY;
  public float FollowSnapDistance;

  public TeamUpTuning()
  {
    NeutralThrowX = 150.0f;
    NeutralThrowY = -200.0f;
    UpThrowX = 0.0f;
    UpThrowY = -350.0f;
    FollowSnapDistance = 64.0f;
  }
}
