using UnityEngine;
using UnityEngine.UI;

public class GridLayoutManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private float cellSpacing = 10f;
    [SerializeField] private Vector2 gridPadding = new Vector2(50f, 50f);
    
    [Header("Background Settings")]
    [SerializeField] private bool useNineSlice = true;
    [SerializeField] private Image.Type backgroundImageType = Image.Type.Sliced;
    [SerializeField] private float backgroundOffsetLeft = 30f;
    [SerializeField] private float backgroundOffsetRight = 30f;
    [SerializeField] private float backgroundOffsetUp = 30f;
    [SerializeField] private float backgroundOffsetDown = 30f;
    [SerializeField] private float backgroundExtraHeight = 100f;
    
    [Header("Grid Offsets")]
    [SerializeField] private float gridOffsetLeft = 0f;
    [SerializeField] private float gridOffsetRight = 0f;
    [SerializeField] private float gridOffsetUp = 0f;
    [SerializeField] private float gridOffsetDown = 0f;
    
    private Canvas gameplayCanvas;
    private Image backgroundImage;
    private RectMask2D gridMask;
    
    public float CellSpacing => cellSpacing;
    public float CellSize { get; private set; }
    
    public void Initialize(Canvas canvas, Transform gridContainer, Transform gridBackgroundContainer)
    {
        gameplayCanvas = canvas;
    }
    
    public void SetupGridLayout(int gridWidth, int gridHeight, Transform gridContainer, Transform gridBackgroundContainer)
    {
        RectTransform canvasRect = gameplayCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        float availableWidth = canvasSize.x - (gridPadding.x * 2);
        float availableHeight = canvasSize.y - (gridPadding.y * 2);
        
        float maxCellWidth = (availableWidth - (gridWidth - 1) * cellSpacing) / gridWidth;
        float maxCellHeight = (availableHeight - (gridHeight - 1) * cellSpacing) / gridHeight;
        
        CellSize = Mathf.Min(maxCellWidth, maxCellHeight);
        
        SetupGridContainer(gridWidth, gridHeight, gridContainer);
        SetupGridBackground(gridWidth, gridHeight, gridContainer, gridBackgroundContainer);
        SetupGridMask(gridBackgroundContainer);

        RectTransform gridRectTf = gridContainer.GetComponent<RectTransform>();

        if (gridContainer.parent != gridBackgroundContainer)
        {
            gridContainer.SetParent(gridBackgroundContainer, true);
        }

        gridRectTf.anchorMin = new Vector2(0.5f, 0.5f);
        gridRectTf.anchorMax = new Vector2(0.5f, 0.5f);
        gridRectTf.pivot     = new Vector2(0.5f, 0.5f);

        int bgIndex = backgroundImage.transform.GetSiblingIndex();
        gridContainer.SetSiblingIndex(bgIndex + 1);
    }
    
    private void SetupGridContainer(int gridWidth, int gridHeight, Transform gridContainer)
    {
        RectTransform gridRect = gridContainer.GetComponent<RectTransform>();
        if (gridRect == null)
            gridRect = gridContainer.gameObject.AddComponent<RectTransform>();
        
        float totalWidth = gridWidth * CellSize + (gridWidth - 1) * cellSpacing;
        float totalHeight = gridHeight * CellSize + (gridHeight - 1) * cellSpacing;
        
        RectTransform canvasRect = gameplayCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        float scaleFactorX = canvasSize.x / 1080f;
        float scaleFactorY = canvasSize.y / 1920f;
        
        float offsetX = (gridOffsetLeft - gridOffsetRight) * scaleFactorX * 0.5f;
        float offsetY = (gridOffsetUp - gridOffsetDown) * scaleFactorY * 0.5f;

        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(offsetX, offsetY);
        gridRect.sizeDelta = new Vector2(totalWidth, totalHeight);
    }
    
    private void SetupGridBackground(int gridWidth, int gridHeight, Transform gridContainer, Transform gridBackgroundContainer)
    {
        backgroundImage = null;
        for (int i = 0; i < gridBackgroundContainer.childCount; i++)
        {
            Transform child = gridBackgroundContainer.GetChild(i);
            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                backgroundImage = img;
                break;
            }
        }

        if (backgroundImage == null)
        {
            GameObject backgroundObj = new GameObject("GridBackground");
            backgroundObj.transform.SetParent(gridBackgroundContainer, false);
            
            backgroundObj.AddComponent<RectTransform>();
            backgroundImage = backgroundObj.AddComponent<Image>();
        }
        
        RectTransform gridRect = gridContainer.GetComponent<RectTransform>();
        RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
        
        RectTransform canvasRect = gameplayCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        float scaleFactorX = canvasSize.x / 1080f;
        float scaleFactorY = canvasSize.y / 1920f;
        
        float offsetX = (backgroundOffsetLeft - backgroundOffsetRight) * scaleFactorX * 0.5f;
        float offsetY = (backgroundOffsetUp - backgroundOffsetDown) * scaleFactorY * 0.5f;
        
        float bgWidth = gridRect.sizeDelta.x + (backgroundOffsetLeft + backgroundOffsetRight) * scaleFactorX;
        float bgHeight = gridRect.sizeDelta.y + (backgroundOffsetUp + backgroundOffsetDown) * scaleFactorY + backgroundExtraHeight;
        
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = gridRect.anchoredPosition + new Vector2(offsetX, offsetY);
        bgRect.sizeDelta = new Vector2(bgWidth, bgHeight);
        bgRect.localScale = Vector3.one;
        
        backgroundImage.raycastTarget = false;
        
        if (useNineSlice)
        {
            backgroundImage.type = backgroundImageType;
        }
    }
    
    private void SetupGridMask(Transform gridBackgroundContainer)
    {
        if (gridMask != null)
        {
            DestroyImmediate(gridMask);
        }
        
        RectMask2D existingBgMask = gridBackgroundContainer.GetComponent<RectMask2D>();
        if (existingBgMask != null)
        {
            DestroyImmediate(existingBgMask);
        }
        
        gridMask = gridBackgroundContainer.gameObject.AddComponent<RectMask2D>();
    }
    
    public void ClearLayout(Transform gridBackgroundContainer)
    {
        if (gridMask != null)
        {
            DestroyImmediate(gridMask);
            gridMask = null;
        }
 
        RectMask2D bgMask = gridBackgroundContainer.GetComponent<RectMask2D>();
        if (bgMask != null)
        {
            DestroyImmediate(bgMask);
        }
    }
} 