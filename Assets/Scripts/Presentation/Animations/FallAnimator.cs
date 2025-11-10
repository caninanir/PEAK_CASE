using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FallAnimator : MonoBehaviour
{
    [Header("Physics-Based Fall Settings")]
    [SerializeField] public AnimationCurve gravityCurve = new AnimationCurve();
    
    [Header("Landing Effects")]
    [SerializeField] private bool enableLandingBounce = true;
    [SerializeField] private float bounceHeight = 0.05f;
    [SerializeField] private float bounceDuration = 0.12f;
    
    [Header("Stretch Effects")]
    [SerializeField] private bool enableStretch = true;
    [SerializeField] private float fallStretchIntensity = 0.15f;
    [SerializeField] private float landingStretchIntensity = 0.2f;
    [SerializeField] private float landingStretchDuration = 0.15f;
    
    [Header("Visual Polish")]
    [SerializeField] public bool enableSubtleRotation = false;
    [SerializeField] public float maxRotationAngle = 2f;

    private HashSet<int> soundPlayedColumns = new HashSet<int>();
    private HashSet<FallOperation> completedOperations = new HashSet<FallOperation>();
    private Dictionary<FallOperation, Vector2> originalScales = new Dictionary<FallOperation, Vector2>();
    private HashSet<FallOperation> animatingLandingEffects = new HashSet<FallOperation>();

    private void Awake()
    {
        SetupDefaultGravityCurve();
    }

    private void SetupDefaultGravityCurve()
    {
        gravityCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 1.2f),
            new Keyframe(0.3f, 0.15f, 1.8f, 2.2f),
            new Keyframe(0.7f, 0.6f, 3f, 2.8f),
            new Keyframe(1f, 1f, 1.8f, 0f)
        );
        
        for (int i = 0; i < gravityCurve.keys.Length; i++)
        {
            gravityCurve.SmoothTangents(i, 0.3f);
        }
    }

    public IEnumerator AnimateFalls(List<FallOperation> operations, GridController gridController = null)
    {
        soundPlayedColumns.Clear();
        
        if (operations.Count == 0)
        {
            yield break;
        }
        
        float maxDuration = operations.Max(op => op.duration);
        float elapsed = 0f;
        
        while (elapsed < maxDuration)
        {
            elapsed += Time.deltaTime;
            
            if (gridController != null)
            {
                gridController.RepopulateBufferRow();
            }
            
            foreach (var op in operations)
            {
                if (op.itemTransform == null) continue;
                
                float normalizedTime = Mathf.Clamp01(elapsed / op.duration);
                float curveValue = gravityCurve.Evaluate(normalizedTime);
                
                Vector2 currentPosition = Vector2.Lerp(op.startPosition, op.targetPosition, curveValue);
                op.itemTransform.anchoredPosition = currentPosition;
                
                if (enableSubtleRotation && op.targetRotation != op.startRotation)
                {
                    op.itemTransform.rotation = Quaternion.Lerp(
                        op.startRotation, 
                        op.targetRotation, 
                        normalizedTime * 0.5f
                    );
                }
                
                if (normalizedTime >= 0.95f && op.fallDistance >= 1 && 
                    op.item != null && op.item.currentCell != null &&
                    !soundPlayedColumns.Contains(op.item.currentCell.x))
                {
                    soundPlayedColumns.Add(op.item.currentCell.x);
                    AudioManager.Instance.PlayCubeFallSound();
                }
            }
            
            yield return null;
        }
        
        foreach (var op in operations)
        {
            if (op.itemTransform != null)
            {
                op.itemTransform.anchoredPosition = op.targetPosition;
                if (enableSubtleRotation)
                {
                    op.itemTransform.rotation = op.startRotation;
                }
                
                if (enableLandingBounce && op.fallDistance >= 2)
                {
                    StartCoroutine(ApplyLandingBounce(op));
                }
            }
        }
    }

    public IEnumerator AnimateFallsWithContinuousProcessing(
        List<FallOperation> operations, 
        Dictionary<FallOperation, float> operationStartTimes,
        System.Func<List<FallOperation>> collectNewFallsCallback,
        System.Action bufferRowSpawnCallback = null)
    {
        soundPlayedColumns.Clear();
        completedOperations.Clear();
        originalScales.Clear();
        animatingLandingEffects.Clear();
        
        foreach (var op in operations)
        {
            if (op.itemTransform != null)
            {
                originalScales[op] = op.itemTransform.localScale;
            }
        }
        
        if (operations.Count == 0)
        {
            yield break;
        }
        
        float maxDuration = operations.Max(op => op.duration);
        float elapsed = 0f;
        
        while (elapsed < maxDuration || operations.Count > operationStartTimes.Count)
        {
            elapsed += Time.deltaTime;
            
            List<FallOperation> newFalls = collectNewFallsCallback?.Invoke() ?? new List<FallOperation>();
            if (newFalls.Count > 0)
            {
                foreach (var op in newFalls)
                {
                    operations.Add(op);
                    operationStartTimes[op] = elapsed;
                    if (op.itemTransform != null)
                    {
                        originalScales[op] = op.itemTransform.localScale;
                    }
                    float opEndTime = elapsed + op.duration;
                    if (opEndTime > maxDuration)
                    {
                        maxDuration = opEndTime;
                    }
                }
            }
            
            bufferRowSpawnCallback?.Invoke();
            
            foreach (var op in operations)
            {
                if (op.itemTransform == null) continue;
                if (!operationStartTimes.ContainsKey(op)) continue;
                
                float opElapsed = elapsed - operationStartTimes[op];
                float normalizedTime = Mathf.Clamp01(opElapsed / op.duration);
                
                if (normalizedTime >= 1f && !completedOperations.Contains(op))
                {
                    completedOperations.Add(op);
                    op.itemTransform.anchoredPosition = op.targetPosition;
                    if (enableSubtleRotation)
                    {
                        op.itemTransform.rotation = op.startRotation;
                    }
                    
                    if (enableLandingBounce && op.fallDistance >= 2)
                    {
                        animatingLandingEffects.Add(op);
                        StartCoroutine(ApplyLandingBounce(op));
                    }
                    else if (enableStretch && op.fallDistance >= 1)
                    {
                        animatingLandingEffects.Add(op);
                        StartCoroutine(ApplyLandingStretch(op));
                    }
                    else
                    {
                        ResetScale(op);
                    }
                    
                    if (op.fallDistance >= 1 && 
                        op.item != null && op.item.currentCell != null &&
                        !soundPlayedColumns.Contains(op.item.currentCell.x))
                    {
                        soundPlayedColumns.Add(op.item.currentCell.x);
                        AudioManager.Instance.PlayCubeFallSound();
                    }
                    continue;
                }
                
                if (normalizedTime >= 1f) continue;
                
                float curveValue = gravityCurve.Evaluate(normalizedTime);
                float curveDerivative = GetCurveDerivative(normalizedTime);
                
                Vector2 currentPosition = Vector2.Lerp(op.startPosition, op.targetPosition, curveValue);
                op.itemTransform.anchoredPosition = currentPosition;
                
                if (enableStretch)
                {
                    float speed = Mathf.Abs(curveDerivative);
                    float stretchY = 1f + (speed * fallStretchIntensity);
                    float stretchX = 1f / stretchY;
                    Vector2 originalScale = originalScales.ContainsKey(op) ? originalScales[op] : Vector2.one;
                    op.itemTransform.localScale = new Vector3(originalScale.x * stretchX, originalScale.y * stretchY, 1f);
                }
                
                if (enableSubtleRotation && op.targetRotation != op.startRotation)
                {
                    op.itemTransform.rotation = Quaternion.Lerp(
                        op.startRotation, 
                        op.targetRotation, 
                        normalizedTime * 0.5f
                    );
                }
            }
            
            yield return null;
        }
        
        foreach (var op in operations)
        {
            if (op.itemTransform != null && !completedOperations.Contains(op))
            {
                op.itemTransform.anchoredPosition = op.targetPosition;
                if (enableSubtleRotation)
                {
                    op.itemTransform.rotation = op.startRotation;
                }
                ResetScale(op);
            }
        }
        
        while (animatingLandingEffects.Count > 0)
        {
            yield return null;
        }
        
        completedOperations.Clear();
        originalScales.Clear();
        animatingLandingEffects.Clear();
    }

    private float GetCurveDerivative(float normalizedTime)
    {
        if (normalizedTime <= 0f || normalizedTime >= 1f) return 0f;
        
        float delta = 0.01f;
        float t1 = Mathf.Max(0f, normalizedTime - delta);
        float t2 = Mathf.Min(1f, normalizedTime + delta);
        float y1 = gravityCurve.Evaluate(t1);
        float y2 = gravityCurve.Evaluate(t2);
        return (y2 - y1) / (t2 - t1);
    }
    
    private void ResetScale(FallOperation op)
    {
        if (op.itemTransform != null && originalScales.ContainsKey(op))
        {
            Vector2 originalScale = originalScales[op];
            op.itemTransform.localScale = new Vector3(originalScale.x, originalScale.y, 1f);
        }
    }

    private IEnumerator ApplyLandingBounce(FallOperation operation)
    {
        if (operation.itemTransform == null)
        {
            animatingLandingEffects.Remove(operation);
            yield break;
        }
        
        Vector2 landingPosition = operation.targetPosition;
        
        float elapsed = 0f;
        
        while (elapsed < bounceDuration)
        {
            if (operation.itemTransform == null)
            {
                animatingLandingEffects.Remove(operation);
                yield break;
            }
            
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / bounceDuration);
            
            float bounceOffset = Mathf.Sin(normalizedTime * Mathf.PI) * bounceHeight * 100f * (1f - normalizedTime);
            Vector2 currentPosition = landingPosition + Vector2.up * bounceOffset;
            
            operation.itemTransform.anchoredPosition = currentPosition;
            yield return null;
        }
        
        if (operation.itemTransform != null)
        {
            operation.itemTransform.anchoredPosition = landingPosition;
        }
        
        if (enableStretch)
        {
            yield return StartCoroutine(ApplyLandingStretch(operation));
        }
        else
        {
            ResetScale(operation);
        }
        
        animatingLandingEffects.Remove(operation);
    }
    
    private IEnumerator ApplyLandingStretch(FallOperation operation)
    {
        if (operation.itemTransform == null)
        {
            animatingLandingEffects.Remove(operation);
            yield break;
        }
        if (!originalScales.ContainsKey(operation))
        {
            animatingLandingEffects.Remove(operation);
            yield break;
        }
        
        Vector2 originalScale = originalScales[operation];
        float elapsed = 0f;
        float duration = landingStretchDuration;
        float totalDuration = duration * 3f;
        
        while (elapsed < totalDuration)
        {
            if (operation.itemTransform == null)
            {
                animatingLandingEffects.Remove(operation);
                yield break;
            }
            
            elapsed += Time.deltaTime;
            float phase = elapsed / duration;
            Vector2 stretch = Vector2.one;
            
            if (phase < 1f)
            {
                float t = phase;
                float stretchX = 1f + Mathf.Sin(t * Mathf.PI) * landingStretchIntensity;
                float stretchY = 1f / stretchX;
                stretch = new Vector2(stretchX, stretchY);
            }
            else if (phase < 2f)
            {
                float t = phase - 1f;
                float stretchY = 1f + Mathf.Sin(t * Mathf.PI) * landingStretchIntensity;
                float stretchX = 1f / stretchY;
                stretch = new Vector2(stretchX, stretchY);
            }
            else if (phase < 3f)
            {
                float t = phase - 2f;
                float stretchX = 1f + Mathf.Sin(t * Mathf.PI) * landingStretchIntensity * 0.5f;
                float stretchY = 1f / stretchX;
                stretch = new Vector2(stretchX, stretchY);
            }
            else
            {
                stretch = Vector2.one;
            }
            
            operation.itemTransform.localScale = new Vector3(
                originalScale.x * stretch.x, 
                originalScale.y * stretch.y, 
                1f
            );
            
            yield return null;
        }
        
        if (operation.itemTransform != null)
        {
            operation.itemTransform.localScale = new Vector3(originalScale.x, originalScale.y, 1f);
        }
        
        animatingLandingEffects.Remove(operation);
    }
}

