namespace GL2Engine.Render;

/// <summary>
/// Animation clip definition with duration and frame count.
/// </summary>
public struct AnimationClip
{
  public string Name;
  public float Duration;      // Duration in seconds
  public int FrameCount;       // Number of frames
  public bool Looping;         // Whether clip loops
}

/// <summary>
/// Registry for animation clips.
/// </summary>
public static class AnimationClipRegistry
{
  private static Dictionary<int, AnimationClip> _clips = new();

  public static void Register(int clipId, AnimationClip clip)
  {
    _clips[clipId] = clip;
  }

  public static AnimationClip? GetClip(int clipId)
  {
    return _clips.TryGetValue(clipId, out var clip) ? clip : null;
  }

  public static float GetDuration(int clipId)
  {
    var clip = GetClip(clipId);
    return clip?.Duration ?? 1.0f; // Default 1 second
  }
}

