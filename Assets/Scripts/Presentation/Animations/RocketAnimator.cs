using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RocketAnimator : MonoBehaviour
{
    [Header("Rocket Creation Settings")]
    [Tooltip("How fast cubes gather together when creating a rocket (duration in seconds). Lower values = faster gathering.")]
    [SerializeField] [Range(0.1f, 2.0f)] private float creationAnimationDuration = 0.4f;

    public IEnumerator AnimateRocketCreation(List<GridCell> sourceGroup, GridCell targetCell, ItemType rocketType)
    {
        if (sourceGroup == null || targetCell == null || sourceGroup.Count == 0)
            yield break;

        SpawnParticlesAtOriginalPositions(sourceGroup);
        
        bool hasGoalCube = false;
        foreach (GridCell cell in sourceGroup)
        {
            if (cell.currentItem is CubeItem cube)
            {
                AudioManager.Instance.PlayCubeBreakSound();
                bool isGoalCube = ObstacleController.Instance != null && ObstacleController.Instance.IsGoalCube(cube.itemType);
                if (isGoalCube)
                {
                    hasGoalCube = true;
                }
            }
        }
        
        if (hasGoalCube)
        {
            AudioManager.Instance.PlayGoalCubeBreakSound();
        }
        
        var animatingObjects = CreateAnimatingCubes(sourceGroup);
        yield return AnimateToTarget(animatingObjects, targetCell);
        CleanupAnimatingCubes(animatingObjects);
        
        AudioManager.Instance.PlayRocketCreationSound();
        yield return new WaitForSeconds(0.1f);
    }

    public IEnumerator AnimateRocketComboMerge(List<RocketItem> sourceRockets, GridCell targetCell)
    {
        if (sourceRockets == null || sourceRockets.Count == 0 || targetCell == null)
            yield break;

        foreach (RocketItem rocket in sourceRockets)
        {
            if (rocket != null)
            {
                rocket.SetVisualActive(false);
            }
        }

        var animatingRockets = CreateAnimatingRockets(sourceRockets);
        if (animatingRockets.Count == 0)
        {
            foreach (RocketItem rocket in sourceRockets)
            {
                if (rocket != null)
                {
                    rocket.SetVisualActive(true);
                }
            }
            yield break;
        }

        yield return AnimateRocketsToTarget(animatingRockets, targetCell);

        foreach (var (clone, _) in animatingRockets)
        {
            if (clone != null)
            {
                Destroy(clone);
            }
        }
    }

    private void SpawnParticlesAtOriginalPositions(List<GridCell> sourceGroup)
    {
        foreach (GridCell sourceCell in sourceGroup)
        {
            if (sourceCell.currentItem is CubeItem cube)
            {
                RectTransform rectTransform = cube.GetComponent<RectTransform>();
                if (rectTransform != null && ParticleEffectManager.Instance != null)
                {
                    ParticleEffectManager.Instance.SpawnCubeBurst(rectTransform, cube.itemType);
                }
            }
        }
    }

    private List<(GameObject cube, CubeItem original)> CreateAnimatingCubes(List<GridCell> sourceGroup)
    {
        var animatingObjects = new List<(GameObject, CubeItem)>();

        foreach (GridCell sourceCell in sourceGroup)
        {
            if (sourceCell.currentItem is CubeItem cube)
            {
                cube.gameObject.SetActive(false);

                GameObject animCube = CreateAnimatingCube(cube, sourceCell);
                animatingObjects.Add((animCube, cube));
            }
        }

        return animatingObjects;
    }

    private GameObject CreateAnimatingCube(CubeItem originalCube, GridCell sourceCell)
    {
        GridController gridController = FindFirstObjectByType<GridController>();
        
        GameObject animCube = new GameObject("AnimatingCube");
        animCube.transform.SetParent(gridController.GridContainer, false);

        RectTransform animRect = animCube.AddComponent<RectTransform>();
        RectTransform sourceRect = sourceCell.GetComponent<RectTransform>();
        
        CopyRectTransformProperties(animRect, sourceRect);

        Image cubeImage = animCube.AddComponent<Image>();
        cubeImage.sprite = originalCube.GetSprite();
        cubeImage.raycastTarget = false;
        cubeImage.preserveAspect = true;

        animCube.transform.SetSiblingIndex(gridController.GridContainer.childCount - 1);
        return animCube;
    }

    private void CopyRectTransformProperties(RectTransform target, RectTransform source)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
    }

    private IEnumerator AnimateToTarget(List<(GameObject cube, CubeItem original)> animatingObjects, GridCell targetCell)
    {
        RectTransform targetRect = targetCell.GetComponent<RectTransform>();
        Vector2 targetPosition = targetRect.anchoredPosition;

        var startPositions = new List<Vector2>();
        foreach (var (cube, _) in animatingObjects)
        {
            RectTransform animRect = cube.GetComponent<RectTransform>();
            startPositions.Add(animRect.anchoredPosition);
        }

        float elapsed = 0f;
        while (elapsed < creationAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / creationAnimationDuration);

            for (int i = 0; i < animatingObjects.Count; i++)
            {
                if (animatingObjects[i].cube != null)
                {
                    RectTransform animRect = animatingObjects[i].cube.GetComponent<RectTransform>();
                    animRect.anchoredPosition = Vector2.Lerp(startPositions[i], targetPosition, t);
                    
                    float scale = 1f + (0.2f * Mathf.Sin(t * Mathf.PI));
                    animRect.localScale = Vector3.one * scale;
                }
            }

            yield return null;
        }
    }

    private void CleanupAnimatingCubes(List<(GameObject cube, CubeItem original)> animatingObjects)
    {
        GridController gridController = FindFirstObjectByType<GridController>();
        
        foreach (var (cube, original) in animatingObjects)
        {
            if (cube != null) Destroy(cube);
            
            if (original != null)
            {
                if (ObstacleController.Instance != null && ObstacleController.Instance.IsGoalCube(original.itemType))
                {
                    int visibleY = original.currentCell != null ? original.currentCell.y - gridController.BufferRows : 0;
                    EventBus.Publish(new ItemDestroyedEvent
                    {
                        GridX = original.currentCell != null ? original.currentCell.x : 0,
                        GridY = visibleY,
                        ItemType = original.itemType
                    });
                }
                
                Destroy(original.gameObject);
                original.currentCell?.RemoveItem();
            }
        }
    }

    private List<(GameObject clone, RocketItem original)> CreateAnimatingRockets(List<RocketItem> sourceRockets)
    {
        var animatingObjects = new List<(GameObject, RocketItem)>();
        GridController gridController = GridController.Instance;
        if (gridController == null)
        {
            return animatingObjects;
        }

        foreach (RocketItem rocket in sourceRockets)
        {
            if (rocket?.currentCell == null) continue;

            GameObject clone = CreateAnimatingRocket(rocket, gridController);
            if (clone != null)
            {
                animatingObjects.Add((clone, rocket));
            }
        }

        return animatingObjects;
    }

    private GameObject CreateAnimatingRocket(RocketItem rocket, GridController gridController)
    {
        GameObject animRocket = new GameObject("AnimatingRocket");
        animRocket.transform.SetParent(gridController.GridContainer, false);

        RectTransform animRect = animRocket.AddComponent<RectTransform>();
        RectTransform sourceRect = rocket.GetComponent<RectTransform>();

        CopyRectTransformProperties(animRect, sourceRect);
        animRect.localScale = sourceRect.localScale;
        animRect.localRotation = rocket.transform.localRotation;

        Image sourcePrimary = rocket.GetPrimaryImage();
        if (sourcePrimary != null)
        {
            Image animPrimary = animRocket.AddComponent<Image>();
            CopyImageProperties(sourcePrimary, animPrimary);
        }

        Image sourceSecondary = rocket.GetSecondaryImage();
        if (sourceSecondary != null)
        {
            GameObject secondaryObj = new GameObject("AnimatingRocketSecondary");
            secondaryObj.transform.SetParent(animRocket.transform, false);

            RectTransform secondaryRect = secondaryObj.AddComponent<RectTransform>();
            CopyRectTransformLocal(secondaryRect, sourceSecondary.rectTransform);

            Image animSecondary = secondaryObj.AddComponent<Image>();
            CopyImageProperties(sourceSecondary, animSecondary);
        }

        animRocket.transform.SetSiblingIndex(gridController.GridContainer.childCount - 1);
        return animRocket;
    }

    private IEnumerator AnimateRocketsToTarget(List<(GameObject clone, RocketItem original)> animatingObjects, GridCell targetCell)
    {
        RectTransform targetRect = targetCell.GetComponent<RectTransform>();
        Vector2 targetPosition = targetRect.anchoredPosition;

        var startPositions = new List<Vector2>();
        var startScales = new List<Vector3>();

        foreach (var (clone, _) in animatingObjects)
        {
            RectTransform rect = clone.GetComponent<RectTransform>();
            startPositions.Add(rect.anchoredPosition);
            startScales.Add(rect.localScale);
        }

        float elapsed = 0f;
        while (elapsed < creationAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / creationAnimationDuration);

            for (int i = 0; i < animatingObjects.Count; i++)
            {
                RectTransform rect = animatingObjects[i].clone.GetComponent<RectTransform>();
                rect.anchoredPosition = Vector2.Lerp(startPositions[i], targetPosition, t);

                float mergeScale = Mathf.Lerp(1f, 0.6f, t);
                rect.localScale = startScales[i] * mergeScale;
            }

            yield return null;
        }

        foreach (var (clone, _) in animatingObjects)
        {
            RectTransform rect = clone.GetComponent<RectTransform>();
            rect.anchoredPosition = targetPosition;
            rect.localScale = Vector3.one;
        }
    }

    private void CopyRectTransformLocal(RectTransform target, RectTransform source)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    private void CopyImageProperties(Image source, Image target)
    {
        target.sprite = source.sprite;
        target.color = source.color;
        target.raycastTarget = false;
        target.preserveAspect = source.preserveAspect;
        target.material = source.material;
        target.type = source.type;
        target.fillCenter = source.fillCenter;
    }
}

