using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GravityController : MonoBehaviour
{
    [Header("Performance")]
    [SerializeField] private bool enableBatching = true;
    [SerializeField] private int maxOperationsPerFrame = 30;
    
    [Header("Buffer Row")]
    [SerializeField] private float bufferRowFallDelay = 0.2f;
    
    [Header("Duck Collection")]
    [SerializeField] private float duckCollectionDelay = 0.2f;

    private GridController gridController;
    private GravityService gravityService;
    private FallAnimator fallAnimator;
    private Dictionary<BaseItem, float> bufferRowSpawnTimes = new Dictionary<BaseItem, float>();

    private void Awake()
    {
        gridController = FindFirstObjectByType<GridController>();
        gravityService = new GravityService();
        fallAnimator = GetComponent<FallAnimator>() ?? GetComponentInChildren<FallAnimator>();
        
        if (fallAnimator == null)
        {
            GameObject animatorObj = new GameObject("FallAnimator");
            animatorObj.transform.SetParent(transform);
            fallAnimator = animatorObj.AddComponent<FallAnimator>();
        }
        
        gravityService.Initialize(gridController);
        gravityService.DuckCollectionDelay = duckCollectionDelay;
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<GravityStartedEvent>(HandleGravityStarted);
        EventBus.Subscribe<MatchProcessedEvent>(HandleMatchProcessed);
    }


    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<GravityStartedEvent>(HandleGravityStarted);
        EventBus.Unsubscribe<MatchProcessedEvent>(HandleMatchProcessed);
    }

    private void HandleGravityStarted(GravityStartedEvent evt)
    {
        StartCoroutine(ProcessGravity());
    }

    private void HandleMatchProcessed(MatchProcessedEvent evt)
    {
        StartCoroutine(ProcessGravity());
    }

    public IEnumerator ProcessGravity()
    {
        yield return null;
        yield return StartCoroutine(ProcessAllFalls());
        
        CollectDucksInCollectionRow();
        gridController.RepopulateBufferRow();
        EventBus.Publish(new GridUpdatedEvent());
        EventBus.Publish(new GravityCompletedEvent());
        
        bool topRowHasEmpty = false;
        for (int x = 0; x < gridController.DataService.GridWidth; x++)
        {
            GridCell topCell = gridController.GetCell(x, 0);
            if (topCell?.IsEmpty() == true)
            {
                topRowHasEmpty = true;
                break;
            }
        }
        
        if (topRowHasEmpty)
        {
            gridController.RepopulateBufferRow();
            EventBus.Publish(new GravityStartedEvent());
        }
    }

    private IEnumerator ProcessAllFalls()
    {
        bool itemsAreFalling = true;
        int fallIterations = 0;
        const int maxFallIterations = 20;
        HashSet<BaseItem> activeFallingItems = new HashSet<BaseItem>();
        
        while (itemsAreFalling && fallIterations < maxFallIterations)
        {
            gridController.RepopulateBufferRow();
            
            itemsAreFalling = false;
            List<FallOperation> currentWave = new List<FallOperation>();
            int operationsThisFrame = 0;
            
            for (int x = 0; x < gridController.DataService.GridWidth; x++)
            {
                for (int y = gridController.DataService.TotalHeight - 2; y >= 0; y--)
                {
                    if (enableBatching && operationsThisFrame >= maxOperationsPerFrame)
                    {
                        yield return null;
                        operationsThisFrame = 0;
                    }
                    
                    GridCell currentCell = gridController.DataService.GetExtendedCell(x, y);
                    if (currentCell?.currentItem == null || !currentCell.currentItem.CanFall()) continue;
                    if (activeFallingItems.Contains(currentCell.currentItem)) continue;
                    
                    int fallDistance = gravityService.CalculateFallDistance(x, y, activeFallingItems);
                    if (fallDistance > 0)
                    {
                        if (y == 0 && !CanBufferRowItemFall(currentCell.currentItem))
                        {
                            continue;
                        }
                        
                        BaseItem itemToFall = currentCell.currentItem;
                        GridCell targetCell = gridController.DataService.GetExtendedCell(x, y + fallDistance);
                        if (targetCell != null)
                        {
                            gravityService.PrepareFallOperation(
                                currentCell, 
                                targetCell, 
                                fallDistance, 
                                currentWave,
                                fallAnimator.enableSubtleRotation,
                                fallAnimator.maxRotationAngle,
                                this
                            );
                            activeFallingItems.Add(itemToFall);
                            if (y == 0 && itemToFall != null)
                            {
                                bufferRowSpawnTimes.Remove(itemToFall);
                            }
                            itemsAreFalling = true;
                            operationsThisFrame++;
                        }
                    }
                }
            }
            
            if (currentWave.Count > 0)
            {
                yield return StartCoroutine(AnimateFallsWithContinuousProcessing(currentWave, activeFallingItems));
            }
            else
            {
                yield return null;
            }
            
            fallIterations++;
        }
    }

    private IEnumerator AnimateFallsWithContinuousProcessing(List<FallOperation> operations, HashSet<BaseItem> activeFallingItems)
    {
        Dictionary<FallOperation, float> operationStartTimes = new Dictionary<FallOperation, float>();
        float globalStartTime = 0f;
        
        foreach (var op in operations)
        {
            operationStartTimes[op] = globalStartTime;
        }
        
        int currentColumnIndex = 0;
        bool rowSpawnInProgress = false;
        
        yield return StartCoroutine(fallAnimator.AnimateFallsWithContinuousProcessing(
            operations,
            operationStartTimes,
            () => {
                List<FallOperation> newFalls = CollectNewFalls(activeFallingItems);
                foreach (var op in newFalls)
                {
                    activeFallingItems.Add(op.item);
                }
                return newFalls;
            },
            () => {
                if (!rowSpawnInProgress)
                {
                    rowSpawnInProgress = true;
                    currentColumnIndex = 0;
                }
                
                if (rowSpawnInProgress)
                {
                    bool spawnedAny = false;
                    
                    for (int i = 0; i < gridController.DataService.GridWidth; i++)
                    {
                        int column = (currentColumnIndex + i) % gridController.DataService.GridWidth;
                        if (gridController.RepopulateBufferRowColumn(column))
                        {
                            spawnedAny = true;
                            currentColumnIndex = (column + 1) % gridController.DataService.GridWidth;
                            break;
                        }
                    }
                    
                    if (!spawnedAny)
                    {
                        rowSpawnInProgress = false;
                    }
                }
            }
        ));
        
        foreach (var op in operations)
        {
            if (op.item != null)
            {
                activeFallingItems.Remove(op.item);
            }
        }
    }

    private List<FallOperation> CollectNewFalls(HashSet<BaseItem> activeFallingItems)
    {
        List<FallOperation> newFalls = new List<FallOperation>();
        
        for (int x = 0; x < gridController.DataService.GridWidth; x++)
        {
            for (int y = gridController.DataService.TotalHeight - 2; y >= 0; y--)
            {
                GridCell currentCell = gridController.DataService.GetExtendedCell(x, y);
                if (currentCell?.currentItem == null || !currentCell.currentItem.CanFall()) continue;
                if (activeFallingItems.Contains(currentCell.currentItem)) continue;
                
                int fallDistance = gravityService.CalculateFallDistance(x, y, activeFallingItems);
                if (fallDistance > 0)
                {
                    if (y == 0 && !CanBufferRowItemFall(currentCell.currentItem))
                    {
                        continue;
                    }
                    
                    BaseItem itemToFall = currentCell.currentItem;
                    GridCell targetCell = gridController.DataService.GetExtendedCell(x, y + fallDistance);
                    if (targetCell != null)
                    {
                        gravityService.PrepareFallOperation(
                            currentCell, 
                            targetCell, 
                            fallDistance, 
                            newFalls,
                            fallAnimator.enableSubtleRotation,
                            fallAnimator.maxRotationAngle,
                            this
                        );
                        if (y == 0 && itemToFall != null)
                        {
                            bufferRowSpawnTimes.Remove(itemToFall);
                        }
                    }
                }
            }
        }
        
        return newFalls;
    }

    private List<FallOperation> CollectNewFallsForColumn(HashSet<BaseItem> activeFallingItems, int column)
    {
        List<FallOperation> newFalls = new List<FallOperation>();
        
        for (int y = gridController.DataService.TotalHeight - 2; y >= 0; y--)
        {
            GridCell currentCell = gridController.DataService.GetExtendedCell(column, y);
            if (currentCell?.currentItem == null || !currentCell.currentItem.CanFall()) continue;
            if (activeFallingItems.Contains(currentCell.currentItem)) continue;
            
            int fallDistance = gravityService.CalculateFallDistance(column, y, activeFallingItems);
            if (fallDistance > 0)
            {
                if (y == 0 && !CanBufferRowItemFall(currentCell.currentItem))
                {
                    continue;
                }
                
                BaseItem itemToFall = currentCell.currentItem;
                GridCell targetCell = gridController.DataService.GetExtendedCell(column, y + fallDistance);
                if (targetCell != null)
                {
                    gravityService.PrepareFallOperation(
                        currentCell, 
                        targetCell, 
                        fallDistance, 
                        newFalls,
                        fallAnimator.enableSubtleRotation,
                        fallAnimator.maxRotationAngle,
                        this
                    );
                    if (y == 0 && itemToFall != null)
                    {
                        bufferRowSpawnTimes.Remove(itemToFall);
                    }
                }
            }
        }
        
        return newFalls;
    }

    private bool CanBufferRowItemFall(BaseItem item)
    {
        if (!bufferRowSpawnTimes.ContainsKey(item))
        {
            bufferRowSpawnTimes[item] = Time.time;
            return false;
        }
        
        float timeSinceSpawn = Time.time - bufferRowSpawnTimes[item];
        return timeSinceSpawn >= bufferRowFallDelay;
    }

    private void CollectDucksInCollectionRow()
    {
        int collectionRowStart = gridController.DataService.BufferRows + gridController.DataService.GridHeight;
        
        for (int x = 0; x < gridController.DataService.GridWidth; x++)
        {
            for (int y = collectionRowStart; y < gridController.DataService.TotalHeight; y++)
            {
                GridCell cell = gridController.DataService.GetExtendedCell(x, y);
                if (cell?.currentItem != null && cell.currentItem.itemType == ItemType.Duck)
                {
                    BaseItem duck = cell.currentItem;
                    int visibleY = cell.y - gridController.DataService.BufferRows;
                    
                    if (GoalCollectionAnimationService.Instance == null)
                    {
                        EventBus.Publish(new ObstacleDestroyedEvent
                        {
                            ObstacleType = ItemType.Duck,
                            GridX = x,
                            GridY = visibleY
                        });
                    }
                    
                    cell.RemoveItem();
                    duck.OnDestroyed();
                    PoolManager.Instance.ReturnItem(duck);
                }
            }
        }
    }
}

