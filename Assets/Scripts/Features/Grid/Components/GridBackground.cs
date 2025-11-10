using UnityEngine;
using UnityEngine.UI;

public class GridBackground : MonoBehaviour
{
    [Header("Background Settings")]
    [SerializeField] [Range(1.0f, 2.0f)] private float backgroundScale = 1.1f;
    [SerializeField] private Vector2 additionalOffset = Vector2.zero;
    [SerializeField] private float backgroundExtraHeight = 0f;
    
    [Header("Nine Slice Settings")]
    [SerializeField] private bool useNineSlice = true;
    [SerializeField] private Image.Type imageType = Image.Type.Sliced;
    
    private Image backgroundImage;
    private RectTransform rectTransform;
    private Transform gridContainer;
    
    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        
        if (useNineSlice)
        {
            backgroundImage.type = imageType;
        }
    }
    
    public void InitializeBackground(Transform container)
    {
        gridContainer = container;
    }
    
    public void UpdateBackgroundTransform()
    {
        RectTransform containerRect = gridContainer.GetComponent<RectTransform>();
        
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        rectTransform.anchoredPosition = containerRect.anchoredPosition + additionalOffset;
        
        Vector2 scaledSize = containerRect.sizeDelta * backgroundScale;
        rectTransform.sizeDelta = new Vector2(scaledSize.x, scaledSize.y + backgroundExtraHeight);
        
        transform.SetSiblingIndex(0);
        
        backgroundImage.raycastTarget = false;
    }
} 