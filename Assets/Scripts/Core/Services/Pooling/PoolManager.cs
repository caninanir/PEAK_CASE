using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("Item Prefabs")]
    [SerializeField] private CubeItem cubePrefab;
    [SerializeField] private RocketItem rocketPrefab;
    [SerializeField] private ObstacleItem obstaclePrefab;

    [Header("Pool Settings")]
    [SerializeField] private int cubePoolSize = 50;
    [SerializeField] private int rocketPoolSize = 20;
    [SerializeField] private int obstaclePoolSize = 30;
    [SerializeField] private int particlePoolSize = 100;

    private Dictionary<ItemType, GenericPool<CubeItem>> cubePools;
    private GenericPool<RocketItem> rocketPool;
    private GenericPool<ObstacleItem> obstaclePool;
    private GenericPool<ParticleElement> particlePool;
    
    private Transform poolContainer;
    private Transform itemContainer;
    private Transform particleContainer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void MarkParentAsDontDestroy()
    {
        if (transform.parent != null)
        {
            DontDestroyOnLoad(transform.parent.gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void InitializePools()
    {
        poolContainer = new GameObject("PoolContainer").transform;
        poolContainer.SetParent(transform);
        poolContainer.localPosition = Vector3.zero;
        poolContainer.localRotation = Quaternion.identity;
        poolContainer.localScale = Vector3.one;
        
        itemContainer = new GameObject("ItemContainer").transform;
        itemContainer.SetParent(poolContainer);
        itemContainer.localPosition = Vector3.zero;
        itemContainer.localRotation = Quaternion.identity;
        itemContainer.localScale = Vector3.one;
        
        particleContainer = new GameObject("ParticleContainer").transform;
        particleContainer.SetParent(poolContainer);
        particleContainer.localPosition = Vector3.zero;
        particleContainer.localRotation = Quaternion.identity;
        particleContainer.localScale = Vector3.one;
        
        InitializeCubePools();
        InitializeRocketPool();
        InitializeObstaclePool();
        InitializeParticlePool();
    }

    private void InitializeCubePools()
    {
        cubePools = new Dictionary<ItemType, GenericPool<CubeItem>>();
        
        ItemType[] cubeTypes = { ItemType.RedCube, ItemType.GreenCube, ItemType.BlueCube, ItemType.YellowCube, ItemType.PurpleCube };
        
        foreach (ItemType cubeType in cubeTypes)
        {
            cubePools[cubeType] = new GenericPool<CubeItem>(cubePrefab, itemContainer, cubePoolSize);
        }
    }

    private void InitializeRocketPool()
    {
        rocketPool = new GenericPool<RocketItem>(rocketPrefab, itemContainer, rocketPoolSize);
    }

    private void InitializeObstaclePool()
    {
        obstaclePool = new GenericPool<ObstacleItem>(obstaclePrefab, itemContainer, obstaclePoolSize);
    }

    private void InitializeParticlePool()
    {
        GameObject particlePrefab = CreateParticlePrefab();
        ParticleElement particleElement = particlePrefab.GetComponent<ParticleElement>();
        particlePool = new GenericPool<ParticleElement>(particleElement, particleContainer, particlePoolSize);
    }

    private GameObject CreateParticlePrefab()
    {
        GameObject prefab = new GameObject("ParticlePrefab");
        prefab.transform.SetParent(particleContainer, false);

        RectTransform rectTransform = prefab.AddComponent<RectTransform>();
        rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image image = prefab.AddComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;

        ParticleElement element = prefab.AddComponent<ParticleElement>();
        element.Initialize(rectTransform, image);        


        prefab.SetActive(false);
        return prefab;
    }

    public BaseItem GetItem(ItemType itemType, Transform parent = null)
    {
        BaseItem item = null;
        
        switch (itemType)
        {
            case ItemType.RedCube:
            case ItemType.GreenCube:
            case ItemType.BlueCube:
            case ItemType.YellowCube:
            case ItemType.PurpleCube:
                if (cubePools.ContainsKey(itemType))
                {
                    item = cubePools[itemType].Get();
                }
                break;
                
            case ItemType.HorizontalRocket:
            case ItemType.VerticalRocket:
                item = rocketPool.Get();
                break;
                
            case ItemType.Balloon:
            case ItemType.Duck:
                item = obstaclePool.Get();
                break;
        }

        if (item != null && parent != null)
        {
            item.transform.SetParent(parent, false);
        }

        return item;
    }

    public void ReturnItem(BaseItem item)
    {
        item.OnReturnToPool();
        item.transform.SetParent(itemContainer, false);
        
        switch (item)
        {
            case CubeItem cubeItem:
                if (cubePools.ContainsKey(cubeItem.itemType))
                {
                    cubePools[cubeItem.itemType].Return(cubeItem);
                }
                break;
                
            case RocketItem rocketItem:
                rocketPool.Return(rocketItem);
                break;
                
            case ObstacleItem obstacleItem:
                obstaclePool.Return(obstacleItem);
                break;
        }
    }

    public ParticleElement GetParticle(Transform parent)
    {
        ParticleElement particle = particlePool.Get();
        particle.transform.SetParent(parent, false);
        return particle;
    }

    public void ReturnParticle(ParticleElement particle, Transform parent)
    {
        particle.transform.SetParent(parent, false);
        particlePool.Return(particle);
    }

    public void ClearAllPools()
    {
        foreach (var pool in cubePools.Values)
        {
            pool.Clear();
        }
        rocketPool.Clear();
        obstaclePool.Clear();
        particlePool.Clear();
    }

    public void WarmUpPools(int cubeCount, int rocketCount, int obstacleCount, int particleCount)
    {
        foreach (var pool in cubePools.Values)
        {
            pool.WarmUp(cubeCount);
        }
        rocketPool.WarmUp(rocketCount);
        obstaclePool.WarmUp(obstacleCount);
        particlePool.WarmUp(particleCount);
    }
}



