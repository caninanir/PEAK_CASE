using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoalCollectionAnimationService : MonoBehaviour
{
    public static GoalCollectionAnimationService Instance { get; private set; }

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private AnimationCurve positionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f);
    [SerializeField] private float rotationAmount = 15f;
    [SerializeField] private float particleBurstDelay = 0.1f;

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem goalCollectBurstPrefab;

    [Header("Animation Container")]
    [SerializeField] private Transform goalCollectionAnimationsContainer;

    private GridController gridController;
    private GameplayUIController gameplayUIController;
    private Canvas uiCanvas;
    private bool isInitialized;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeReferences();
        StartCoroutine(DelayedInitializationCheck());
    }

    private IEnumerator DelayedInitializationCheck()
    {
        yield return new WaitForSeconds(1f);
        isInitialized = false;
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        if (isInitialized) return;

        if (gridController == null)
        {
            gridController = FindFirstObjectByType<GridController>();
        }

        if (gameplayUIController == null)
        {
            gameplayUIController = FindFirstObjectByType<GameplayUIController>();
        }
        
        if (gameplayUIController != null && uiCanvas == null)
        {
            Canvas gameplayCanvas = gameplayUIController.GetComponentInParent<Canvas>();
            if (gameplayCanvas != null)
            {
                uiCanvas = gameplayCanvas;
            }
        }
        
        if (uiCanvas == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    uiCanvas = canvas;
                    break;
                }
            }
            
            if (uiCanvas == null && canvases.Length > 0)
            {
                uiCanvas = canvases[0];
            }
        }

        isInitialized = gridController != null && gameplayUIController != null && goalCollectionAnimationsContainer != null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AnimateGoalCollection(ItemType itemType, Vector2Int gridPosition, bool isDuck = false)
    {
        InitializeReferences();

        if (gridController == null || gameplayUIController == null || goalCollectionAnimationsContainer == null)
        {
            return;
        }

        GoalItem goalItem = GetGoalItem(itemType);
        if (goalItem == null) return;

        Vector2 startWorldPos = GetStartPosition(gridPosition, isDuck);
        Vector2 targetWorldPos = GetGoalIconPosition(goalItem);

        StartCoroutine(AnimateCollection(startWorldPos, targetWorldPos, itemType, goalItem, gridPosition));
    }

    private GoalItem GetGoalItem(ItemType itemType)
    {
        if (gameplayUIController == null) return null;
        
        var goalItems = gameplayUIController.GetGoalItems();
        if (goalItems.ContainsKey(itemType))
        {
            return goalItems[itemType];
        }
        
        return null;
    }

    private Vector2 GetStartPosition(Vector2Int gridPosition, bool isDuck)
    {
        if (isDuck)
        {
            return GetDuckCollectionPosition(gridPosition);
        }

        GridCell cell = gridController.GetCell(gridPosition.x, gridPosition.y);
        if (cell != null)
        {
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            if (cellRect != null)
            {
                return GetScreenPosition(cellRect);
            }
        }

        return Vector2.zero;
    }

    private Vector2 GetDuckCollectionPosition(Vector2Int gridPosition)
    {
        int collectionRowStart = gridController.DataService.BufferRows + gridController.DataService.GridHeight;
        GridCell collectionCell = gridController.DataService.GetExtendedCell(gridPosition.x, collectionRowStart);
        
        if (collectionCell != null)
        {
            RectTransform cellRect = collectionCell.GetComponent<RectTransform>();
            if (cellRect != null)
            {
                return GetScreenPosition(cellRect);
            }
        }

        GridCell visibleCell = gridController.GetCell(gridPosition.x, gridController.DataService.GridHeight - 1);
        if (visibleCell != null)
        {
            RectTransform cellRect = visibleCell.GetComponent<RectTransform>();
            if (cellRect != null)
            {
                Vector2 pos = GetScreenPosition(cellRect);
                pos.y -= 100f;
                return pos;
            }
        }

        return Vector2.zero;
    }

    private Vector2 GetGoalIconPosition(GoalItem goalItem)
    {
        if (goalItem == null) return Vector2.zero;
        
        RectTransform goalRect = goalItem.GetComponent<RectTransform>();
        if (goalRect != null)
        {
            return GetScreenPosition(goalRect);
        }
        
        return Vector2.zero;
    }

    private Vector2 GetScreenPosition(RectTransform rectTransform)
    {
        if (rectTransform == null) return Vector2.zero;
        
        Canvas sourceCanvas = rectTransform.GetComponentInParent<Canvas>();
        if (sourceCanvas != null)
        {
            if (sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return rectTransform.position;
            }
            else if (sourceCanvas.renderMode == RenderMode.ScreenSpaceCamera && sourceCanvas.worldCamera != null)
            {
                return RectTransformUtility.WorldToScreenPoint(sourceCanvas.worldCamera, rectTransform.position);
            }
        }
        
        Camera cam = Camera.main;
        if (cam != null)
        {
            return RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);
        }
        
        return rectTransform.position;
    }

    private IEnumerator AnimateCollection(Vector2 startPos, Vector2 targetPos, ItemType itemType, GoalItem goalItem, Vector2Int gridPosition)
    {
        GameObject flyingSprite = CreateFlyingSprite(itemType, startPos);
        if (flyingSprite == null) yield break;

        RectTransform flyingRect = flyingSprite.GetComponent<RectTransform>();
        Image flyingImage = flyingSprite.GetComponent<Image>();
        
        Vector2 startScreenPos = startPos;
        Vector2 targetScreenPos = targetPos;
        Vector3 startScale = Vector3.one;
        
        RectTransform goalRect = goalItem.GetComponent<RectTransform>();
        float targetScale = goalRect != null ? Mathf.Min(goalRect.sizeDelta.x, goalRect.sizeDelta.y) / 100f : 0.3f;
        Vector3 endScale = Vector3.one * targetScale;

        float elapsed = 0f;
        float rotation = Random.Range(-rotationAmount, rotationAmount);

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            
            float curveT = positionCurve.Evaluate(t);
            float scaleT = scaleCurve.Evaluate(t);

            Vector2 currentPos = Vector2.Lerp(startScreenPos, targetScreenPos, curveT);
            Vector3 currentScale = Vector3.Lerp(startScale, endScale, scaleT);

            if (flyingRect != null)
            {
                if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    flyingRect.position = currentPos;
                }
                else
                {
                    Vector3 worldPos;
                    if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceCamera && uiCanvas.worldCamera != null)
                    {
                        worldPos = uiCanvas.worldCamera.ScreenToWorldPoint(new Vector3(currentPos.x, currentPos.y, uiCanvas.planeDistance));
                    }
                    else
                    {
                        Camera mainCam = Camera.main;
                        if (mainCam != null)
                        {
                            worldPos = mainCam.ScreenToWorldPoint(new Vector3(currentPos.x, currentPos.y, 10f));
                        }
                        else
                        {
                            worldPos = currentPos;
                        }
                    }
                    flyingRect.position = worldPos;
                }
                flyingRect.localScale = currentScale;
                flyingRect.rotation = Quaternion.Euler(0, 0, rotation * (1f - t));
            }

            yield return null;
        }

        if (flyingRect != null)
        {
            if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                flyingRect.position = targetScreenPos;
            }
            else
            {
                Vector3 worldPos;
                if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceCamera && uiCanvas.worldCamera != null)
                {
                    worldPos = uiCanvas.worldCamera.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, uiCanvas.planeDistance));
                }
                else
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        worldPos = mainCam.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, 10f));
                    }
                    else
                    {
                        worldPos = targetScreenPos;
                    }
                }
                flyingRect.position = worldPos;
            }
            flyingRect.localScale = endScale;
        }

        yield return new WaitForSeconds(particleBurstDelay);

        SpawnParticleBurst(targetScreenPos, itemType);

        yield return new WaitForSeconds(0.2f);

        if (flyingSprite != null)
        {
            Destroy(flyingSprite);
        }

        EventBus.Publish(new GoalCollectionAnimationCompleteEvent
        {
            ItemType = itemType,
            GridX = gridPosition.x,
            GridY = gridPosition.y
        });
    }

    private GameObject CreateFlyingSprite(ItemType itemType, Vector2 screenPosition)
    {
        Sprite sprite = GetSpriteForItem(itemType);
        if (sprite == null || goalCollectionAnimationsContainer == null) return null;

        GameObject spriteObj = new GameObject($"FlyingGoal_{itemType}");
        spriteObj.transform.SetParent(goalCollectionAnimationsContainer, false);

        RectTransform rectTransform = spriteObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);
        
        if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = screenPosition;
        }
        else
        {
            Vector3 worldPos;
            if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceCamera && uiCanvas.worldCamera != null)
            {
                worldPos = uiCanvas.worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, uiCanvas.planeDistance));
            }
            else
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
                }
                else
                {
                    worldPos = screenPosition;
                }
            }
            rectTransform.position = worldPos;
        }

        Image image = spriteObj.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;

        return spriteObj;
    }

    private Sprite GetSpriteForItem(ItemType itemType)
    {
        if (gameplayUIController == null) return null;

        GoalItem goalItem = GetGoalItem(itemType);
        if (goalItem != null)
        {
            Image goalImage = goalItem.GetComponent<Image>();
            if (goalImage != null && goalImage.sprite != null)
            {
                return goalImage.sprite;
            }
        }

        if (gridController == null) return null;

        BaseItem sampleItem = PoolManager.Instance.GetItem(itemType, null);
        if (sampleItem != null)
        {
            sampleItem.Initialize(itemType);
            Sprite sprite = sampleItem.GetSprite();
            PoolManager.Instance.ReturnItem(sampleItem);
            return sprite;
        }

        return null;
    }

    private void SpawnParticleBurst(Vector2 screenPosition, ItemType itemType)
    {
        if (ParticleEffectManager.Instance == null) return;

        Vector3 worldPos;
        if (uiCanvas != null && uiCanvas.renderMode == RenderMode.ScreenSpaceCamera && uiCanvas.worldCamera != null)
        {
            worldPos = uiCanvas.worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, uiCanvas.planeDistance));
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            }
            else
            {
                worldPos = screenPosition;
            }
        }

        if (goalCollectBurstPrefab != null)
        {
            ParticleSystem burst = Instantiate(goalCollectBurstPrefab, worldPos, Quaternion.identity, ParticleEffectManager.Instance.ParticleContainer);
            burst.Play();
            StartCoroutine(DestroyParticleAfterDelay(burst, 2f));
        }
    }

    private IEnumerator DestroyParticleAfterDelay(ParticleSystem particle, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (particle != null)
        {
            Destroy(particle.gameObject);
        }
    }
}

