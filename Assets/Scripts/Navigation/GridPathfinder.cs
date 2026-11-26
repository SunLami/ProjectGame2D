using System.Collections.Generic;
using UnityEngine;

/// <summary>Walkability source for <see cref="GridPathfinder"/>. Implemented by the scene grid
/// (physics-backed) and by test fakes.</summary>
public interface IWalkableGrid
{
    bool IsWalkable(Vector2Int cell);
}

/// <summary>Pure 8-direction A* over an <see cref="IWalkableGrid"/> (D-109). No MonoBehaviour
/// dependency so it can be EditMode-tested. Diagonal moves never cut a blocked corner.
/// Single-threaded: internal buffers are reused between calls.</summary>
public static class GridPathfinder
{
    public enum Result
    {
        /// <summary>Full path from start to goal.</summary>
        Found,
        /// <summary>Node budget ran out; path leads to the explored cell closest to the goal.</summary>
        Partial,
        /// <summary>Goal is unreachable (search space exhausted) or start == blocked-in.</summary>
        NoPath,
    }

    private const float Diagonal = 1.41421356f;

    private static readonly Vector2Int[] Neighbours =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
    };

    private static readonly Dictionary<Vector2Int, float> GScore = new();
    private static readonly Dictionary<Vector2Int, Vector2Int> CameFrom = new();
    private static readonly HashSet<Vector2Int> Closed = new();
    private static readonly List<HeapNode> Open = new();

    private struct HeapNode
    {
        public Vector2Int Cell;
        public float F;
        public float H;
    }

    /// <summary>Finds a cell path from <paramref name="start"/> to <paramref name="goal"/>. The start
    /// cell is always treated as walkable (an actor may overlap an obstacle edge). The goal must be
    /// walkable; resolve it with <see cref="TryFindNearestWalkable"/> first.</summary>
    /// <param name="path">Cleared and filled with cells from start (inclusive) to the end cell.</param>
    /// <param name="expanded">Number of nodes expanded, for frame budgeting.</param>
    public static Result FindPath(IWalkableGrid grid, Vector2Int start, Vector2Int goal, int maxNodes,
        List<Vector2Int> path, out int expanded)
    {
        path.Clear();
        expanded = 0;
        if (grid == null)
            return Result.NoPath;

        if (start == goal)
        {
            path.Add(start);
            return Result.Found;
        }

        if (!grid.IsWalkable(goal))
            return Result.NoPath;

        GScore.Clear();
        CameFrom.Clear();
        Closed.Clear();
        Open.Clear();

        GScore[start] = 0f;
        float startH = Heuristic(start, goal);
        Push(new HeapNode { Cell = start, F = startH, H = startH });
        Vector2Int best = start;
        float bestH = startH;
        maxNodes = Mathf.Max(1, maxNodes);

        while (Open.Count > 0)
        {
            HeapNode node = Pop();
            if (!Closed.Add(node.Cell))
                continue;

            if (node.Cell == goal)
            {
                Reconstruct(goal, path);
                return Result.Found;
            }

            if (node.H < bestH)
            {
                bestH = node.H;
                best = node.Cell;
            }

            if (++expanded >= maxNodes)
            {
                Reconstruct(best, path);
                return best == start ? Result.NoPath : Result.Partial;
            }

            float g = GScore[node.Cell];
            for (int i = 0; i < Neighbours.Length; i++)
            {
                Vector2Int step = Neighbours[i];
                Vector2Int next = node.Cell + step;
                if (Closed.Contains(next) || !grid.IsWalkable(next))
                    continue;

                bool diagonal = step.x != 0 && step.y != 0;
                if (diagonal
                    && (!grid.IsWalkable(new Vector2Int(node.Cell.x + step.x, node.Cell.y))
                        || !grid.IsWalkable(new Vector2Int(node.Cell.x, node.Cell.y + step.y))))
                    continue;

                float tentative = g + (diagonal ? Diagonal : 1f);
                if (GScore.TryGetValue(next, out float existing) && tentative >= existing)
                    continue;

                GScore[next] = tentative;
                CameFrom[next] = node.Cell;
                float h = Heuristic(next, goal);
                Push(new HeapNode { Cell = next, F = tentative + h, H = h });
            }
        }

        return Result.NoPath;
    }

    /// <summary>Spiral search for the walkable cell nearest to <paramref name="cell"/>.</summary>
    public static bool TryFindNearestWalkable(IWalkableGrid grid, Vector2Int cell, int maxRadius,
        out Vector2Int result)
    {
        result = cell;
        if (grid == null)
            return false;
        if (grid.IsWalkable(cell))
            return true;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            float bestDistance = float.MaxValue;
            bool found = false;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    Vector2Int candidate = new(cell.x + x, cell.y + y);
                    float distance = x * x + y * y;
                    if (distance < bestDistance && grid.IsWalkable(candidate))
                    {
                        bestDistance = distance;
                        result = candidate;
                        found = true;
                    }
                }
            }

            if (found)
                return true;
        }

        return false;
    }

    /// <summary>Conservative line of sight between two cell centres: every cell touched by a line
    /// slightly thicker than zero must be walkable, so smoothed paths never clip corners.</summary>
    public static bool HasLineOfSight(IWalkableGrid grid, Vector2Int from, Vector2Int to)
    {
        Vector2 a = new(from.x + 0.5f, from.y + 0.5f);
        Vector2 b = new(to.x + 0.5f, to.y + 0.5f);
        Vector2 delta = b - a;
        float length = delta.magnitude;
        if (length < 0.0001f)
            return grid.IsWalkable(from);

        Vector2 direction = delta / length;
        Vector2 side = new Vector2(-direction.y, direction.x) * 0.45f;
        int steps = Mathf.CeilToInt(length / 0.25f);
        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = a + delta * (i / (float)steps);
            if (!IsWalkablePoint(grid, point) || !IsWalkablePoint(grid, point + side)
                || !IsWalkablePoint(grid, point - side))
                return false;
        }

        return true;
    }

    /// <summary>String-pulls a cell path: keeps only the cells needed so consecutive kept cells have
    /// line of sight. The first cell (start) is dropped from the output.</summary>
    public static void Smooth(IWalkableGrid grid, List<Vector2Int> cells, List<Vector2Int> output)
    {
        output.Clear();
        if (cells.Count == 0)
            return;
        if (cells.Count == 1)
        {
            output.Add(cells[0]);
            return;
        }

        int anchor = 0;
        int index = 1;
        while (index < cells.Count)
        {
            int furthest = index;
            for (int probe = cells.Count - 1; probe > index; probe--)
            {
                if (HasLineOfSight(grid, cells[anchor], cells[probe]))
                {
                    furthest = probe;
                    break;
                }
            }

            output.Add(cells[furthest]);
            anchor = furthest;
            index = furthest + 1;
        }
    }

    private static bool IsWalkablePoint(IWalkableGrid grid, Vector2 point)
        => grid.IsWalkable(new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y)));

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx + dy + (Diagonal - 2f) * Mathf.Min(dx, dy);
    }

    private static void Reconstruct(Vector2Int end, List<Vector2Int> path)
    {
        path.Clear();
        Vector2Int current = end;
        path.Add(current);
        while (CameFrom.TryGetValue(current, out Vector2Int previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
    }

    private static void Push(HeapNode node)
    {
        Open.Add(node);
        int i = Open.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (!Less(Open[i], Open[parent]))
                break;
            (Open[i], Open[parent]) = (Open[parent], Open[i]);
            i = parent;
        }
    }

    private static HeapNode Pop()
    {
        HeapNode top = Open[0];
        int last = Open.Count - 1;
        Open[0] = Open[last];
        Open.RemoveAt(last);
        int i = 0;
        while (true)
        {
            int left = i * 2 + 1;
            if (left >= Open.Count)
                break;
            int right = left + 1;
            int smallest = right < Open.Count && Less(Open[right], Open[left]) ? right : left;
            if (!Less(Open[smallest], Open[i]))
                break;
            (Open[i], Open[smallest]) = (Open[smallest], Open[i]);
            i = smallest;
        }

        return top;
    }

    private static bool Less(HeapNode a, HeapNode b) => a.F < b.F || (Mathf.Approximately(a.F, b.F) && a.H < b.H);
}
