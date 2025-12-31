namespace GL2Engine.Engine;

public enum FramerateMode
{
  Modern120Hz,
  SNES30Hz,
  Adaptive
}

public struct GameConfig
{
  public FramerateMode FramerateMode;
  public bool FrameSkipEnabled; // For SNES mode: skip rendering frames, update at 30Hz

  public static GameConfig Default => new GameConfig
  {
    FramerateMode = FramerateMode.Modern120Hz,
    FrameSkipEnabled = false
  };
}

