using System.Collections.Generic;
using GL2Engine.ECS;

namespace GL2Engine.Render;

/// <summary>
/// Animation system - updates animation playback for 3D models.
/// </summary>
public class AnimationSystem
{
  private GameWorld _world;

  public AnimationSystem(GameWorld world)
  {
    _world = world;
  }

  public void Update(float dt)
  {
    // Update all animation components
    for (int i = 0; i < _world.Animations.Count; i++)
    {
      var entity = _world.Animations.GetEntity(i);
      if (!entity.IsValid) continue;

      var animation = _world.Animations.GetByIndex(i);
      var state = animation.State;

      // Update animation time
      state.Time += dt;

      // Get clip duration from registry
      float duration = AnimationClipRegistry.GetDuration(state.CurrentClip);
      
      // Loop if needed
      if (state.Looping && state.Time > duration)
      {
        state.Time = 0.0f;
      }
      else if (!state.Looping && state.Time > duration)
      {
        state.Time = duration; // Clamp to end
      }

      animation.State = state;
      _world.Animations.GetByIndex(i) = animation;
    }
  }
}

