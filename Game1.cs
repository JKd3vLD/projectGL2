using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GL2Engine.Engine;
using GL2Engine.ECS;
using GL2Engine.Render;
using GL2Engine.Physics2D;
using GL2Engine.Gameplay;
using GL2Engine.Debug;

namespace GL2Engine;

public class Game1 : Game
{
  private GraphicsDeviceManager _graphics;
  private SpriteBatch _spriteBatch;
  
  private GameWorld _world;
  private RenderPipeline _renderPipeline;
  private DebugOverlay _debugOverlay;
  private GameConfig _config;
  private int _frameCount = 0;
  
  public Game1()
  {
    _graphics = new GraphicsDeviceManager(this);
    Content.RootDirectory = "Content";
    IsMouseVisible = true; // For debug now
    
    _config = GameConfig.Default;
    TargetElapsedTime = GameLoop.GetTargetElapsedTime(_config);
    IsFixedTimeStep = true;
  }

  protected override void Initialize()
  {
    base.Initialize();
    
    _graphics.PreferredBackBufferWidth = 1280;
    _graphics.PreferredBackBufferHeight = 720;
    _graphics.ApplyChanges();
    
    _world = new GameWorld();
    _renderPipeline = new RenderPipeline(GraphicsDevice, 320, 240); // Virtual resolution
    _world.InitializeRender(GraphicsDevice);
    _world.GetCameraSystem()?.SetCamera(_renderPipeline.Camera);
    _debugOverlay = new DebugOverlay(GraphicsDevice, Content);
  }

  protected override void LoadContent()
  {
    _spriteBatch = new SpriteBatch(GraphicsDevice);
    
    _world.LoadTestbedLevel();
  }

  protected override void Update(GameTime gameTime)
  {
    // Toggle framerate mode with F1 (debug)
    var keyboard = Keyboard.GetState();
    if (keyboard.IsKeyDown(Keys.F1) && !_wasF1Pressed)
    {
      _config.FramerateMode = _config.FramerateMode switch
      {
        FramerateMode.Modern120Hz => FramerateMode.SNES30Hz,
        FramerateMode.SNES30Hz => FramerateMode.Modern120Hz,
        _ => FramerateMode.Modern120Hz
      };
      TargetElapsedTime = GameLoop.GetTargetElapsedTime(_config);
    }
    _wasF1Pressed = keyboard.IsKeyDown(Keys.F1);

    _world.UpdateFixed((float)TargetElapsedTime.TotalSeconds);
    
    base.Update(gameTime);
  }

  private bool _wasF1Pressed = false;

  protected override void Draw(GameTime gameTime)
  {
    _frameCount++;

    // Frame skip for SNES mode
    if (!GameLoop.ShouldRender(_config, _frameCount))
    {
      base.Draw(gameTime);
      return;
    }

    // Render to low-res target
    _renderPipeline.BeginRender(GraphicsDevice);
    _world.Draw(_renderPipeline);
    _renderPipeline.EndRender();
    
    // Present with upscale
    GraphicsDevice.Clear(Color.Black);
    _renderPipeline.Present(GraphicsDevice, _spriteBatch);
    
    // Debug overlay
    _debugOverlay.Draw(_spriteBatch, _world, gameTime);
    
    base.Draw(gameTime);
  }
}
