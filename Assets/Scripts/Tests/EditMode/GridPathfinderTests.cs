using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class GridPathfinderTests
{
    private sealed class FakeGrid : IWalkableGrid
    {
        private readonly HashSet<Vector2Int> _blocked = new();
        public int Min = -20, Max = 20;

        public void Block(int x, int y) => _blocked.Add(new Vector2Int(x, y));

        public void BlockColumn(int x, int fromY, int toY)
        {
            for (int y = fromY; y <= toY; y++)
                Block(x, y);
        }

        public bool IsWalkable(Vector2Int cell)
            => cell.x >= Min && cell.x <= Max && cell.y >= Min && cell.y <= Max && !_blocked.Contains(cell);
    }

    private readonly List<Vector2Int> _path = new();

    [Test]
    public void StraightLine_FindsDirectPath()
    {
        FakeGrid grid = new();
        GridPathfinder.Result result = GridPathfinder.FindPath(grid, Vector2Int.zero, new Vector2Int(5, 0), 1000, _path, out _);
        Assert.AreEqual(GridPathfinder.Result.Found, result);
        Assert.AreEqual(6, _path.Count);
        Assert.AreEqual(new Vector2Int(5, 0), _path[^1]);
    }

    [Test]
    public void Wall_PathGoesAround()
    {
        FakeGrid grid = new();
        grid.BlockColumn(2, -3, 3);
        GridPathfinder.Result result = GridPathfinder.FindPath(grid, Vector2Int.zero, new Vector2Int(4, 0), 1000, _path, out _);
        Assert.AreEqual(GridPathfinder.Result.Found, result);
        foreach (Vector2Int cell in _path)
            Assert.IsTrue(grid.IsWalkable(cell) || cell == Vector2Int.zero, $"path crosses blocked {cell}");
        Assert.IsTrue(_path.Exists(c => Mathf.Abs(c.y) >= 4), "path should detour around the wall end");
    }

    [Test]
    public void DiagonalNeverCutsBlockedCorner()
    {
        FakeGrid grid = new();
        grid.Block(1, 0);
        GridPathfinder.FindPath(grid, Vector2Int.zero, new Vector2Int(1, 1), 1000, _path, out _);
        for (int i = 1; i < _path.Count; i++)
        {
            Vector2Int a = _path[i - 1], b = _path[i];
            if (a.x != b.x && a.y != b.y)
            {
                Assert.IsTrue(grid.IsWalkable(new Vector2Int(b.x, a.y)));
                Assert.IsTrue(grid.IsWalkable(new Vector2Int(a.x, b.y)));
            }
        }
    }

    [Test]
    public void EnclosedGoal_ReturnsNoPath()
    {
        FakeGrid grid = new();
        for (int x = 4; x <= 6; x++)
            for (int y = -1; y <= 1; y++)
                if (x != 5 || y != 0)
                    grid.Block(x, y);
        GridPathfinder.Result result = GridPathfinder.FindPath(grid, Vector2Int.zero, new Vector2Int(5, 0), 5000, _path, out _);
        Assert.AreEqual(GridPathfinder.Result.NoPath, result);
    }

    [Test]
    public void BlockedGoal_NoPath_ButNearestWalkableResolves()
    {
        FakeGrid grid = new();
        grid.Block(5, 0);
        Assert.AreEqual(GridPathfinder.Result.NoPath,
            GridPathfinder.FindPath(grid, Vector2Int.zero, new Vector2Int(5, 0), 1000, _path, out _));
        Assert.IsTrue(GridPathfinder.TryFindNearestWalkable(grid, new Vector2Int(5, 0), 2, out Vector2Int snapped));
        Assert.IsTrue(grid.IsWalkable(snapped));
        Assert.LessOrEqual((snapped - new Vector2Int(5, 0)).sqrMagnitude, 1);
    }

    [Test]
    public void NodeBudget_ReturnsPartialTowardGoal()
    {
        FakeGrid grid = new() { Min = -200, Max = 200 };
        grid.BlockColumn(10, -150, 150);
        GridPathfinder.Result result = GridPathfinder.FindPath(grid, Vector2Int.zero, new Vector2Int(20, 0), 60, _path, out int expanded);
        Assert.AreEqual(GridPathfinder.Result.Partial, result);
        Assert.LessOrEqual(expanded, 60);
        Assert.Greater(_path[^1].x, 0);
    }

    [Test]
    public void Smooth_RemovesRedundantWaypointsWithoutCuttingWalls()
    {
        FakeGrid grid = new();
        grid.BlockColumn(2, -3, 3);
        GridPathfinder.FindPath(grid, Vector2Int.zero, new Vector2Int(4, 0), 1000, _path, out _);
        List<Vector2Int> smooth = new();
        GridPathfinder.Smooth(grid, _path, smooth);
        Assert.Less(smooth.Count, _path.Count);
        Assert.AreEqual(new Vector2Int(4, 0), smooth[^1]);
        Vector2Int previous = Vector2Int.zero;
        foreach (Vector2Int cell in smooth)
        {
            Assert.IsTrue(GridPathfinder.HasLineOfSight(grid, previous, cell), $"{previous}->{cell} crosses wall");
            previous = cell;
        }
    }
}
