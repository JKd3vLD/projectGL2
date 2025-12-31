using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GL2Engine.Render;

/// <summary>
/// Rendering pipeline: low-res RenderTarget + nearest-neighbor upscale + ortho camera + pixel snap
/// </summary>
public class RenderPipeline
{
  private RenderTarget2D _lowResTarget;
  private int _virtualWidth;
  private int _virtualHeight;
  private Camera _camera;

  public RenderPipeline(GraphicsDevice device, int virtualWidth, int virtualHeight)
  {
    _virtualWidth = virtualWidth;
    _virtualHeight = virtualHeight;
    _lowResTarget = new RenderTarget2D(device, virtualWidth, virtualHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
    _camera = new Camera(virtualWidth, virtualHeight);
  }

  public Camera Camera => _camera;

  public void BeginRender(GraphicsDevice device)
  {
    // Set render target to low-res buffer
    device.SetRenderTarget(_lowResTarget);
    device.Clear(Color.CornflowerBlue);
    
    // Set depth stencil state for Z sorting
    device.DepthStencilState = DepthStencilState.Default;
  }

  public void EndRender()
  {
    // Render target will be reset in Present()
  }

  public void Present(GraphicsDevice device, SpriteBatch spriteBatch)
  {
    // Reset render target to backbuffer
    device.SetRenderTarget(null);
    device.Clear(Color.Black);

    // Upscale low-res target to backbuffer using nearest-neighbor
    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, null, null);
    var backbufferWidth = device.PresentationParameters.BackBufferWidth;
    var backbufferHeight = device.PresentationParameters.BackBufferHeight;
    var destRect = new Rectangle(0, 0, backbufferWidth, backbufferHeight);
    spriteBatch.Draw(_lowResTarget, destRect, Color.White);
    spriteBatch.End();
  }

  public RenderTarget2D LowResTarget => _lowResTarget;
}
