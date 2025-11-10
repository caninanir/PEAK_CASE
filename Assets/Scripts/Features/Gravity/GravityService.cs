using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityService
{
    private GridController gridController;
    private readonly Dictionary<BaseItem, RectTransform> rectTransformCache = new Dictionary<BaseItem, RectTransform>();
    
    public float DuckCollectionDelay { get; set; } = 0.2f;

    public void Initialize(GridController controller)
    {
        gridController = controller;
    }

    public int CalculateFallDistance(int x, int startY, HashSet<BaseItem> activeFallingItems = null)
    {
        int distance = 0;
        GridCell startCell = gridController.DataService.GetExtendedCell(x, startY);
        BaseItem item = startCell?.currentItem;
        bool isDuck = item?.itemType == ItemType.Duck;
        bool canEnterCollectionRow = isDuck;

        for (int y = startY + 1; y < gridController.DataService.TotalHeight; y++)
        {
            GridCell cellBelow = gridController.DataService.GetExtendedCell(x, y);
            
            if (cellBelow == null)
            {
                break;
            }

            if (gridController.DataService.IsCollectionRow(y) && !canEnterCollectionRow)
            {
                break;
            }

            bool isCollectionRow = gridController.DataService.IsCollectionRow(y);
            bool cellHasDuck = cellBelow.currentItem != null && cellBelow.currentItem.itemType == ItemType.Duck;
            
            if (cellBelow.IsEmpty())
            {
                distance++;
            }
            else if (isCollectionRow && cellHasDuck && isDuck)
            {
                distance++;
            }
            else if (activeFallingItems != null && cellBelow.currentItem != null && activeFallingItems.Contains(cellBelow.currentItem))
            {
                break;
            }
            else
            {
                break;
            }
        }

        return distance;
    }

    public void PrepareFallOperation(GridCell fromCell, GridCell toCell, int distance, List<FallOperation> wave, bool enableSubtleRotation, float maxRotationAngle, MonoBehaviour coroutineRunner)
    {
        BaseItem item = fromCell.RemoveItem();
        RectTransform itemRect = GetCachedRectTransform(item);
        RectTransform fromRect = fromCell.GetComponent<RectTransform>();
        RectTransform toRect = toCell.GetComponent<RectTransform>();
        
        bool isDuck = item?.itemType == ItemType.Duck;
        bool landingInCollectionRow = gridController.DataService.IsCollectionRow(toCell.y);
        
        if (itemRect != null && fromRect != null && toRect != null)
        {
            Vector2 startPos = fromRect.anchoredPosition;
            Quaternion startRot = itemRect.rotation;
            
            toCell.SetItem(item);
            
            if (isDuck && landingInCollectionRow)
            {
                CollectDuckImmediately(item, coroutineRunner);
            }
            
            itemRect.anchoredPosition = startPos;
            
            float duration = CalculateFallDuration(distance);
            
            var operation = new FallOperation
            {
                item = item,
                itemTransform = itemRect,
                startPosition = startPos,
                targetPosition = toRect.anchoredPosition,
                fallDistance = distance,
                duration = duration,
                startRotation = startRot,
                targetRotation = enableSubtleRotation ? 
                    startRot * Quaternion.Euler(0, 0, Random.Range(-maxRotationAngle, maxRotationAngle)) : 
                    startRot,
            };
            
            wave.Add(operation);
        }
        else
        {
            toCell.SetItem(item);
            if (isDuck && landingInCollectionRow)
            {
                CollectDuckImmediately(item, coroutineRunner);
            }
        }
    }

    public void CollectDuckImmediately(BaseItem duck, MonoBehaviour coroutineRunner)
    {
        if (duck == null || duck.itemType != ItemType.Duck) return;

        coroutineRunner.StartCoroutine(CollectDuckDelayed(duck));
    }

    private IEnumerator CollectDuckDelayed(BaseItem duck)
    {
        float randomDelay = Random.Range(0.2f, 0.4f);
        yield return new WaitForSeconds(randomDelay);

        AudioManager.Instance.PlayDuckCollectionSound();
        
        if (GoalCollectionAnimationService.Instance != null && ObstacleController.Instance != null)
        {
            if (ObstacleController.Instance.GetRemainingObstacles(ItemType.Duck) > 0)
            {
                Vector2Int gridPos = duck.GetGridPosition();
                GoalCollectionAnimationService.Instance.AnimateGoalCollection(ItemType.Duck, gridPos, true);
            }
        }
        else
        {
            Vector2Int gridPos = duck.GetGridPosition();
            EventBus.Publish(new ObstacleDestroyedEvent
            {
                ObstacleType = ItemType.Duck,
                GridX = gridPos.x,
                GridY = gridPos.y
            });
        }
    }

    private float CalculateFallDuration(int cellDistance)
    {
        const float fallTimePerCell = 0.08f;
        const float minimumFallTime = 0.1f;
        const float maximumFallTime = 0.6f;
        
        float duration = minimumFallTime + (cellDistance * fallTimePerCell);
        return Mathf.Clamp(duration, minimumFallTime, maximumFallTime);
    }

    private RectTransform GetCachedRectTransform(BaseItem item)
    {
        if (!rectTransformCache.TryGetValue(item, out RectTransform rect))
        {
            rect = item.GetComponent<RectTransform>();
            if (rect != null)
            {
                rectTransformCache[item] = rect;
            }
        }
        
        return rect;
    }
}

public class FallOperation
{
    public BaseItem item;
    public RectTransform itemTransform;
    public Vector2 startPosition;
    public Vector2 targetPosition;
    public int fallDistance;
    public float duration;
    public Quaternion startRotation;
    public Quaternion targetRotation;
}

