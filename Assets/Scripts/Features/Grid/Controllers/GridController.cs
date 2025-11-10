using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GridController : MonoBehaviour
{
    public static GridController Instance { get; private set; }

    [Header("Grid Setup")]
    [SerializeField] private GridCell cellPrefab;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private Transform gridBackgroundContainer;
    [SerializeField] private float cellSpacing = 10f;
    [SerializeField] private int bufferRows = 1;
    [SerializeField] private int collectionRows = 1;
    
    [Header("Grid Constraints")]
    [SerializeField] private int maxGridWidth = 14;
    [SerializeField] private int maxGridHeight = 10;
    [SerializeField] private float paddingLeft = 50f;
    [SerializeField] private float paddingRight = 50f;
    [SerializeField] private float paddingTop = 262f;
    [SerializeField] private float paddingBottom = 50f;
    [SerializeField] private float bufferRowGap = 4f;
    [SerializeField] private float collectionRowOffset = 5f;

    [Header("Item Prefabs")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject rocketPrefab;
    [SerializeField] private GameObject obstaclePrefab;
    [FormerlySerializedAs("itemSizeMultiplier")]
    [SerializeField] private float cubeSizeMultiplier = 1.2f;
    [SerializeField] private float obstacleSizeMultiplier = 1.2f;

    [Header("Mask Settings")]
    [SerializeField] private float maskCutoffOffset = 0f;

    private GridDataService dataService;
    private GridLayoutService layoutService;
    private RectMask2D gridMask;
    private GridBackground gridBackground;
    private Dictionary<ItemType, GameObject> itemPrefabMap;

    [System.Serializable]
    private struct ObstacleSizeOverride
    {
        public ItemType obstacleType;
        [Min(0.1f)] public float heightMultiplier;
    }

    [SerializeField] private ObstacleSizeOverride[] obstacleHeightOverrides = new ObstacleSizeOverride[0];

    public Transform GridContainer => gridContainer;
    public Transform GridBackgroundContainer => gridBackgroundContainer;
    public GridDataService DataService => dataService;
    public GridLayoutService LayoutService => layoutService;
    public int BufferRows => bufferRows;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        dataService = new GridDataService();
        layoutService = new GridLayoutService();
        InitializePrefabMap();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Subscribe<GravityCompletedEvent>(HandleGravityCompleted);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<LevelStartedEvent>(HandleLevelStarted);
        EventBus.Unsubscribe<GravityCompletedEvent>(HandleGravityCompleted);
    }

    private void InitializePrefabMap()
    {
        itemPrefabMap = new Dictionary<ItemType, GameObject>
        {
            { ItemType.RedCube, cubePrefab },
            { ItemType.GreenCube, cubePrefab },
            { ItemType.BlueCube, cubePrefab },
            { ItemType.YellowCube, cubePrefab },
            { ItemType.PurpleCube, cubePrefab },
            { ItemType.HorizontalRocket, rocketPrefab },
            { ItemType.VerticalRocket, rocketPrefab },
            { ItemType.Balloon, obstaclePrefab },
            { ItemType.Duck, obstaclePrefab }
        };
    }

    private void HandleLevelStarted(LevelStartedEvent evt)
    {
        if (TransitionController.Instance != null && TransitionController.Instance.IsFading)
        {
            StartCoroutine(WaitForFadeAndInitialize(evt.LevelNumber));
        }
        else
        {
            InitializeGridForLevel(evt.LevelNumber);
        }
    }

    private IEnumerator WaitForFadeAndInitialize(int levelNumber)
    {
        bool fadeComplete = false;
        System.Action onFadeComplete = () => fadeComplete = true;
        TransitionController.OnFadeInComplete.AddListener(onFadeComplete.Invoke);
        
        while (!fadeComplete && TransitionController.Instance.IsFading)
        {
            yield return null;
        }
        
        TransitionController.OnFadeInComplete.RemoveListener(onFadeComplete.Invoke);
        yield return new WaitForSeconds(0.1f);
        
        InitializeGridForLevel(levelNumber);
    }

    private void InitializeGridForLevel(int levelNumber)
    {
        LevelData levelData = LevelManager.Instance.GetLevelData(levelNumber);
        if (levelData != null)
        {
            InitializeGrid(levelData);
        }
    }

    public void InitializeGrid(LevelData levelData)
    {
        ClearGrid();
        
        dataService.Initialize(levelData.grid_width, levelData.grid_height, bufferRows, collectionRows);
        layoutService.Initialize(cellSpacing, paddingLeft, paddingRight, paddingTop, paddingBottom, bufferRowGap, maxGridWidth, maxGridHeight);
        layoutService.SetCollectionRowOffset(collectionRowOffset);
        layoutService.CalculateCellSize(gridContainer, dataService.GridWidth, dataService.GridHeight);
        
        CreateExtendedGrid();
        PopulateVisibleGrid(levelData.grid);
        PopulateBufferRows();
        SetupGridMask();
        SetupGridBackground();
        layoutService.CenterGrid(gridContainer, dataService.GridWidth, dataService.GridHeight);
        ApplyMaskPadding();
        
        gridBackground.UpdateBackgroundTransform();
        
        EventBus.Publish(new GridInitializedEvent
        {
            GridWidth = dataService.GridWidth,
            GridHeight = dataService.GridHeight
        });
    }

    private void ClearGrid()
    {
        if (dataService.Grid != null)
        {
            for (int x = 0; x < dataService.Grid.GetLength(0); x++)
            {
                for (int y = 0; y < dataService.Grid.GetLength(1); y++)
                {
                    if (dataService.Grid[x, y] != null)
                    {
                        DestroyImmediate(dataService.Grid[x, y].gameObject);
                    }
                }
            }
        }
        
        for (int i = gridContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridContainer.GetChild(i).gameObject);
        }
        
        if (gridMask != null)
        {
            DestroyImmediate(gridMask);
            gridMask = null;
        }
        
        dataService.Clear();
    }

    private void CreateExtendedGrid()
    {
        for (int x = 0; x < dataService.GridWidth; x++)
        {
            for (int y = 0; y < dataService.TotalHeight; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab.gameObject, gridContainer);
                GridCell cell = cellObj.GetComponent<GridCell>();
                
                RectTransform cellRect = cellObj.GetComponent<RectTransform>();
                int collectionRowStart = bufferRows + dataService.GridHeight;
                layoutService.PositionCell(cellRect, x, y, bufferRows, collectionRowStart);
                
                cell.Initialize(x, y);
                dataService.SetCell(x, y, cell);
            }
        }
    }

    private void PopulateVisibleGrid(string[] gridData)
    {
        for (int i = 0; i < gridData.Length && i < dataService.GridWidth * dataService.GridHeight; i++)
        {
            int x = i % dataService.GridWidth;
            int jsonY = i / dataService.GridWidth;
            int gridY = (dataService.GridHeight - 1) - jsonY + bufferRows;
            
            string itemString = gridData[i];
            ItemType itemType = ParseItemType(itemString);
            
            if (itemType != ItemType.Empty)
            {
                SpawnItemInExtendedGrid(itemType, x, gridY);
            }
        }
    }

    private void PopulateBufferRows()
    {
        for (int x = 0; x < dataService.GridWidth; x++)
        {
            for (int y = 0; y < bufferRows; y++)
            {
                ItemType randomCube = GetRandomCubeType();
                SpawnItemInExtendedGrid(randomCube, x, y);
            }
        }
    }

    private void SetupGridMask()
    {
        layoutService.SetupGridMask(gridContainer, dataService.GridHeight);
        gridMask = gridContainer.gameObject.GetComponent<RectMask2D>();
        ApplyMaskPadding();
    }

    private void SetupGridBackground()
    {
        gridBackground = gridBackgroundContainer.GetComponentInChildren<GridBackground>();
        if (gridBackground != null)
        {
            gridBackground.InitializeBackground(gridContainer);
        }
    }

    public BaseItem SpawnItem(ItemType itemType, int x, int y)
    {
        int extendedY = y + bufferRows;
        return SpawnItemInExtendedGrid(itemType, x, extendedY);
    }

    public BaseItem SpawnItemInExtendedGrid(ItemType itemType, int x, int y)
    {
        if (!GridValidator.IsValidExtendedPosition(x, y, dataService.GridWidth, dataService.TotalHeight))
        {
            return null;
        }
        
        if (!itemPrefabMap.ContainsKey(itemType))
        {
            return null;
        }
        
        GridCell cell = dataService.GetExtendedCell(x, y);
        if (!GridValidator.CanSpawnItem(cell))
        {
            return null;
        }
        
        
        BaseItem item = PoolManager.Instance.GetItem(itemType, gridContainer);
        
        RectTransform itemRect = item.GetComponent<RectTransform>();
        RectTransform cellRect = cell.GetComponent<RectTransform>();
        
        itemRect.anchorMin = cellRect.anchorMin;
        itemRect.anchorMax = cellRect.anchorMax;
        itemRect.anchoredPosition = cellRect.anchoredPosition;
        Vector2 sizeMultiplier = GetScaleForItemType(itemType);
        itemRect.sizeDelta = new Vector2(
            cellRect.sizeDelta.x * sizeMultiplier.x,
            cellRect.sizeDelta.y * sizeMultiplier.y);
        
        item.Initialize(itemType);
        cell.SetItem(item);
        
        UpdateItemSiblingOrder(item);
        
        bool isRocket = itemType == ItemType.HorizontalRocket || itemType == ItemType.VerticalRocket;
        if (isRocket && item is RocketItem rocketItem)
        {
            StartCoroutine(AnimateRocketSpawn(rocketItem));
        }
        
        EventBus.Publish(new ItemSpawnedEvent
        {
            GridX = x,
            GridY = y - bufferRows,
            ItemType = itemType
        });
        
        return item;
    }

    public void SpawnNewCubes()
    {
        for (int x = 0; x < dataService.GridWidth; x++)
        {
            GridCell cell = dataService.GetExtendedCell(x, 0);
            if (cell != null && cell.IsEmpty())
            {
                ItemType randomCube = GetRandomCubeType();
                SpawnItemInExtendedGrid(randomCube, x, 0);
            }
        }
    }

    public bool CanSpawnInBufferRow(int x)
    {
        GridCell bufferCell = dataService.GetExtendedCell(x, 0);
        if (bufferCell == null || !bufferCell.IsEmpty())
        {
            return false;
        }

        for (int y = 1; y < dataService.TotalHeight; y++)
            {
            GridCell cellBelow = dataService.GetExtendedCell(x, y);
            if (cellBelow != null && cellBelow.IsEmpty())
            {
                return true;
            }
        }

        return false;
    }

    public void RepopulateBufferRow()
    {
        for (int x = 0; x < dataService.GridWidth; x++)
        {
            if (CanSpawnInBufferRow(x))
                {
                    ItemType randomCube = GetRandomCubeType();
                SpawnItemInExtendedGrid(randomCube, x, 0);
            }
        }
    }

    public bool RepopulateBufferRowColumn(int x)
    {
        if (CanSpawnInBufferRow(x))
        {
            ItemType randomCube = GetRandomCubeType();
            SpawnItemInExtendedGrid(randomCube, x, 0);
            return true;
        }
        return false;
    }

    private void HandleGravityCompleted(GravityCompletedEvent evt)
    {
        RepopulateBufferRow();
        EventBus.Publish(new GridUpdatedEvent());
    }

    public Vector2 GetScaleForItemType(ItemType type)
    {
        switch (type)
        {
            case ItemType.RedCube:
            case ItemType.GreenCube:
            case ItemType.BlueCube:
            case ItemType.YellowCube:
            case ItemType.PurpleCube:
                return new Vector2(cubeSizeMultiplier, cubeSizeMultiplier);
            case ItemType.Balloon:
            case ItemType.Duck:
                return new Vector2(obstacleSizeMultiplier, GetObstacleHeightMultiplier(type));
            default:
                return Vector2.one;
        }
    }

    private IEnumerator AnimateRocketSpawn(RocketItem rocketItem)
    {
        if (rocketItem == null) yield break;
        
        RectTransform rocketRect = rocketItem.GetComponent<RectTransform>();
        if (rocketRect == null) yield break;
        
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 originalScale = rocketRect.localScale;
        
        rocketRect.localScale = originalScale;
        rocketItem.SetVisualScale(0f);
        
        while (elapsed < duration)
        {
            if (rocketRect == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.SmoothStep(0f, 1f, t);
            
            rocketItem.SetVisualScale(scale);
            yield return null;
        }
        
        if (rocketRect != null)
        {
            rocketRect.localScale = originalScale;
            rocketItem.SetVisualScale(1f);
        }
    }

    private float GetObstacleHeightMultiplier(ItemType type)
    {
        for (int i = 0; i < obstacleHeightOverrides.Length; i++)
        {
            if (obstacleHeightOverrides[i].obstacleType == type)
            {
                return obstacleHeightOverrides[i].heightMultiplier;
            }
        }

        return obstacleSizeMultiplier;
    }

    private void ApplyMaskPadding()
    {
        if (gridMask == null) return;

        float offset = -maskCutoffOffset;
        gridMask.padding = new Vector4(offset, offset, offset, offset);
    }

    public void UpdateItemSiblingOrder(BaseItem item)
    {
        if (item.currentCell == null) return;
        
        int cellCount = dataService.GridWidth * dataService.TotalHeight;
        int rowBasedIndex = (dataService.TotalHeight - item.currentCell.y - 1) * dataService.GridWidth + item.currentCell.x;
        int desiredIndex = cellCount + rowBasedIndex;

        desiredIndex = Mathf.Min(desiredIndex, gridContainer.childCount - 1);
        item.transform.SetSiblingIndex(desiredIndex);
    }

    public ItemType ParseItemType(string itemString)
    {
        switch (itemString.ToLower())
        {
            case "r": return ItemType.RedCube;
            case "g": return ItemType.GreenCube;
            case "b": return ItemType.BlueCube;
            case "y": return ItemType.YellowCube;
            case "p": return ItemType.PurpleCube;
            case "rand": return GetRandomCubeType();
            case "vro": return ItemType.VerticalRocket;
            case "hro": return ItemType.HorizontalRocket;
            case "ba": return ItemType.Balloon;
            case "du": return ItemType.Duck;
            default: return ItemType.Empty;
        }
    }

    public ItemType GetRandomCubeType()
    {
        ItemType[] cubeTypes = { ItemType.RedCube, ItemType.GreenCube, ItemType.BlueCube, ItemType.YellowCube, ItemType.PurpleCube };
        return cubeTypes[Random.Range(0, cubeTypes.Length)];
    }

    public GridCell GetCell(int x, int y)
    {
        return dataService.GetCell(x, y);
    }

    public BaseItem GetItem(int x, int y)
    {
        return dataService.GetItem(x, y);
    }

    public bool IsValidPosition(int x, int y)
    {
        return dataService.IsValidPosition(x, y);
    }

    public Vector2Int GetGridSize()
    {
        return dataService.GetGridSize();
    }

    public List<GridCell> GetAdjacentCells(int x, int y)
    {
        return dataService.GetAdjacentCells(x, y);
    }
}

