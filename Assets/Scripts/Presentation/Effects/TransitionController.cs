using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class TransitionController : MonoBehaviour
{
    public static TransitionController Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private float fadeHoldDuration = 0.2f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Visual Effects")]
    [SerializeField] private bool useRadialFade = true;
    [SerializeField] private bool useScaleEffect = true;
    [SerializeField] private Vector2 radialCenter = new Vector2(0.5f, 0.5f);

    private GameObject fadeCanvasGO;
    private Canvas fadeCanvas;
    private Image fadeImage;
    private CanvasGroup fadeCanvasGroup;
    private Material fadeMaterial;
    private bool isFading = false;
    private Coroutine currentFadeCoroutine;

    public static UnityEvent OnFadeInComplete = new UnityEvent();
    public static UnityEvent OnFadeOutComplete = new UnityEvent();

    public bool IsFading => isFading;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CreateFadeMaterial();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (fadeMaterial != null)
        {
            Destroy(fadeMaterial);
        }

        OnFadeInComplete.RemoveAllListeners();
        OnFadeOutComplete.RemoveAllListeners();
    }

    private void CreateFadeMaterial()
    {
        Shader fadeShader = Shader.Find("UI/Default");
        if (useRadialFade)
        {
            Shader radialShader = Shader.Find("UI/RadialFade");
            if (radialShader != null)
                fadeShader = radialShader;
        }

        fadeMaterial = new Material(fadeShader);

        if (fadeMaterial.HasProperty("_Center"))
        {
            fadeMaterial.SetVector("_Center", new Vector4(radialCenter.x, radialCenter.y, 0, 0));
        }
        if (fadeMaterial.HasProperty("_Smoothness"))
        {
            fadeMaterial.SetFloat("_Smoothness", 0.02f);
        }
        if (fadeMaterial.HasProperty("_Feather"))
        {
            fadeMaterial.SetFloat("_Feather", 0.1f);
        }
    }

    public void FadeIn(System.Action onComplete = null, float? customDuration = null)
    {
        if (isFading && currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        float duration = customDuration ?? fadeInDuration;
        currentFadeCoroutine = StartCoroutine(FadeInCoroutine(duration, onComplete));
    }

    public void FadeOut(System.Action onComplete = null, float? customDuration = null)
    {
        if (isFading && currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        float duration = customDuration ?? fadeOutDuration;
        currentFadeCoroutine = StartCoroutine(FadeOutCoroutine(duration, onComplete));
    }

    private IEnumerator FadeInCoroutine(float duration, System.Action onComplete)
    {
        isFading = true;
        CreateFadeCanvas();

        if (useRadialFade)
        {
            fadeCanvasGroup.alpha = 1f;
        }
        else
        {
            fadeCanvasGroup.alpha = 1f;
        }
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = true;

        if (useScaleEffect)
        {
            fadeCanvasGO.transform.localScale = Vector3.one;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float curveValue = 1f - fadeCurve.Evaluate(t);

            if (useRadialFade)
            {
                fadeCanvasGroup.alpha = 1f;
                if (fadeMaterial.HasProperty("_RadialProgress"))
                {
                    fadeMaterial.SetFloat("_RadialProgress", curveValue);
                }
            }
            else
            {
                fadeCanvasGroup.alpha = curveValue;
            }

            if (useScaleEffect)
            {
                float scale = Mathf.Lerp(1f, 0.8f, t);
                fadeCanvasGO.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        DestroyFadeCanvas();

        isFading = false;
        onComplete?.Invoke();
        OnFadeInComplete.Invoke();
    }

    private IEnumerator FadeOutCoroutine(float duration, System.Action onComplete)
    {
        isFading = true;
        CreateFadeCanvas();

        if (useRadialFade)
        {
            fadeCanvasGroup.alpha = 1f;
        }
        else
        {
            fadeCanvasGroup.alpha = 0f;
        }
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = true;

        if (useScaleEffect)
        {
            fadeCanvasGO.transform.localScale = Vector3.one * 1.2f;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float curveValue = fadeCurve.Evaluate(t);

            if (useRadialFade)
            {
                fadeCanvasGroup.alpha = 1f;
                if (fadeMaterial.HasProperty("_RadialProgress"))
                {
                    fadeMaterial.SetFloat("_RadialProgress", curveValue);
                }
            }
            else
            {
                fadeCanvasGroup.alpha = curveValue;
            }

            if (useScaleEffect)
            {
                float scale = Mathf.Lerp(1.2f, 1f, curveValue);
                fadeCanvasGO.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        if (useRadialFade)
        {
            fadeCanvasGroup.alpha = 1f;
            if (fadeMaterial.HasProperty("_RadialProgress"))
            {
                fadeMaterial.SetFloat("_RadialProgress", 1f);
            }
        }
        else
        {
            fadeCanvasGroup.alpha = 1f;
        }
        if (useScaleEffect)
        {
            fadeCanvasGO.transform.localScale = Vector3.one;
        }

        yield return new WaitForSecondsRealtime(fadeHoldDuration);

        isFading = false;
        onComplete?.Invoke();
        OnFadeOutComplete.Invoke();
    }

    private void CreateFadeCanvas()
    {
        if (fadeCanvasGO != null) return;

        fadeCanvasGO = new GameObject("FadeTransitionCanvas");
        fadeCanvasGO.transform.SetParent(transform);

        fadeCanvas = fadeCanvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;

        CanvasScaler scaler = fadeCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = fadeCanvasGO.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        fadeCanvasGroup = fadeCanvasGO.AddComponent<CanvasGroup>();

        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(fadeCanvasGO.transform, false);

        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.material = fadeMaterial;
        fadeImage.raycastTarget = false;

        RectTransform rectTransform = fadeImage.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        if (useRadialFade && fadeMaterial != null)
        {
            if (fadeMaterial.HasProperty("_RadialProgress"))
            {
                fadeMaterial.SetFloat("_RadialProgress", 0.0f);
            }
            if (fadeMaterial.HasProperty("_Center"))
            {
                fadeMaterial.SetVector("_Center", new Vector4(radialCenter.x, radialCenter.y, 0, 0));
            }
            if (fadeImage != null)
            {
                fadeImage.color = Color.black;
            }
        }
    }

    private void DestroyFadeCanvas()
    {
        if (fadeCanvasGO != null)
        {
            Destroy(fadeCanvasGO);
            fadeCanvasGO = null;
            fadeCanvas = null;
            fadeImage = null;
            fadeCanvasGroup = null;
        }
    }

    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (fadeImage != null)
        {
            fadeImage.color = fadeColor;
        }
    }

    public void SetFadeDuration(float fadeOut, float fadeIn)
    {
        fadeOutDuration = fadeOut;
        fadeInDuration = fadeIn;
    }

    public void QuickFadeOut(System.Action onComplete = null)
    {
        FadeOut(onComplete, 0.3f);
    }

    public void QuickFadeIn(System.Action onComplete = null)
    {
        FadeIn(onComplete, 0.4f);
    }
}

