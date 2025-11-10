using System.Collections.Generic;

public static class MatchValidator
{
    public static bool IsValidMatch(List<GridCell> group)
    {
        return group.Count >= 2;
    }

    public static bool CanCreateRocket(List<GridCell> group)
    {
        return group.Count >= 5;
    }
}

