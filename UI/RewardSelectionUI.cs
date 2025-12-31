using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GL2Engine.Inventory;
using GL2Engine.Engine;

namespace GL2Engine.UI;

/// <summary>
/// Hades-style reward selection UI triggered when XP threshold is reached.
/// Shows 3 reward options from LootTable.
/// </summary>
public class RewardSelectionUI
{
  private bool _isVisible;
  private List<ItemDrop> _rewardOptions;
  private int _selectedIndex;
  private SpriteFont? _font;

  public RewardSelectionUI()
  {
    _isVisible = false;
    _rewardOptions = new List<ItemDrop>();
    _selectedIndex = 0;
  }

  /// <summary>
  /// Shows reward selection with 3 options from loot table.
  /// </summary>
  public void ShowRewardSelection(LootTable lootTable, Rng rng)
  {
    _rewardOptions.Clear();
    
    // Roll 3 rewards from loot table
    var rewards = lootTable.RollLoot(rng, 3);
    _rewardOptions = rewards;

    _isVisible = true;
    _selectedIndex = 0;
  }

  /// <summary>
  /// Handles input and selection.
  /// </summary>
  public void Update(InputState input)
  {
    if (!_isVisible)
      return;

    // TODO: Handle input (arrow keys to select, Enter to confirm)
    // For now, just a placeholder
  }

  /// <summary>
  /// Confirms selection and returns the selected reward.
  /// </summary>
  public ItemDrop? ConfirmSelection()
  {
    if (!_isVisible || _rewardOptions.Count == 0)
      return null;

    if (_selectedIndex >= 0 && _selectedIndex < _rewardOptions.Count)
    {
      var selected = _rewardOptions[_selectedIndex];
      _isVisible = false;
      return selected;
    }

    return null;
  }

  /// <summary>
  /// Draws the reward selection UI.
  /// </summary>
  public void Draw(SpriteBatch spriteBatch, GraphicsDevice device)
  {
    if (!_isVisible || _font == null)
      return;

    spriteBatch.Begin();

    // Draw background
    var bgRect = new Rectangle(device.Viewport.Width / 2 - 200, device.Viewport.Height / 2 - 150, 400, 300);
    spriteBatch.DrawString(_font, "Select Reward", new Vector2(bgRect.X + 10, bgRect.Y + 10), Color.White);

    // Draw reward options
    for (int i = 0; i < _rewardOptions.Count; i++)
    {
      var option = _rewardOptions[i];
      var yPos = bgRect.Y + 50 + i * 60;
      var color = (i == _selectedIndex) ? Color.Yellow : Color.White;
      
      spriteBatch.DrawString(_font, $"{i + 1}. {option.ItemId} x{option.Count}", 
        new Vector2(bgRect.X + 20, yPos), color);
    }

    spriteBatch.DrawString(_font, "Press Enter to select", 
      new Vector2(bgRect.X + 10, bgRect.Bottom - 30), Color.Gray);

    spriteBatch.End();
  }

  public void SetFont(SpriteFont font)
  {
    _font = font;
  }

  public bool IsVisible => _isVisible;
}

/// <summary>
/// Input state for UI (placeholder).
/// </summary>
public struct InputState
{
  public bool LeftPressed;
  public bool RightPressed;
  public bool UpPressed;
  public bool DownPressed;
  public bool ConfirmPressed;
}

