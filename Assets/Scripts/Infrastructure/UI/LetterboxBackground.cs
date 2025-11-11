using UnityEngine;
using UnityEngine.UI;

public class LetterboxBackground : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Canvas backgroundCanvas;

    private void Awake()
    {
        if (backgroundCanvas == null)
        {
            backgroundCanvas = GetComponent<Canvas>();
        }

        if (backgroundCanvas != null)
        {
            backgroundCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            backgroundCanvas.sortingOrder = -1;

            CanvasScaler scaler = backgroundCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = backgroundCanvas.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Screen.width, Screen.height);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponentInChildren<Image>();
        }

        if (backgroundImage != null)
        {
            RectTransform rectTransform = backgroundImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private void Update()
    {
        if (backgroundCanvas != null && backgroundImage != null)
        {
            RectTransform canvasRect = backgroundCanvas.GetComponent<RectTransform>();
            RectTransform imageRect = backgroundImage.rectTransform;

            if (canvasRect != null && imageRect != null)
            {
                imageRect.sizeDelta = new Vector2(Screen.width, Screen.height);
            }
        }
    }
}



