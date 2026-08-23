using Catalyst.Enemies;
using Catalyst.Run;
using Godot;

namespace Catalyst.Spatial;

public partial class SpatialGrid : Node
{
    [Export]
    public Vector2 BoundsMin { get; set; } = new(-40.0f, -40.0f);

    [Export]
    public Vector2 BoundsMax { get; set; } = new(40.0f, 40.0f);

    [Export(PropertyHint.Range, "0.5,10,0.25")]
    public float CellSize { get; set; } = 2.5f;

    private List<EntityHandle>[] _buckets = Array.Empty<List<EntityHandle>>();
    private int _width;
    private int _height;
    private EnemySystem _enemies = null!;
    private RunController _run = null!;

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _run = GetNode<RunController>("../RunController");
        _width = Math.Max(1, Mathf.CeilToInt((BoundsMax.X - BoundsMin.X) / CellSize));
        _height = Math.Max(1, Mathf.CeilToInt((BoundsMax.Y - BoundsMin.Y) / CellSize));
        _buckets = new List<EntityHandle>[_width * _height];
        for (int index = 0; index < _buckets.Length; index++)
        {
            _buckets[index] = new List<EntityHandle>(16);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_run.State == RunState.Playing)
        {
            Rebuild();
        }
    }

    public void Rebuild()
    {
        foreach (List<EntityHandle> bucket in _buckets)
        {
            bucket.Clear();
        }

        for (int denseIndex = 0; denseIndex < _enemies.ActiveCount; denseIndex++)
        {
            Vector2 position = _enemies.GetPositionAtDenseIndex(denseIndex);
            int bucketIndex = GetBucketIndex(position);
            if (bucketIndex >= 0)
            {
                _buckets[bucketIndex].Add(_enemies.GetHandleAtDenseIndex(denseIndex));
            }
        }
    }

    public bool TryFindNearest(Vector2 center, float radius, out EntityHandle nearest)
    {
        float bestDistanceSquared = radius * radius;
        nearest = EntityHandle.Invalid;
        GetCellRange(center, radius, out int minX, out int maxX, out int minY, out int maxY);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                List<EntityHandle> bucket = _buckets[y * _width + x];
                foreach (EntityHandle handle in bucket)
                {
                    if (!_enemies.TryGet(handle, out EnemyState enemy))
                    {
                        continue;
                    }

                    float distanceSquared = center.DistanceSquaredTo(enemy.Position);
                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        nearest = handle;
                    }
                }
            }
        }

        return nearest.IsValid;
    }

    public void QueryCircle(Vector2 center, float radius, List<EntityHandle> results)
    {
        results.Clear();
        float radiusSquared = radius * radius;
        GetCellRange(center, radius, out int minX, out int maxX, out int minY, out int maxY);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                List<EntityHandle> bucket = _buckets[y * _width + x];
                foreach (EntityHandle handle in bucket)
                {
                    if (_enemies.TryGet(handle, out EnemyState enemy) &&
                        center.DistanceSquaredTo(enemy.Position) <= radiusSquared)
                    {
                        results.Add(handle);
                    }
                }
            }
        }
    }

    private int GetBucketIndex(Vector2 position)
    {
        int x = Mathf.FloorToInt((position.X - BoundsMin.X) / CellSize);
        int y = Mathf.FloorToInt((position.Y - BoundsMin.Y) / CellSize);
        return x < 0 || x >= _width || y < 0 || y >= _height
            ? -1
            : y * _width + x;
    }

    private void GetCellRange(
        Vector2 center,
        float radius,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        minX = Mathf.Clamp(Mathf.FloorToInt((center.X - radius - BoundsMin.X) / CellSize), 0, _width - 1);
        maxX = Mathf.Clamp(Mathf.FloorToInt((center.X + radius - BoundsMin.X) / CellSize), 0, _width - 1);
        minY = Mathf.Clamp(Mathf.FloorToInt((center.Y - radius - BoundsMin.Y) / CellSize), 0, _height - 1);
        maxY = Mathf.Clamp(Mathf.FloorToInt((center.Y + radius - BoundsMin.Y) / CellSize), 0, _height - 1);
    }
}
