using System.Collections.Generic;
using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    public static ObstacleController Instance { get; private set; }
    
    private Dictionary<ItemType, int> obstacleGoals = new Dictionary<ItemType, int>();
    private Dictionary<ItemType, int> obstaclesRemaining = new Dictionary<ItemType, int>();
    private Dictionary<ItemType, int> cubeGoals = new Dictionary<ItemType, int>();
    private Dictionary<ItemType, int> cubesRemaining = new Dictionary<ItemType, int>();

    public IReadOnlyDictionary<ItemType, int> GetRemainingGoals()
    {
        Dictionary<ItemType, int> allGoals = new Dictionary<ItemType, int>();
        
        foreach (var obstacle in obstaclesRemaining)
        {
            allGoals[obstacle.Key] = obstacle.Value;
        }
        
        foreach (var cube in cubesRemaining)
        {
            allGoals[cube.Key] = cube.Value;
        }
        
        return allGoals;
    }

    public Dictionary<ItemType, int> GetAllGoals()
    {
        Dictionary<ItemType, int> allGoals = new Dictionary<ItemType, int>();
        
        foreach (var obstacle in obstacleGoals)
        {
            allGoals[obstacle.Key] = obstacle.Value;
        }
        
        foreach (var cube in cubeGoals)
        {
            allGoals[cube.Key] = cube.Value;
        }
        
        return allGoals;
    }

    public bool IsGoalCube(ItemType cubeType)
    {
        return cubeGoals.ContainsKey(cubeType) && cubesRemaining.ContainsKey(cubeType) && cubesRemaining[cubeType] > 0;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Subscribe<ObstacleDestroyedEvent>(HandleObstacleDestroyed);
        EventBus.Subscribe<ItemDestroyedEvent>(HandleItemDestroyed);
        EventBus.Subscribe<GoalCollectionAnimationCompleteEvent>(HandleGoalCollectionAnimationComplete);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Unsubscribe<ObstacleDestroyedEvent>(HandleObstacleDestroyed);
        EventBus.Unsubscribe<ItemDestroyedEvent>(HandleItemDestroyed);
        EventBus.Unsubscribe<GoalCollectionAnimationCompleteEvent>(HandleGoalCollectionAnimationComplete);
    }

    private void HandleLevelStarted(LevelStartedEvent evt)
    {
        LevelData levelData = LevelManager.Instance.GetLevelData(evt.LevelNumber);
        if (levelData != null)
        {
            InitializeGoals(levelData);
        }
    }

    public void InitializeGoals(LevelData levelData)
    {
        obstacleGoals.Clear();
        obstaclesRemaining.Clear();
        cubeGoals.Clear();
        cubesRemaining.Clear();
        
        Dictionary<ItemType, int> obstacleGoalCounts = levelData.GetObstacleGoals();
        foreach (var goal in obstacleGoalCounts)
        {
            obstacleGoals[goal.Key] = goal.Value;
            obstaclesRemaining[goal.Key] = goal.Value;
        }
        
        Dictionary<ItemType, int> cubeGoalCounts = levelData.GetCubeGoals();
        foreach (var goal in cubeGoalCounts)
        {
            cubeGoals[goal.Key] = goal.Value;
            cubesRemaining[goal.Key] = goal.Value;
        }
        
        UpdateGoalDisplay();
    }

    private void HandleObstacleDestroyed(ObstacleDestroyedEvent evt)
    {
        ItemType obstacleType = evt.ObstacleType;
        
        if (obstaclesRemaining.ContainsKey(obstacleType) && obstaclesRemaining[obstacleType] > 0)
        {
            if (GoalCollectionAnimationService.Instance != null)
            {
                Vector2Int gridPos = new Vector2Int(evt.GridX, evt.GridY);
                bool isDuck = obstacleType == ItemType.Duck;
                GoalCollectionAnimationService.Instance.AnimateGoalCollection(obstacleType, gridPos, isDuck);
            }
            else
            {
                DecrementObstacleGoal(obstacleType);
            }
        }
    }

    private void HandleGoalCollectionAnimationComplete(GoalCollectionAnimationCompleteEvent evt)
    {
        if (obstaclesRemaining.ContainsKey(evt.ItemType))
        {
            DecrementObstacleGoal(evt.ItemType);
        }
        else if (cubesRemaining.ContainsKey(evt.ItemType))
        {
            DecrementCubeGoal(evt.ItemType);
        }
    }

    private void DecrementObstacleGoal(ItemType obstacleType)
    {
        obstaclesRemaining[obstacleType] = Mathf.Max(0, obstaclesRemaining[obstacleType] - 1);
        
        UpdateGoalDisplay();
        
        if (AreAllGoalsCleared())
        {
            GameStateController.Instance.WinLevel();
        }
    }

    private void DecrementCubeGoal(ItemType cubeType)
    {
        cubesRemaining[cubeType] = Mathf.Max(0, cubesRemaining[cubeType] - 1);
        
        EventBus.Publish(new GoalUpdatedEvent
        {
            ObstacleType = cubeType,
            RemainingCount = cubesRemaining[cubeType]
        });
        
        if (AreAllGoalsCleared())
        {
            GameStateController.Instance.WinLevel();
        }
    }


    private void UpdateGoalDisplay()
    {
        foreach (var obstacle in obstaclesRemaining)
        {
            EventBus.Publish(new GoalUpdatedEvent
            {
                ObstacleType = obstacle.Key,
                RemainingCount = obstacle.Value
            });
        }
        
        foreach (var cube in cubesRemaining)
        {
            EventBus.Publish(new GoalUpdatedEvent
            {
                ObstacleType = cube.Key,
                RemainingCount = cube.Value
            });
        }
    }

    public int GetRemainingObstacles(ItemType obstacleType)
    {
        return obstaclesRemaining.ContainsKey(obstacleType) ? obstaclesRemaining[obstacleType] : 0;
    }

    private void HandleItemDestroyed(ItemDestroyedEvent evt)
    {
        ItemType itemType = evt.ItemType;
        
        if (cubesRemaining.ContainsKey(itemType) && cubesRemaining[itemType] > 0)
        {
            if (GoalCollectionAnimationService.Instance != null)
            {
                GoalCollectionAnimationService.Instance.AnimateGoalCollection(itemType, new Vector2Int(evt.GridX, evt.GridY), false);
            }
            else
            {
                DecrementCubeGoal(itemType);
            }
        }
    }

    public bool AreAllObstaclesCleared()
    {
        foreach (var obstacle in obstaclesRemaining)
        {
            if (obstacle.Value > 0)
                return false;
        }
        return true;
    }

    public bool AreAllGoalsCleared()
    {
        foreach (var obstacle in obstaclesRemaining)
        {
            if (obstacle.Value > 0)
                return false;
        }
        foreach (var cube in cubesRemaining)
        {
            if (cube.Value > 0)
                return false;
        }
        return true;
    }

    public Dictionary<ItemType, int> GetObstacleGoals()
    {
        return new Dictionary<ItemType, int>(obstacleGoals);
    }

    public Dictionary<ItemType, int> GetRemainingObstacles()
    {
        return new Dictionary<ItemType, int>(obstaclesRemaining);
    }
}

