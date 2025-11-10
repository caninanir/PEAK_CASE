using System.Collections.Generic;
using UnityEngine;

public class GridDataService
{
    private GridCell[,] grid;
    private int gridWidth;
    private int gridHeight;
    private int totalHeight;
    private int bufferRows;
    private int collectionRows;

    public GridCell[,] Grid => grid;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public int TotalHeight => totalHeight;
    public int BufferRows => bufferRows;
    public int CollectionRows => collectionRows;

    public void Initialize(int width, int height, int bufferRows, int collectionRows)
    {
        this.gridWidth = width;
        this.gridHeight = height;
        this.bufferRows = bufferRows;
        this.collectionRows = collectionRows;
        this.totalHeight = height + bufferRows + collectionRows;
        this.grid = new GridCell[gridWidth, totalHeight];
    }

    public bool IsCollectionRow(int extendedY)
    {
        int collectionRowStart = bufferRows + gridHeight;
        return extendedY >= collectionRowStart && extendedY < totalHeight;
    }

    public bool IsBufferRow(int extendedY)
    {
        return extendedY < bufferRows;
    }

    public void SetCell(int x, int y, GridCell cell)
    {
        if (IsValidExtendedPosition(x, y))
        {
            grid[x, y] = cell;
        }
    }

    public GridCell GetExtendedCell(int x, int y)
    {
        if (IsValidExtendedPosition(x, y))
        {
            return grid[x, y];
        }
        return null;
    }

    public GridCell GetCell(int x, int y)
    {
        int extendedY = y + bufferRows;
        return GetExtendedCell(x, extendedY);
    }

    public BaseItem GetItem(int x, int y)
    {
        GridCell cell = GetCell(x, y);
        return cell?.currentItem;
    }

    public bool IsValidExtendedPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < totalHeight;
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    public Vector2Int GetGridSize()
    {
        return new Vector2Int(gridWidth, gridHeight);
    }

    public List<GridCell> GetAdjacentCells(int x, int y)
    {
        List<GridCell> adjacentCells = new List<GridCell>();
        
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (Vector2Int dir in directions)
        {
            int newX = x + dir.x;
            int newY = y + dir.y;
            
            if (IsValidPosition(newX, newY))
            {
                GridCell cell = GetCell(newX, newY);
                if (cell != null)
                {
                    adjacentCells.Add(cell);
                }
            }
        }
        
        return adjacentCells;
    }

    public void Clear()
    {
        grid = null;
        gridWidth = 0;
        gridHeight = 0;
        totalHeight = 0;
        bufferRows = 0;
        collectionRows = 0;
    }
}

