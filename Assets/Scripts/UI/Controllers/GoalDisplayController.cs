using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoalDisplayController : MonoBehaviour
{
    public void SetupGoalLayout(Transform container, Dictionary<ItemType, int> obstacleGoals, GameObject goalItemPrefab, Dictionary<ItemType, GoalItem> goalItems, GridLayoutGroup gridLayout, GameplayUIController.GoalCellSizeConfig[] cellSizeConfigs)
    {
        if (container == null || gridLayout == null) return;
        
        int goalCount = obstacleGoals.Count;
        List<ItemType> goalTypes = new List<ItemType>(obstacleGoals.Keys);
        
        ApplyCellSizeForGoalCount(gridLayout, goalCount, cellSizeConfigs);
        
        for (int i = 0; i < goalTypes.Count; i++)
        {
            ItemType goalType = goalTypes[i];
            int count = obstacleGoals[goalType];
            CreateGoalItem(container, goalType, count, goalItemPrefab, goalItems);
        }
    }

    private void ApplyCellSizeForGoalCount(GridLayoutGroup gridLayout, int goalCount, GameplayUIController.GoalCellSizeConfig[] cellSizeConfigs)
    {
        if (cellSizeConfigs == null || cellSizeConfigs.Length == 0)
        {
            ApplyDefaultCellSize(gridLayout, goalCount);
            return;
        }
        
        foreach (var config in cellSizeConfigs)
        {
            if (config.itemCount == goalCount)
            {
                gridLayout.cellSize = config.cellSize;
                
                if (config.fixedRowCount > 0)
                {
                    gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    gridLayout.constraintCount = config.fixedRowCount;
                }
                else
                {
                    gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
                }
                
                return;
            }
        }
        
        ApplyDefaultCellSize(gridLayout, goalCount);
    }
    
    private void ApplyDefaultCellSize(GridLayoutGroup gridLayout, int goalCount)
    {
        if (goalCount <= 3)
        {
            gridLayout.cellSize = new Vector2(100, 100);
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        }
        else if (goalCount == 4)
        {
            gridLayout.cellSize = new Vector2(75, 75);
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        }
        else if (goalCount == 5)
        {
            gridLayout.cellSize = new Vector2(60, 60);
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        }
        else if (goalCount >= 6)
        {
            gridLayout.cellSize = new Vector2(60, 60);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            gridLayout.constraintCount = 2;
        }
    }

    private void CreateGoalItem(Transform container, ItemType itemType, int count, GameObject goalItemPrefab, Dictionary<ItemType, GoalItem> goalItems)
    {
        GoalItem goalItem = null;
        
        if (UIElementPoolManager.Instance != null)
        {
            if (goalItemPrefab != null)
            {
                UIElementPoolManager.Instance.SetGoalItemPrefab(goalItemPrefab);
            }
            goalItem = UIElementPoolManager.Instance.GetGoalItem(container);
        }
        else
        {
            GameObject goalObj = Instantiate(goalItemPrefab, container);
            goalItem = goalObj.GetComponent<GoalItem>();
            if (goalItem == null)
            {
                goalItem = goalObj.AddComponent<GoalItem>();
            }
        }
        
        if (goalItem != null)
        {
            goalItem.Initialize(itemType, count);
            goalItems[itemType] = goalItem;
        }
    }

    public void ClearGoals(Dictionary<ItemType, GoalItem> goalItems)
    {
        if (UIElementPoolManager.Instance != null)
        {
            UIElementPoolManager.Instance.ReturnAllGoalItems(goalItems.Values);
        }
        else
        {
            foreach (var goalItem in goalItems.Values)
            {
                if (goalItem != null)
                {
                    Destroy(goalItem.gameObject);
                }
            }
        }
    }
}

