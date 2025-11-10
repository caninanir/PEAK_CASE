using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayUIController : MonoBehaviour
{
    [Header("Top UI Elements")]
    [SerializeField] private TextMeshProUGUI movesText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Transform goalsContainer;
    [SerializeField] private GameObject goalItemPrefab;
    
    [Header("Goal Display")]
    [SerializeField] private GridLayoutGroup goalsGridLayout;
    
    [System.Serializable]
    public class GoalCellSizeConfig
    {
        public int itemCount;
        public Vector2 cellSize;
        [Tooltip("Fixed row count. Set to 0 or less for flexible layout.")]
        public int fixedRowCount;
    }
    
    [SerializeField] private GoalCellSizeConfig[] cellSizeConfigs = new GoalCellSizeConfig[]
    {
        new GoalCellSizeConfig { itemCount = 1, cellSize = new Vector2(100, 100), fixedRowCount = 0 },
        new GoalCellSizeConfig { itemCount = 2, cellSize = new Vector2(100, 100), fixedRowCount = 0 },
        new GoalCellSizeConfig { itemCount = 3, cellSize = new Vector2(100, 100), fixedRowCount = 0 },
        new GoalCellSizeConfig { itemCount = 4, cellSize = new Vector2(75, 75), fixedRowCount = 0 },
        new GoalCellSizeConfig { itemCount = 5, cellSize = new Vector2(60, 60), fixedRowCount = 0 },
        new GoalCellSizeConfig { itemCount = 6, cellSize = new Vector2(60, 60), fixedRowCount = 2 },
        new GoalCellSizeConfig { itemCount = 7, cellSize = new Vector2(60, 60), fixedRowCount = 2 },
        new GoalCellSizeConfig { itemCount = 8, cellSize = new Vector2(60, 60), fixedRowCount = 2 },
        new GoalCellSizeConfig { itemCount = 9, cellSize = new Vector2(60, 60), fixedRowCount = 2 }
    };

    private GoalDisplayController goalDisplayController;
    private Dictionary<ItemType, GoalItem> goalItems = new Dictionary<ItemType, GoalItem>();

    public IReadOnlyDictionary<ItemType, GoalItem> GetGoalItems()
    {
        return goalItems;
    }

    private void Awake()
    {
        goalDisplayController = GetComponent<GoalDisplayController>() ?? GetComponentInChildren<GoalDisplayController>() ?? gameObject.AddComponent<GoalDisplayController>();
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
        EventBus.Subscribe<MovesChangedEvent>(HandleMovesChanged);
        EventBus.Subscribe<GoalUpdatedEvent>(HandleGoalUpdated);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Unsubscribe<MovesChangedEvent>(HandleMovesChanged);
        EventBus.Unsubscribe<GoalUpdatedEvent>(HandleGoalUpdated);
    }

    private void HandleLevelStarted(LevelStartedEvent evt)
    {
        StartCoroutine(SetupGoalsDelayed());
        UpdateMovesDisplay(GameStateController.Instance.MovesRemaining);
        levelText.text = $"{evt.LevelNumber}"; 
    }

    private System.Collections.IEnumerator SetupGoalsDelayed()
    {
        yield return new WaitForEndOfFrame();
        SetupGoals();
    }

    private void HandleMovesChanged(MovesChangedEvent evt)
    {
        UpdateMovesDisplay(evt.MovesRemaining);
    }

    private void HandleGoalUpdated(GoalUpdatedEvent evt)
    {
        UpdateGoalProgress(evt.ObstacleType, evt.RemainingCount);
    }

    private void SetupGoals()
    {
        LevelData levelData = LevelManager.Instance.GetCurrentLevelData();
        
        ClearGoals();
        
        Dictionary<ItemType, int> allGoals = new Dictionary<ItemType, int>();
        
        Dictionary<ItemType, int> obstacleGoals = levelData.GetObstacleGoals();
        foreach (var goal in obstacleGoals)
        {
            allGoals[goal.Key] = goal.Value;
        }
        
        Dictionary<ItemType, int> cubeGoals = levelData.GetCubeGoals();
        foreach (var goal in cubeGoals)
        {
            allGoals[goal.Key] = goal.Value;
        }
        
        goalDisplayController.SetupGoalLayout(goalsContainer, allGoals, goalItemPrefab, goalItems, goalsGridLayout, cellSizeConfigs);
    }

    private void ClearGoals()
    {
        goalDisplayController.ClearGoals(goalItems);
        goalItems.Clear();
    }

    public void UpdateGoalProgress(ItemType itemType, int remaining)
    {
        if (goalItems.ContainsKey(itemType))
        {
            goalItems[itemType].UpdateCount(remaining);
        }
    }

    private void UpdateMovesDisplay(int moves)
    {
        movesText.text = moves.ToString();
    }
}

