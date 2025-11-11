using UnityEngine;
using UnityEngine.UI;

public class LetterboxManager : MonoBehaviour
{
    public static LetterboxManager Instance { get; private set; }

    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Canvas gameplayCanvas;
    
    private const float TargetAspectRatio = 9f / 16f;
    private Rect lastViewportRect = new Rect(0, 0, 1, 1);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupLetterbox();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupLetterbox();
    }

    private void Update()
    {
        SetupLetterbox();
    }

    private void SetupLetterbox()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null) return;

        float currentAspect = (float)Screen.width / (float)Screen.height;
        float targetAspect = TargetAspectRatio;

        Rect viewportRect = new Rect(0, 0, 1, 1);

        if (currentAspect > targetAspect)
        {
            float scaleHeight = currentAspect / targetAspect;
            float scaleWidth = 1f;

            float normalizedWidth = scaleWidth / scaleHeight;
            float normalizedX = (1f - normalizedWidth) / 2f;

            viewportRect = new Rect(normalizedX, 0, normalizedWidth, 1);
        }
        else
        {
            float scaleWidth = targetAspect / currentAspect;
            float scaleHeight = 1f;

            float normalizedHeight = scaleHeight / scaleWidth;
            float normalizedY = (1f - normalizedHeight) / 2f;

            viewportRect = new Rect(0, normalizedY, 1, normalizedHeight);
        }

        if (viewportRect != lastViewportRect)
        {
            gameplayCamera.rect = viewportRect;
            lastViewportRect = viewportRect;

            if (gameplayCanvas != null)
            {
                CanvasScaler scaler = gameplayCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920);
                    scaler.matchWidthOrHeight = 1f;
                }
            }
        }
    }

    public Rect GetGameplayViewportRect()
    {
        return gameplayCamera != null ? gameplayCamera.rect : new Rect(0, 0, 1, 1);
    }

    public Vector2 GetGameplayScreenSize()
    {
        Rect viewport = GetGameplayViewportRect();
        return new Vector2(Screen.width * viewport.width, Screen.height * viewport.height);
    }

    public Vector2 GetGameplayScreenPosition()
    {
        Rect viewport = GetGameplayViewportRect();
        return new Vector2(Screen.width * viewport.x, Screen.height * viewport.y);
    }

    public bool IsPointInGameplayArea(Vector2 screenPoint)
    {
        Rect viewport = GetGameplayViewportRect();
        Vector2 gameplayPos = GetGameplayScreenPosition();
        Vector2 gameplaySize = GetGameplayScreenSize();

        return screenPoint.x >= gameplayPos.x && 
               screenPoint.x <= gameplayPos.x + gameplaySize.x &&
               screenPoint.y >= gameplayPos.y && 
               screenPoint.y <= gameplayPos.y + gameplaySize.y;
    }

    public Vector2 ScreenToGameplayPoint(Vector2 screenPoint)
    {
        Rect viewport = GetGameplayViewportRect();
        Vector2 gameplayPos = GetGameplayScreenPosition();
        
        return new Vector2(
            screenPoint.x - gameplayPos.x,
            screenPoint.y - gameplayPos.y
        );
    }

    public Vector3 ScreenToGameplayWorldPoint(Vector2 screenPoint, float worldZ = 0f)
    {
        if (gameplayCamera == null) return Vector3.zero;
        
        Vector2 gameplayPoint = ScreenToGameplayPoint(screenPoint);
        Vector2 gameplaySize = GetGameplayScreenSize();
        
        Vector2 normalizedPoint = new Vector2(
            gameplayPoint.x / gameplaySize.x,
            gameplayPoint.y / gameplaySize.y
        );
        
        return gameplayCamera.ViewportToWorldPoint(new Vector3(normalizedPoint.x, normalizedPoint.y, worldZ));
    }
}

