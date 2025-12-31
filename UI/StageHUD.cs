using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GL2Engine.World;

namespace GL2Engine.UI;

/// <summary>
/// In-stage HUD showing pacing-specific information (timer for FAST, exploration checklist for SLOW).
/// </summary>
public class StageHUD
{
  private SpriteFont? _font;
  private PacingTag _currentPacing;
  
  // FAST mode metrics
  private float _stageTimer;
  private float _idleTime;
  private float _averageSpeed;
  
  // SLOW mode metrics
  private int _secretsFound;
  private int _bonusDoorsCompleted;
  private int _questItemsCollected;
  private int _carryObjectiveProgress;

  public StageHUD()
  {
    _currentPacing = PacingTag.FAST;
    Reset();
  }

  public void SetPacing(PacingTag pacing)
  {
    _currentPacing = pacing;
    Reset();
  }

  public void Reset()
  {
    _stageTimer = 0f;
    _idleTime = 0f;
    _averageSpeed = 0f;
    _secretsFound = 0;
    _bonusDoorsCompleted = 0;
    _questItemsCollected = 0;
    _carryObjectiveProgress = 0;
  }

  /// <summary>
  /// Updates FAST mode metrics.
  /// </summary>
  public void UpdateFastMetrics(float dt, float currentSpeed, bool isMoving)
  {
    _stageTimer += dt;
    
    if (!isMoving)
    {
      _idleTime += dt;
    }
    
    // Update average speed (simple moving average)
    _averageSpeed = _averageSpeed * 0.9f + currentSpeed * 0.1f;
  }

  /// <summary>
  /// Updates SLOW mode metrics.
  /// </summary>
  public void UpdateSlowMetrics(int secretsFound, int bonusDoorsCompleted, int questItemsCollected, int carryProgress)
  {
    _secretsFound = secretsFound;
    _bonusDoorsCompleted = bonusDoorsCompleted;
    _questItemsCollected = questItemsCollected;
    _carryObjectiveProgress = carryProgress;
  }

  /// <summary>
  /// Draws the HUD based on current pacing mode.
  /// </summary>
  public void Draw(SpriteBatch spriteBatch, GraphicsDevice device)
  {
    if (_font == null)
      return;

    spriteBatch.Begin();

    if (_currentPacing == PacingTag.FAST)
    {
      DrawFastHUD(spriteBatch, device);
    }
    else
    {
      DrawSlowHUD(spriteBatch, device);
    }

    spriteBatch.End();
  }

  private void DrawFastHUD(SpriteBatch spriteBatch, GraphicsDevice device)
  {
    // Timer display (not a time limit, just shows bonus potential)
    string timerText = $"Time: {_stageTimer:F1}s";
    spriteBatch.DrawString(_font, timerText, new Vector2(10, 10), Color.White);

    // Speed rank bar (optional)
    float speedPercent = MathHelper.Clamp(_averageSpeed / 200f, 0f, 1f); // Normalize to 0-1
    var speedBarRect = new Rectangle(10, 30, 200, 20);
    // TODO: Draw speed bar background and fill

    // Flow meter (idle time tracking)
    string flowText = $"Flow: {(_idleTime < 2.0f ? "Good" : "Broken")}";
    spriteBatch.DrawString(_font, flowText, new Vector2(10, 60), _idleTime < 2.0f ? Color.Green : Color.Red);
  }

  private void DrawSlowHUD(SpriteBatch spriteBatch, GraphicsDevice device)
  {
    // Exploration checklist
    spriteBatch.DrawString(_font, "Exploration:", new Vector2(10, 10), Color.White);
    spriteBatch.DrawString(_font, $"  Secrets: {_secretsFound}", new Vector2(10, 30), Color.Cyan);
    spriteBatch.DrawString(_font, $"  Bonus Doors: {_bonusDoorsCompleted}", new Vector2(10, 50), Color.Yellow);
    
    // Quest items
    spriteBatch.DrawString(_font, $"Quest Items: {_questItemsCollected}", new Vector2(10, 80), Color.Green);
    
    // Carry objective progress
    if (_carryObjectiveProgress > 0)
    {
      spriteBatch.DrawString(_font, $"Carry Progress: {_carryObjectiveProgress}%", new Vector2(10, 110), Color.Orange);
    }
  }

  public void SetFont(SpriteFont font)
  {
    _font = font;
  }

  // Getters for reward calculation
  public float GetStageTimer() => _stageTimer;
  public float GetIdleTime() => _idleTime;
  public float GetAverageSpeed() => _averageSpeed;
  public int GetSecretsFound() => _secretsFound;
  public int GetBonusDoorsCompleted() => _bonusDoorsCompleted;
  public int GetQuestItemsCollected() => _questItemsCollected;
}

