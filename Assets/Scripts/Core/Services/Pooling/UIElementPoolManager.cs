using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIElementPoolManager : MonoBehaviour
{
    public static UIElementPoolManager Instance { get; private set; }

    [Header("UI Prefabs")]
    [SerializeField] private GameObject goalItemPrefab;

    [Header("Pool Settings")]
    [SerializeField] private int goalItemPoolSize = 10;

    private GenericPool<GoalItem> goalItemPool;
    private Transform poolContainer;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
            isInitialized = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            if (!isInitialized)
            {
                InitializePools();
                isInitialized = true;
            }
        }
    }

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        UIElementPoolManager existing = FindFirstObjectByType<UIElementPoolManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            if (!existing.gameObject.activeSelf)
            {
                existing.gameObject.SetActive(true);
            }
            if (!existing.isInitialized)
            {
                existing.InitializePools();
                existing.isInitialized = true;
            }
        }
    }

    private void InitializePools()
    {
        if (isInitialized && goalItemPool != null) return;

        if (poolContainer == null)
        {
            poolContainer = new GameObject("UIElementPoolContainer").transform;
            poolContainer.SetParent(transform);
        }

        if (goalItemPrefab != null && goalItemPool == null)
        {
            GoalItem goalPrefab = goalItemPrefab.GetComponent<GoalItem>();
            if (goalPrefab == null)
            {
                goalPrefab = goalItemPrefab.AddComponent<GoalItem>();
            }
            goalItemPool = new GenericPool<GoalItem>(goalPrefab, poolContainer, goalItemPoolSize);
        }
    }

    public GoalItem GetGoalItem(Transform parent = null)
    {
        if (goalItemPool == null)
        {
            return null;
        }
        
        GoalItem goalItem = goalItemPool.Get();
        if (goalItem != null && parent != null)
        {
            goalItem.transform.SetParent(parent, false);
            goalItem.transform.localScale = Vector3.one;
        }

        return goalItem;
    }

    public void ReturnGoalItem(GoalItem goalItem)
    {
        if (goalItem == null || goalItemPool == null) return;

        goalItem.transform.SetParent(poolContainer, false);
        goalItemPool.Return(goalItem);
    }

    public void ReturnAllGoalItems(IEnumerable<GoalItem> goalItems)
    {
        if (goalItems == null) return;
        
        foreach (var goalItem in goalItems)
        {
            ReturnGoalItem(goalItem);
        }
    }

    public void ClearAllPools()
    {
        goalItemPool?.Clear();
    }

    public void SetGoalItemPrefab(GameObject prefab)
    {
        if (goalItemPrefab == null && prefab != null)
        {
            goalItemPrefab = prefab;
            
            if (goalItemPool == null)
            {
                GoalItem goalPrefab = goalItemPrefab.GetComponent<GoalItem>();
                if (goalPrefab == null)
                {
                    goalPrefab = goalItemPrefab.AddComponent<GoalItem>();
                }
                goalItemPool = new GenericPool<GoalItem>(goalPrefab, poolContainer, goalItemPoolSize);
            }
        }
    }
} 