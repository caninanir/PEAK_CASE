public struct GameStateChangedEvent : IGameEvent
{
    public GameState PreviousState;
    public GameState NewState;
}

public struct LevelStartedEvent : IGameEvent
{
    public int LevelNumber;
}

public struct LevelWonEvent : IGameEvent
{
    public int LevelNumber;
}

public struct LevelLostEvent : IGameEvent
{
    public int LevelNumber;
}

public struct MovesChangedEvent : IGameEvent
{
    public int MovesRemaining;
}

public struct CubeTappedEvent : IGameEvent
{
    public int GridX;
    public int GridY;
    public ItemType CubeType;
}

public struct MatchFoundEvent : IGameEvent
{
    public int MatchCount;
    public ItemType MatchedType;
}

public struct MatchProcessedEvent : IGameEvent
{
    public int MatchCount;
    public bool RocketCreated;
}

public struct RocketCreatedEvent : IGameEvent
{
    public int GridX;
    public int GridY;
    public ItemType RocketType;
}

public struct RocketExplodedEvent : IGameEvent
{
    public int GridX;
    public int GridY;
    public ItemType RocketType;
    public bool IsCombo;
}

public struct RocketTappedEvent : IGameEvent
{
    public int GridX;
    public int GridY;
    public ItemType RocketType;
}

public struct GravityStartedEvent : IGameEvent { }

public struct GravityCompletedEvent : IGameEvent { }

public struct GridInitializedEvent : IGameEvent
{
    public int GridWidth;
    public int GridHeight;
}

public struct GridUpdatedEvent : IGameEvent { }

public struct ItemSpawnedEvent : IGameEvent
{
    public int GridX;
    public int GridY;
    public ItemType ItemType;
}

public struct ItemDestroyedEvent : IGameEvent
{
    public int GridX;
    public int GridY;
    public ItemType ItemType;
}

public struct ItemDamagedEvent : IGameEvent
{
    public int GridX;
    public int GridY;
    public ItemType ItemType;
    public int DamageAmount;
}

public struct ObstacleDestroyedEvent : IGameEvent
{
    public ItemType ObstacleType;
    public int GridX;
    public int GridY;
}

public struct GoalUpdatedEvent : IGameEvent
{
    public ItemType ObstacleType;
    public int RemainingCount;
}

public struct GoalCollectionAnimationCompleteEvent : IGameEvent
{
    public ItemType ItemType;
    public int GridX;
    public int GridY;
}

public struct ButtonClickedEvent : IGameEvent
{
    public string ButtonName;
}

public struct UIActionEvent : IGameEvent
{
    public string ActionName;
    public object Data;
}

