using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoseGoalsDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Container (empty GameObject) inside LosePopup where goal items will be placed.")]
    [SerializeField] private Transform goalsContainer;

    [Tooltip("Goal item prefab to use if cloning existing UI fails.")]
    [SerializeField] private GameObject goalItemPrefab;

    private void OnEnable()
    {
        StartCoroutine(PopulateGoalsWhenReady());
    }

    private IEnumerator PopulateGoalsWhenReady()
    {
        UIElementPoolManager.EnsureInstance();
        
        int maxAttempts = 10;
        int attempts = 0;
        
        while (UIElementPoolManager.Instance == null && attempts < maxAttempts)
        {
            attempts++;
            UIElementPoolManager.EnsureInstance();
            yield return null;
        }

        if (UIElementPoolManager.Instance == null)
        {
            yield break;
        }

        PopulateGoals();
    }

    private void PopulateGoals()
    {
        if (goalsContainer == null)
        {
            return;
        }

        if (UIElementPoolManager.Instance == null)
        {
            return;
        }

        if (ObstacleController.Instance == null)
        {
            return;
        }

        List<GoalItem> currentGoalItems = new List<GoalItem>();
        foreach (Transform child in goalsContainer)
        {
            GoalItem goalItem = child.GetComponent<GoalItem>();
            if (goalItem != null)
            {
                currentGoalItems.Add(goalItem);
            }
        }
        
        if (currentGoalItems.Count > 0)
        {
            UIElementPoolManager.Instance.ReturnAllGoalItems(currentGoalItems);
        }

        IReadOnlyDictionary<ItemType, int> remainingGoals = ObstacleController.Instance.GetRemainingGoals();
        if (remainingGoals == null) return;

        SetupGoalLayout(remainingGoals.Count);

        foreach (var kvp in remainingGoals)
        {
            ItemType itemType = kvp.Key;
            int countRemaining = kvp.Value;

            if (goalItemPrefab != null)
            {
                UIElementPoolManager.Instance.SetGoalItemPrefab(goalItemPrefab);
            }
            
            GoalItem goalItem = UIElementPoolManager.Instance.GetGoalItem(goalsContainer);
            
            if (goalItem != null)
            {
                goalItem.Initialize(itemType, countRemaining);

                TextMeshProUGUI countText = goalItem.GetComponentInChildren<TextMeshProUGUI>();
                if (countText != null)
                {
                    countText.transform.localScale = new Vector3(4f, 4f, 4f);
                }
            }
        }
    }

    private void SetupGoalLayout(int goalCount)
    {
        HorizontalLayoutGroup hLayout = goalsContainer.GetComponent<HorizontalLayoutGroup>();
        if (hLayout != null) DestroyImmediate(hLayout);

        GridLayoutGroup gLayout = goalsContainer.GetComponent<GridLayoutGroup>();
        if (gLayout != null) DestroyImmediate(gLayout);

        if (goalCount <= 0) return;

        if (goalCount == 1)
        {
            HorizontalLayoutGroup layout = goalsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 0;
            layout.padding = new RectOffset(200, 200, 200, 200);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }
        else if (goalCount == 2)
        {
            GridLayoutGroup layout = goalsContainer.gameObject.AddComponent<GridLayoutGroup>();
            layout.spacing = new Vector2(50, 50);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            layout.constraintCount = 1;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.cellSize = new Vector2(300, 300);
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
        else
        {
            GridLayoutGroup layout = goalsContainer.gameObject.AddComponent<GridLayoutGroup>();
            layout.spacing = new Vector2(50, 50);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            layout.constraintCount = 2;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.cellSize = new Vector2(300, 300);
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
    }
} 