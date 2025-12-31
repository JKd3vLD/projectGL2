using System;

namespace GL2Engine.Engine;

/// <summary>
/// Game loop management with configurable framerate modes.
/// </summary>
public static class GameLoop
{
  public static TimeSpan GetTargetElapsedTime(GameConfig config)
  {
    return config.FramerateMode switch
    {
      FramerateMode.Modern120Hz => TimeSpan.FromSeconds(1.0 / 120.0),
      FramerateMode.SNES30Hz => TimeSpan.FromSeconds(1.0 / 30.0),
      FramerateMode.Adaptive => TimeSpan.FromSeconds(1.0 / 60.0), // Default to 60Hz for adaptive
      _ => TimeSpan.FromSeconds(1.0 / 120.0)
    };
  }

  public static bool ShouldRender(GameConfig config, int frameCount)
  {
    if (!config.FrameSkipEnabled)
      return true;

    // In SNES mode with frame skip: render every 4th frame (30Hz update, 120Hz render = skip 3 frames)
    if (config.FramerateMode == FramerateMode.SNES30Hz)
      return frameCount % 4 == 0;

    return true;
  }
}

