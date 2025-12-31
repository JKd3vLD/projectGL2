using System;

namespace GL2Engine.ECS;

/// <summary>
/// SoA (Structure of Arrays) component storage for a single component type.
/// Dense packing with no gaps for cache efficiency.
/// </summary>
public class ComponentStore<T> where T : struct
{
  private T[] _components;
  private int[] _entityToIndex;
  private int[] _indexToEntity;
  private int _count;
  private int _capacity;

  public ComponentStore(int initialCapacity = 64)
  {
    _capacity = initialCapacity;
    _components = new T[_capacity];
    _entityToIndex = new int[_capacity];
    _indexToEntity = new int[_capacity];
    _count = 0;

    // Initialize mappings to -1 (invalid)
    for (int i = 0; i < _capacity; i++)
    {
      _entityToIndex[i] = -1;
      _indexToEntity[i] = -1;
    }
  }

  public int Count => _count;

  public bool Has(Entity entity)
  {
    if (entity.Id < 0 || entity.Id >= _entityToIndex.Length)
      return false;
    return _entityToIndex[entity.Id] >= 0;
  }

  public ref T Get(Entity entity)
  {
    int index = _entityToIndex[entity.Id];
    if (index < 0)
      throw new InvalidOperationException($"Entity {entity.Id} does not have component {typeof(T).Name}");
    return ref _components[index];
  }

  public ref T Add(Entity entity, T component)
  {
    if (entity.Id < 0)
      throw new ArgumentException("Invalid entity ID");

    // Grow arrays if needed
    if (entity.Id >= _entityToIndex.Length)
    {
      int newSize = Math.Max(entity.Id + 1, _capacity * 2);
      Array.Resize(ref _entityToIndex, newSize);
      for (int i = _entityToIndex.Length; i < newSize; i++)
        _entityToIndex[i] = -1;
    }

    if (_entityToIndex[entity.Id] >= 0)
      throw new InvalidOperationException($"Entity {entity.Id} already has component {typeof(T).Name}");

    // Grow component array if needed
    if (_count >= _capacity)
    {
      _capacity *= 2;
      Array.Resize(ref _components, _capacity);
      Array.Resize(ref _indexToEntity, _capacity);
    }

    int newIndex = _count++;
    _components[newIndex] = component;
    _entityToIndex[entity.Id] = newIndex;
    _indexToEntity[newIndex] = entity.Id;

    return ref _components[newIndex];
  }

  public void Remove(Entity entity)
  {
    if (entity.Id < 0 || entity.Id >= _entityToIndex.Length)
      return;

    int index = _entityToIndex[entity.Id];
    if (index < 0)
      return;

    // Swap with last element for dense packing
    int lastIndex = _count - 1;
    if (index != lastIndex)
    {
      _components[index] = _components[lastIndex];
      int lastEntityId = _indexToEntity[lastIndex];
      _entityToIndex[lastEntityId] = index;
      _indexToEntity[index] = lastEntityId;
    }

    _entityToIndex[entity.Id] = -1;
    _indexToEntity[lastIndex] = -1;
    _count--;
  }

  public Entity GetEntity(int index)
  {
    if (index < 0 || index >= _count)
      return Entity.Invalid;
    return new Entity(_indexToEntity[index], 0); // Gen not stored here
  }

  public ref T GetByIndex(int index)
  {
    if (index < 0 || index >= _count)
      throw new IndexOutOfRangeException();
    return ref _components[index];
  }

  /// <summary>
  /// Returns an enumerable of all entities that have this component.
  /// Efficient iteration over active entities.
  /// </summary>
  public System.Collections.Generic.IEnumerable<Entity> GetActiveEntities()
  {
    for (int i = 0; i < _count; i++)
    {
      yield return new Entity(_indexToEntity[i], 0); // Gen not stored here
    }
  }
}
