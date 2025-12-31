namespace GL2Engine.ECS;

public struct Entity
{
  public int Id;
  public int Gen;

  public Entity(int id, int gen)
  {
    Id = id;
    Gen = gen;
  }

  public bool IsValid => Id >= 0 && Gen > 0;
  public static Entity Invalid => new Entity(-1, 0);
}
