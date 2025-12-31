using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FarmManager
{
    // Configuration
    private static float threshold = 0.5f;
    private static List<FarmFieldBase> farmFieldList = new List<FarmFieldBase>();

    // Caching
    private static FarmFieldBase[,] _grid = null;
    private static Dictionary<FarmFieldBase, Vector2Int> _posLookup = null;

    public static int Width { get; private set; }
    public static int Height { get; private set; }

    /// <summary>
    /// Ensures the grid and lookup dictionary are built before use.
    /// </summary>
    private static void EnsureInitialized()
    {
        if (_grid != null) return;

        _grid = SortObjectsIntoGrid(farmFieldList, out int w, out int h);
        Width = w;
        Height = h;

        // Build a reverse-lookup dictionary for O(1) coordinate access
        _posLookup = new Dictionary<FarmFieldBase, Vector2Int>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_grid[x, y] != null)
                    _posLookup[_grid[x, y]] = new Vector2Int(x, y);
            }
        }
    }

    public static FarmFieldBase GetFarmField(int x, int y)
    {
        EnsureInitialized();

        // Check bounds (using >= because array indices are 0 to Length-1)
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return null;

        return _grid[x, y];
    }

    public static Vector2Int? GetFieldXY(FarmFieldBase farmField)
    {
        EnsureInitialized();

        if (farmField != null && _posLookup.TryGetValue(farmField, out Vector2Int pos))
            return pos;

        return null;
    }

    public static FarmFieldBase[,] SortObjectsIntoGrid(List<FarmFieldBase> inputList, out int width, out int height)
    {
        if (inputList == null || inputList.Count == 0)
        {
            width = height = 0;
            return new FarmFieldBase[0, 0];
        }

        // Grouping logic using threshold
        var sortedByY = inputList.OrderBy(obj => obj.transform.position.y).ToList();
        List<List<FarmFieldBase>> rows = new List<List<FarmFieldBase>>();

        if (sortedByY.Count > 0)
        {
            List<FarmFieldBase> currentRow = new List<FarmFieldBase> { sortedByY[0] };
            rows.Add(currentRow);

            for (int i = 1; i < sortedByY.Count; i++)
            {
                if (Mathf.Abs(sortedByY[i].transform.position.y - currentRow[0].transform.position.y) < threshold)
                {
                    currentRow.Add(sortedByY[i]);
                }
                else
                {
                    currentRow = new List<FarmFieldBase> { sortedByY[i] };
                    rows.Add(currentRow);
                }
            }
        }

        height = rows.Count;
        width = rows.Max(r => r.Count);

        FarmFieldBase[,] grid = new FarmFieldBase[width, height];
        for (int y = 0; y < height; y++)
        {
            // Sort each row by X to ensure correct order
            var sortedRow = rows[y].OrderBy(obj => obj.transform.position.x).ToList();
            for (int x = 0; x < sortedRow.Count; x++)
            {
                grid[x, y] = sortedRow[x];
            }
        }

        return grid;
    }

    // Call this if the farm layout changes during runtime
    public static void ClearCache()
    {
        _grid = null;
        _posLookup = null;
        farmFieldList = new();
    }

    internal static void AddField(FarmFieldBase farmFieldBase)
    {
        farmFieldList.Add(farmFieldBase);
    }

}