using System.Collections.Generic;
using UnityEngine;

public class MatchDetectorService
{
    private GridController gridController;

    public void Initialize(GridController controller)
    {
        gridController = controller;
    }

    public List<GridCell> FindMatchingGroup(GridCell startCell)
    {
        List<GridCell> group = new List<GridCell>();
        HashSet<GridCell> visited = new HashSet<GridCell>();
        
        if (startCell.currentItem is CubeItem startCube)
        {
            FindMatchingNeighbors(startCell, startCube.GetCubeColor(), group, visited);
        }
        
        return group;
    }

    private void FindMatchingNeighbors(GridCell cell, ItemType targetType, List<GridCell> group, HashSet<GridCell> visited)
    {
        if (cell == null || visited.Contains(cell)) return;
        
        CubeItem cube = cell.currentItem as CubeItem;
        if (cube == null || cube.GetCubeColor() != targetType) return;
        
        visited.Add(cell);
        group.Add(cell);
        
        int visibleY = cell.y - gridController.BufferRows;
        
        if (visibleY >= 0 && visibleY < gridController.DataService.GridHeight)
        {
            List<GridCell> adjacentCells = gridController.GetAdjacentCells(cell.x, visibleY);
            foreach (GridCell adjacent in adjacentCells)
            {
                FindMatchingNeighbors(adjacent, targetType, group, visited);
            }
        }
    }
}

