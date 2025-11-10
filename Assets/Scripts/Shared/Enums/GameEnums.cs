using UnityEngine;

public enum ItemType
{
    Empty,
    RedCube,
    GreenCube,
    BlueCube,
    YellowCube,
    PurpleCube,
    RandomCube,
    HorizontalRocket,
    VerticalRocket,
    Balloon,
    Duck
}

public enum GameState
{
    MainMenu,
    Playing,
    GameWon,
    GameLost,
    Finished,
    Paused
}

public enum ObstacleState
{
    Full,
    Damaged,
    Destroyed
}

public enum CubeColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Purple
}

public enum DamageSource
{
    AdjacentBlast,
    Rocket
} 