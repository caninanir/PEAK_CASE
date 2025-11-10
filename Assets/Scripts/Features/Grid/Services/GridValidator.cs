public static class GridValidator
{
    public static bool IsValidPosition(int x, int y, int gridWidth, int gridHeight)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    public static bool IsValidExtendedPosition(int x, int y, int gridWidth, int totalHeight)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < totalHeight;
    }

    public static bool CanSpawnItem(GridCell cell)
    {
        return cell != null && cell.IsEmpty();
    }
}

