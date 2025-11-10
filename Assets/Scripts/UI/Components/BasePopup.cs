using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class BasePopup : MonoBehaviour
{
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected Button closeButton;
    [SerializeField] protected float animationDuration = 0.3f;
    
    protected bool isVisible = false;

    protected virtual void Awake()
    {
        EnsureCanvasGroup();
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public virtual void Show()
    {
        if (isVisible) return;
        
        gameObject.SetActive(true);
        StartCoroutine(AnimateIn());
        OnShow();
    }

    public virtual void Hide()
    {
        if (!isVisible) return;
        
        StartCoroutine(AnimateOut());
        OnHide();
    }

    protected virtual void OnShow()
    {
    }

    protected virtual void OnHide()
    {
    }

    protected virtual IEnumerator AnimateIn()
    {
        isVisible = true;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
        canvasGroup.interactable = true;
    }

    protected virtual IEnumerator AnimateOut()
    {
        canvasGroup.interactable = false;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
        isVisible = false;
    }
} 