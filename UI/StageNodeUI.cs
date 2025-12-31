using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GL2Engine.World;

namespace GL2Engine.UI;

/// <summary>
/// UI for displaying stage nodes on the world map with pacing icons and reward previews.
/// </summary>
public class StageNodeUI
{
  private SpriteFont? _font;

  public StageNodeUI()
  {
  }

  /// <summary>
  /// Draws a stage node with pacing icon, reward preview, and difficulty stars.
  /// </summary>
  public void DrawStageNode(SpriteBatch spriteBatch, Stage stage, Vector2 position, bool isSelected)
  {
    if (_font == null)
      return;

    spriteBatch.Begin();

    // Draw node background
    var nodeRect = new Rectangle((int)position.X - 50, (int)position.Y - 50, 100, 100);
    var bgColor = isSelected ? Color.Yellow : Color.White;
    // TODO: Draw actual background texture/rectangle

    // Draw pacing icon
    string pacingIcon = stage.PacingTag == PacingTag.FAST ? "⏱" : "🗺"; // Clock or compass
    spriteBatch.DrawString(_font, pacingIcon, new Vector2(position.X - 40, position.Y - 40), Color.White);

    // Draw reward preview
    string rewardText = stage.RewardProfile switch
    {
      RewardProfile.SPEED => "Speed",
      RewardProfile.TREASURE => "Treasure",
      RewardProfile.QUEST => "Quest",
      RewardProfile.MIXED => "Mixed",
      _ => ""
    };
    spriteBatch.DrawString(_font, rewardText, new Vector2(position.X - 30, position.Y - 20), Color.Cyan);

    // Draw difficulty stars (placeholder)
    string stars = "★".PadRight(5, '☆').Substring(0, 5); // TODO: Use actual difficulty
    spriteBatch.DrawString(_font, stars, new Vector2(position.X - 30, position.Y), Color.Yellow);

    // Draw stage name
    spriteBatch.DrawString(_font, stage.Name, new Vector2(position.X - 30, position.Y + 20), Color.White);

    spriteBatch.End();
  }

  public void SetFont(SpriteFont font)
  {
    _font = font;
  }
}

