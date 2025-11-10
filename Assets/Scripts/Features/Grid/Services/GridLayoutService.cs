using UnityEngine;
using UnityEngine.UI;

public class GridLayoutService
{
    private float cellSpacing;
    private float paddingLeft;
    private float paddingRight;
    private float paddingTop;
    private float paddingBottom;
    private float bufferRowGap;
    private float collectionRowOffset;
    private int maxGridWidth;
    private int maxGridHeight;
    private float calculatedCellWidth;
    private float calculatedCellHeight;

    public float CalculatedCellWidth => calculatedCellWidth;
    public float CalculatedCellHeight => calculatedCellHeight;

    public void Initialize(float spacing, float padLeft, float padRight, float padTop, float padBottom, float gap, int maxWidth, int maxHeight)
    {
        cellSpacing = spacing;
        paddingLeft = padLeft;
        paddingRight = padRight;
        paddingTop = padTop;
        paddingBottom = padBottom;
        bufferRowGap = gap;
        collectionRowOffset = 0f;
        maxGridWidth = maxWidth;
        maxGridHeight = maxHeight;
    }

    public void SetCollectionRowOffset(float offset)
    {
        collectionRowOffset = offset;
    }

    public void CalculateCellSize(Transform gridContainer, int gridWidth, int gridHeight)
    {
        Canvas canvas = gridContainer.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        
        float availableWidth = canvasRect.sizeDelta.x - paddingLeft - paddingRight;
        float availableHeight = canvasRect.sizeDelta.y - paddingTop - paddingBottom;
        
        float maxCellWidth = (availableWidth - (maxGridWidth - 1) * cellSpacing) / maxGridWidth;
        float maxCellHeight = (availableHeight - (maxGridHeight - 1) * cellSpacing) / maxGridHeight;
        
        float aspectRatio = 142f / 142f;
        
        if (maxCellWidth / aspectRatio <= maxCellHeight)
        {
            calculatedCellWidth = maxCellWidth;
            calculatedCellHeight = maxCellWidth / aspectRatio;
        }
        else
        {
            calculatedCellHeight = maxCellHeight;
            calculatedCellWidth = maxCellHeight * aspectRatio;
        }
    }

    public void PositionCell(RectTransform cellRect, int x, int y, int bufferRows, int collectionRowStart)
    {
        cellRect.anchorMin = new Vector2(0, 1);
        cellRect.anchorMax = new Vector2(0, 1);
        
        float gapOffset = (y < bufferRows) ? bufferRowGap : 0f;
        bool isCollectionRow = collectionRowStart >= 0 && y >= collectionRowStart;
        float collectionOffset = isCollectionRow ? collectionRowOffset : 0f;
        float yOffset = (y - bufferRows) * (calculatedCellHeight + cellSpacing) + gapOffset + collectionOffset;
        
        cellRect.anchoredPosition = new Vector2(
            x * (calculatedCellWidth + cellSpacing) + calculatedCellWidth * 0.5f,
            -yOffset - calculatedCellHeight * 0.5f
        );
        cellRect.sizeDelta = new Vector2(calculatedCellWidth, calculatedCellHeight);
    }

    public void SetupGridMask(Transform gridContainer, int gridHeight)
    {
        RectMask2D gridMask = gridContainer.gameObject.GetComponent<RectMask2D>();
        if (gridMask == null)
        {
            gridMask = gridContainer.gameObject.AddComponent<RectMask2D>();
        }
        
        RectTransform containerRect = gridContainer.GetComponent<RectTransform>();
        float visibleHeight = gridHeight * calculatedCellHeight + (gridHeight - 1) * cellSpacing;
        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, visibleHeight);
    }

    public void CenterGrid(Transform gridContainer, int gridWidth, int gridHeight)
    {
        RectTransform gridRect = gridContainer.GetComponent<RectTransform>();
        float totalWidth = gridWidth * calculatedCellWidth + (gridWidth - 1) * cellSpacing;
        float totalHeight = gridHeight * calculatedCellHeight + (gridHeight - 1) * cellSpacing;
        
        gridRect.sizeDelta = new Vector2(totalWidth, totalHeight);
        
        Canvas canvas = gridContainer.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float availableWidth = canvasRect.sizeDelta.x - paddingLeft - paddingRight;
        float availableHeight = canvasRect.sizeDelta.y - paddingTop - paddingBottom;
        
        float centerX = paddingLeft + availableWidth * 0.5f - canvasRect.sizeDelta.x * 0.5f;
        float centerY = -paddingTop - availableHeight * 0.5f + canvasRect.sizeDelta.y * 0.5f;
        
        gridRect.anchoredPosition = new Vector2(centerX, centerY);
    }
}

