using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RocketProjectileService : MonoBehaviour
{
    [Header("Rocket Projectile Prefab")] 
    [SerializeField] private GameObject projectilePrefab;

    [Header("Rocket Projectile Settings")]
    [Tooltip("How fast rockets fly across the grid (time per cell in seconds). Lower values = faster rockets.")]
    [SerializeField] [Range(0.05f, 1.0f)] private float rocketSpeed = 0.15f;

    private GridController gridController;
    private static int globalActiveProjectiles = 0;
    private Dictionary<GameObject, GameObject> duplicateProjectiles = new Dictionary<GameObject, GameObject>();

    public float RocketSpeed => rocketSpeed;

    public bool HasActiveProjectiles()
    {
        return globalActiveProjectiles > 0;
    }

    private void Awake()
    {
        gridController = FindFirstObjectByType<GridController>();
    }

    public IEnumerator AnimateProjectile(Vector2Int startPos, Vector2Int direction)
    {
        globalActiveProjectiles++;

        GameObject projectile = CreateRocketProjectile(startPos, direction);
        if (projectile == null)
        {
            globalActiveProjectiles--;
            yield break;
        }

        RectTransform projectileRect = projectile.GetComponent<RectTransform>();
        GameObject duplicateProjectile = GetDuplicateProjectile(projectile);
        RectTransform duplicateRect = duplicateProjectile?.GetComponent<RectTransform>();

        yield return MoveProjectileThroughGrid(projectileRect, duplicateRect, startPos, direction);

        globalActiveProjectiles--;
        if (globalActiveProjectiles <= 0)
        {
            yield return null;
            yield return new WaitForSeconds(0.05f);
            EventBus.Publish(new GravityStartedEvent());
        }

        if (duplicateRect != null)
        {
            duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
        }

        yield return MoveProjectileOffScreen(projectileRect, duplicateRect, direction);
        yield return FadeOutProjectile(projectile, duplicateProjectile, direction);
        
        CleanupProjectile(projectile);
    }

    public IEnumerator WaitForAllProjectilesToComplete()
    {
        while (globalActiveProjectiles > 0)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);
    }


    public IEnumerator SpawnCrossProjectiles(Vector2Int center)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (Vector2Int direction in directions)
        {
            StartCoroutine(AnimateProjectile(center, direction));
        }
        
        yield return null;
    }

    public IEnumerator SpawnComboProjectiles(Vector2Int center, RocketService rocketService)
    {
        Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        
        foreach (Vector2Int direction in directions)
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int projectileStart = rocketService.GetPerpendicularStartPosition(center, direction, offset);
                if (gridController.IsValidPosition(projectileStart.x, projectileStart.y))
                {
                    StartCoroutine(AnimateProjectile(projectileStart, direction));
                }
            }
        }
        
        yield return null;
    }

    private GameObject GetDuplicateProjectile(GameObject projectile)
    {
        return duplicateProjectiles.ContainsKey(projectile) ? duplicateProjectiles[projectile] : null;
    }

    private IEnumerator MoveProjectileThroughGrid(RectTransform projectileRect, RectTransform duplicateRect, Vector2Int startPos, Vector2Int direction)
    {
        Vector2Int currentPos = startPos + direction;
        float cellSize = 60f;
        
        if (duplicateRect != null && !gridController.IsValidPosition(currentPos.x, currentPos.y))
        {
            duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
        }

        while (gridController.IsValidPosition(currentPos.x, currentPos.y))
        {
            GridCell targetCell = gridController.GetCell(currentPos.x, currentPos.y);
            if (targetCell != null)
            {
                RectTransform targetRect = targetCell.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    cellSize = Mathf.Max(targetRect.sizeDelta.x, targetRect.sizeDelta.y);
                    yield return MoveProjectileToPosition(projectileRect, duplicateRect, targetRect.anchoredPosition, direction, cellSize);
                }
            }

            ProcessProjectileAtPosition(currentPos);
            currentPos += direction;
        }
    }

    private IEnumerator MoveProjectileToPosition(RectTransform projectileRect, RectTransform duplicateRect, Vector2 targetPosition, Vector2Int direction, float cellSize)
    {
        Vector2 startPosition = projectileRect.anchoredPosition;
        
        yield return AnimateMovement(projectileRect, duplicateRect, startPosition, targetPosition, rocketSpeed);
    }

    private void ProcessProjectileAtPosition(Vector2Int position)
    {
        BaseItem item = gridController.GetItem(position.x, position.y);
        if (item != null)
        {
            DamageItem(item);
        }
    }

    private void DamageItem(BaseItem item)
    {
        switch (item)
        {
            case CubeItem cube:
                RectTransform rectTransform = cube.GetComponent<RectTransform>();
                ParticleEffectManager.Instance.SpawnCubeBurst(rectTransform, cube.itemType);
                
                bool isGoalCube = ObstacleController.Instance != null && ObstacleController.Instance.IsGoalCube(cube.itemType);
                
                AudioManager.Instance.PlayCubeBreakSound();
                if (isGoalCube)
                {
                    AudioManager.Instance.PlayGoalCubeBreakSound();
                }
                
                Vector2Int gridPos = cube.GetGridPosition();
                EventBus.Publish(new ItemDestroyedEvent
                {
                    GridX = gridPos.x,
                    GridY = gridPos.y,
                    ItemType = cube.itemType
                });
                
                PoolManager.Instance.ReturnItem(cube);
                cube.currentCell?.RemoveItem();
                break;
            case ObstacleItem obstacle:
                if (obstacle.CanTakeDamageFrom(DamageSource.Rocket))
                {
                    obstacle.TakeDamage(1);
                }
                break;
            case RocketItem rocket:
                StartCoroutine(ProcessChainReactionExplosion(rocket));
                break;
        }
    }

    private IEnumerator ProcessChainReactionExplosion(RocketItem rocket)
    {
        Vector2Int rocketPos = rocket.GetGridPosition();
        Vector2Int direction = GetRocketDirection(rocket.itemType);
        
        AudioManager.Instance.PlayRocketPopSound();
        DestroyRocket(rocket);
        
        StartCoroutine(AnimateProjectile(rocketPos, direction));
        StartCoroutine(AnimateProjectile(rocketPos, -direction));
        
        yield return new WaitForSeconds(0.1f);
    }

    private Vector2Int GetRocketDirection(ItemType rocketType)
    {
        if (rocketType == ItemType.HorizontalRocket)
        {
            return Vector2Int.right;
        }
        else
        {
            return Vector2Int.up;
        }
    }

    private void DestroyRocket(RocketItem rocket)
    {
        Destroy(rocket.gameObject);
        rocket.currentCell?.RemoveItem();
    }


    private IEnumerator MoveProjectileOffScreen(RectTransform projectileRect, RectTransform duplicateRect, Vector2Int direction)
    {
        float cellSize = 80f;
        int offScreenSteps = 16;
        
        Vector2Int originalDirection = direction;
        bool isVertical = direction == Vector2Int.up || direction == Vector2Int.down;
        if (isVertical)
        {
            originalDirection = -direction;
        }

        for (int step = 0; step < offScreenSteps; step++)
        {
            Vector2 startPosition = projectileRect.anchoredPosition;
            Vector2 targetPosition = startPosition + new Vector2(originalDirection.x * cellSize, originalDirection.y * cellSize);
            
            yield return AnimateMovement(projectileRect, duplicateRect, startPosition, targetPosition, rocketSpeed);
        }
    }

    private IEnumerator FadeOutProjectile(GameObject projectile, GameObject duplicateProjectile, Vector2Int direction)
    {
        Image projectileImage = projectile.GetComponent<Image>();
        Image duplicateImage = duplicateProjectile?.GetComponent<Image>();
        
        Color originalColor = projectileImage.color;
        Color duplicateOriginalColor = duplicateImage?.color ?? Color.white;
        
        RectTransform projectileRect = projectile.GetComponent<RectTransform>();
        RectTransform duplicateRect = duplicateProjectile?.GetComponent<RectTransform>();
        
        float cellSize = 60f;
        
        Vector2Int originalDirection = direction;
        bool isVertical = direction == Vector2Int.up || direction == Vector2Int.down;
        if (isVertical)
        {
            originalDirection = -direction;
        }

        for (int fadeStep = 0; fadeStep < 3; fadeStep++)
        {
            Vector2 startPosition = projectileRect.anchoredPosition;
            Vector2 targetPosition = startPosition + new Vector2(originalDirection.x * cellSize, originalDirection.y * cellSize);
            
            float fadeAlpha = 1f - ((fadeStep + 1) / 3f);
            
            yield return AnimateMovementWithFade(projectileRect, duplicateRect, projectileImage, duplicateImage, 
                startPosition, targetPosition, originalColor, duplicateOriginalColor, fadeAlpha, rocketSpeed);
        }
    }

    private IEnumerator AnimateMovement(RectTransform projectileRect, RectTransform duplicateRect, 
        Vector2 startPos, Vector2 targetPos, float moveTime)
    {
        float elapsed = 0f;
        
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            
            projectileRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            
            if (duplicateRect != null)
            {
                duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
            }
            
            yield return null;
        }

        projectileRect.anchoredPosition = targetPos;
        if (duplicateRect != null)
        {
            duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
        }
    }

    private IEnumerator AnimateMovementWithFade(RectTransform projectileRect, RectTransform duplicateRect, 
        Image projectileImage, Image duplicateImage, Vector2 startPos, Vector2 targetPos, 
        Color originalColor, Color duplicateOriginalColor, float fadeAlpha, float moveTime)
    {
        float elapsed = 0f;
        
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            
            projectileRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            
            if (duplicateRect != null)
            {
                duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
                
                if (duplicateImage != null)
                {
                    Color duplicateFadedColor = duplicateOriginalColor;
                    duplicateFadedColor.a = fadeAlpha;
                    duplicateImage.color = duplicateFadedColor;
                }
            }
            
            yield return null;
        }

        projectileRect.anchoredPosition = targetPos;
        if (duplicateRect != null)
        {
            duplicateRect.anchoredPosition = GetUIPosition(projectileRect.transform);
        }
    }

    private void CleanupProjectile(GameObject projectile)
    {
        if (duplicateProjectiles.ContainsKey(projectile))
        {
            GameObject duplicate = duplicateProjectiles[projectile];
            if (duplicate != null) Destroy(duplicate);
            duplicateProjectiles.Remove(projectile);
        }
        
        if (projectile != null) Destroy(projectile);
    }

    private GameObject CreateRocketProjectile(Vector2Int startPos, Vector2Int direction)
    {
        Sprite projectileSprite = GetProjectileSprite(direction);
        
        GameObject projectile = new GameObject("RocketProjectile");
        projectile.transform.SetParent(gridController.GridContainer, false);
        
        SetupProjectileComponents(projectile, projectileSprite, startPos);
        CreateDuplicateProjectile(projectile, projectileSprite, direction);
        
        return projectile;
    }

    private void SetupProjectileComponents(GameObject projectile, Sprite sprite, Vector2Int startPos)
    {
        RectTransform projectileRect = projectile.AddComponent<RectTransform>();
        Image projectileImage = projectile.AddComponent<Image>();
        
        projectileImage.sprite = sprite;
        projectileImage.raycastTarget = false;
        projectileImage.preserveAspect = true;
        projectileImage.color = new Color(1f, 1f, 1f, 0f);
        
        GridCell startCell = gridController.GetCell(startPos.x, startPos.y);
        if (startCell != null)
        {
            RectTransform cellRect = startCell.GetComponent<RectTransform>();
            if (cellRect != null)
            {
                CopyRectTransformProperties(projectileRect, cellRect);
                projectile.transform.SetSiblingIndex(gridController.GridContainer.childCount - 1);
            }
        }
    }

    private void CopyRectTransformProperties(RectTransform target, RectTransform source)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
    }

    private void CreateDuplicateProjectile(GameObject originalProjectile, Sprite projectileSprite, Vector2Int direction)
    {
        Transform container = ParticleEffectManager.Instance.ParticleContainer;

        GameObject duplicate = Instantiate(projectilePrefab, container);
        duplicate.SetActive(true);

        RectTransform dupRect = duplicate.GetComponent<RectTransform>();
        dupRect.anchoredPosition = GetUIPosition(originalProjectile.transform);
        dupRect.localRotation = Quaternion.Euler(0f, 0f, GetRotationAngle(direction));

        ParticleSystem[] particleSystems = duplicate.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.gameObject.SetActive(true);
            ps.Play();
        }

        duplicateProjectiles[originalProjectile] = duplicate;
    }

    private float GetRotationAngle(Vector2Int dir)
    {
        if (dir == Vector2Int.right) return 180f;
        if (dir == Vector2Int.up) return 90f;
        if (dir == Vector2Int.down) return -90f;
        return 0f;
    }

    private Vector2 GetUIPosition(Transform itemTransform)
    {
        Transform particleContainer = ParticleEffectManager.Instance.ParticleContainer;

        RectTransform itemRect = itemTransform.GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        itemRect.GetWorldCorners(corners);
        
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        
        return particleContainer.InverseTransformPoint(worldCenter);
    }

    private Sprite GetProjectileSprite(Vector2Int direction)
    {
        return null;
    }

    public void CleanupAllProjectiles()
    {
        StopAllCoroutines();
        
        List<GameObject> projectilesToClean = new List<GameObject>(duplicateProjectiles.Keys);
        foreach (GameObject projectile in projectilesToClean)
        {
            CleanupProjectile(projectile);
        }
        
        for (int i = gridController.GridContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = gridController.GridContainer.GetChild(i);
            if (child.name == "RocketProjectile")
            {
                Destroy(child.gameObject);
            }
        }
        
        globalActiveProjectiles = 0;
        duplicateProjectiles.Clear();
    }
}


