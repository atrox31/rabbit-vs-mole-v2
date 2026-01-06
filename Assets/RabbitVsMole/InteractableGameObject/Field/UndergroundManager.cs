using RabbitVsMole.InteractableGameObject.Field.Base;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Grid lookup helper for UndergroundFieldBase (mirrors FarmManager).
/// Provides stable X/Y indices for network sync and neighbor queries.
/// </summary>
public static class UndergroundManager
{
    // Configuration (same approach as FarmManager)
    private static float threshold = 0.5f;
    private static List<UndergroundFieldBase> undergroundFieldList = new List<UndergroundFieldBase>();

    // Caching
    private static UndergroundFieldBase[,] _grid = null;
    private static Dictionary<UndergroundFieldBase, Vector2Int> _posLookup = null;

    public static int Width { get; private set; }
    public static int Height { get; private set; }

    private static void EnsureInitialized()
    {
        if (_grid != null) return;

        _grid = SortObjectsIntoGrid(undergroundFieldList, out int w, out int h);
        Width = w;
        Height = h;

        _posLookup = new Dictionary<UndergroundFieldBase, Vector2Int>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_grid[x, y] != null)
                    _posLookup[_grid[x, y]] = new Vector2Int(x, y);
            }
        }
    }

    public static UndergroundFieldBase GetUndergroundField(int x, int y)
    {
        EnsureInitialized();
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return null;
        return _grid[x, y];
    }

    public static Vector2Int? GetFieldXY(UndergroundFieldBase field)
    {
        EnsureInitialized();
        if (field != null && _posLookup.TryGetValue(field, out Vector2Int pos))
            return pos;
        return null;
    }

    public static UndergroundFieldBase[,] SortObjectsIntoGrid(List<UndergroundFieldBase> inputList, out int width, out int height)
    {
        if (inputList == null || inputList.Count == 0)
        {
            width = height = 0;
            return new UndergroundFieldBase[0, 0];
        }

        // Same grouping logic as FarmManager (do not change axis assumptions here).
        var sortedByY = inputList.OrderBy(obj => obj.transform.position.y).ToList();
        List<List<UndergroundFieldBase>> rows = new List<List<UndergroundFieldBase>>();

        if (sortedByY.Count > 0)
        {
            List<UndergroundFieldBase> currentRow = new List<UndergroundFieldBase> { sortedByY[0] };
            rows.Add(currentRow);

            for (int i = 1; i < sortedByY.Count; i++)
            {
                if (Mathf.Abs(sortedByY[i].transform.position.y - currentRow[0].transform.position.y) < threshold)
                {
                    currentRow.Add(sortedByY[i]);
                }
                else
                {
                    currentRow = new List<UndergroundFieldBase> { sortedByY[i] };
                    rows.Add(currentRow);
                }
            }
        }

        height = rows.Count;
        width = rows.Max(r => r.Count);

        UndergroundFieldBase[,] grid = new UndergroundFieldBase[width, height];
        for (int y = 0; y < height; y++)
        {
            var sortedRow = rows[y].OrderBy(obj => obj.transform.position.x).ToList();
            for (int x = 0; x < sortedRow.Count; x++)
            {
                grid[x, y] = sortedRow[x];
            }
        }

        return grid;
    }

    public static void ClearCache()
    {
        _grid = null;
        _posLookup = null;
        undergroundFieldList = new();
    }

    internal static void AddField(UndergroundFieldBase field)
    {
        if (field == null) return;
        undergroundFieldList.Add(field);
    }
}


