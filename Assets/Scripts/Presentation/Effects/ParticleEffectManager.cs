using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectManager : MonoBehaviour
{
    public static ParticleEffectManager Instance { get; private set; }

    [Header("Cube Burst Settings")]
    [SerializeField] private ParticleSystem cubeBurstPrefab;
    [SerializeField] private ParticleSystem[] additionalCubeBurstPrefabs = new ParticleSystem[0];
    [SerializeField] private Color defaultBurstColor = Color.white;

    [Header("Balloon Pop Settings")]
    [SerializeField] private ParticleSystem balloonPopPrefab1;
    [SerializeField] private ParticleSystem balloonPopPrefab2;

    [System.Serializable]
    private struct CubeBurstColor
    {
        public ItemType cubeType;
        public Color color;
    }

    [SerializeField] private CubeBurstColor[] cubeBurstColors = new CubeBurstColor[0];

    private Transform effectRoot;

    public Transform ParticleContainer
    {
        get
        {
            EnsureInitialized();
            return effectRoot;
        }
    }

    private readonly Dictionary<ItemType, Color> colorLookup = new Dictionary<ItemType, Color>();
    private readonly List<ParticleSystem> variantPrefabs = new List<ParticleSystem>();
    private readonly List<Queue<ParticleSystem>> variantPools = new List<Queue<ParticleSystem>>();
    private readonly List<ParticleSystem> activeSystems = new List<ParticleSystem>();
    private readonly Dictionary<ParticleSystem, int> systemVariantLookup = new Dictionary<ParticleSystem, int>();

    private readonly List<ParticleSystem> balloonPopPrefabs = new List<ParticleSystem>();
    private readonly List<Queue<ParticleSystem>> balloonPopPools = new List<Queue<ParticleSystem>>();
    private readonly Dictionary<ParticleSystem, int> balloonPopVariantLookup = new Dictionary<ParticleSystem, int>();

    private MaterialPropertyBlock colorPropertyBlock;
    private bool initialized;
    private bool warnedMissingPrefabs;

    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int TintColorPropertyId = Shader.PropertyToID("_TintColor");
    private static readonly int GlowColorPropertyId = Shader.PropertyToID("_GlowColor");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            EnsureInitialized();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        colorPropertyBlock ??= new MaterialPropertyBlock();
        BuildColorLookup();
        RefreshVariantPrefabsList();
        EnsureVariantPoolsCapacity();
        EnsureBalloonPopPoolsCapacity();
        warnedMissingPrefabs = false;

        if (Application.isPlaying && Instance == this)
        {
            CleanupAllParticles();
            InitializeVariantDefinitions();
        }
        else
        {
            initialized = false;
        }
    }
#endif

    private void CreateRoot()
    {
        if (effectRoot != null) return;

        GameObject rootObject = new GameObject("ParticleBurstRoot");
        rootObject.transform.SetParent(transform, false);
        effectRoot = rootObject.transform;
    }

    private void InitializeVariantDefinitions()
    {
        RefreshVariantPrefabsList();
        EnsureVariantPoolsCapacity(true);
        EnsureBalloonPopPoolsCapacity(true);
        activeSystems.Clear();
        systemVariantLookup.Clear();
        balloonPopVariantLookup.Clear();
        initialized = true;
    }

    private void EnsureInitialized()
    {
        if (!initialized)
        {
            colorPropertyBlock ??= new MaterialPropertyBlock();
            RefreshVariantPrefabsList();
            EnsureVariantPoolsCapacity();
            EnsureBalloonPopPoolsCapacity();
            BuildColorLookup();
            CreateRoot();
            initialized = true;
        }
        else
        {
            EnsureVariantPoolsCapacity();
            EnsureBalloonPopPoolsCapacity();
        }
    }

    private void EnsureVariantPoolsCapacity(bool reset = false)
    {
        if (reset)
        {
            DisposePools(variantPools);
            variantPools.Clear();
        }

        int targetCount = variantPrefabs.Count;

        while (variantPools.Count < targetCount)
        {
            variantPools.Add(new Queue<ParticleSystem>());
        }

        while (variantPools.Count > targetCount)
        {
            int lastIndex = variantPools.Count - 1;
            DisposeQueue(variantPools[lastIndex]);
            variantPools.RemoveAt(lastIndex);
        }
    }

    private void EnsureBalloonPopPoolsCapacity(bool reset = false)
    {
        if (reset)
        {
            DisposePools(balloonPopPools);
            balloonPopPools.Clear();
        }

        int targetCount = balloonPopPrefabs.Count;

        while (balloonPopPools.Count < targetCount)
        {
            balloonPopPools.Add(new Queue<ParticleSystem>());
        }

        while (balloonPopPools.Count > targetCount)
        {
            int lastIndex = balloonPopPools.Count - 1;
            DisposeQueue(balloonPopPools[lastIndex]);
            balloonPopPools.RemoveAt(lastIndex);
        }
    }

    private void DisposePools(List<Queue<ParticleSystem>> pools)
    {
        if (pools == null) return;

        for (int i = 0; i < pools.Count; i++)
        {
            DisposeQueue(pools[i]);
        }
    }

    private void DisposeQueue(Queue<ParticleSystem> pool)
    {
        if (pool == null) return;

        while (pool.Count > 0)
        {
            ParticleSystem system = pool.Dequeue();
            if (system == null) continue;

            if (Application.isPlaying)
            {
                Destroy(system.gameObject);
            }
            else
            {
                DestroyImmediate(system.gameObject);
            }
        }

        pool.Clear();
    }

    private void RefreshVariantPrefabsList()
    {
        variantPrefabs.Clear();

        if (cubeBurstPrefab != null)
        {
            variantPrefabs.Add(cubeBurstPrefab);
        }

        if (additionalCubeBurstPrefabs != null)
        {
            for (int i = 0; i < additionalCubeBurstPrefabs.Length; i++)
            {
                ParticleSystem extraPrefab = additionalCubeBurstPrefabs[i];
                if (extraPrefab != null)
                {
                    variantPrefabs.Add(extraPrefab);
                }
            }
        }

        RefreshBalloonPopPrefabsList();
    }

    private void RefreshBalloonPopPrefabsList()
    {
        balloonPopPrefabs.Clear();

        if (balloonPopPrefab1 != null)
        {
            balloonPopPrefabs.Add(balloonPopPrefab1);
        }

        if (balloonPopPrefab2 != null)
        {
            balloonPopPrefabs.Add(balloonPopPrefab2);
        }

        EnsureBalloonPopPoolsCapacity();
    }

    private void BuildColorLookup()
    {
        colorLookup.Clear();

        if (cubeBurstColors == null) return;

        for (int i = 0; i < cubeBurstColors.Length; i++)
        {
            CubeBurstColor entry = cubeBurstColors[i];
            colorLookup[entry.cubeType] = entry.color;
        }
    }

    public void SpawnCubeBurst(RectTransform source, ItemType cubeType)
    {
        if (source == null) return;

        SpawnCubeBurst(source.position, cubeType);
    }

    public void SpawnCubeBurst(Vector3 worldPosition, ItemType cubeType)
    {
        EnsureInitialized();

        if (variantPrefabs.Count == 0)
        {
            if (!warnedMissingPrefabs)
            {
                Debug.LogWarning("ParticleEffectManager has no burst prefabs assigned.", this);
                warnedMissingPrefabs = true;
            }
            return;
        }

        warnedMissingPrefabs = false;

        if (effectRoot == null)
        {
            CreateRoot();
        }

        for (int variantIndex = 0; variantIndex < variantPrefabs.Count; variantIndex++)
        {
            SpawnVariantBurst(variantIndex, worldPosition, cubeType);
        }
    }

    public void SpawnBalloonPop(RectTransform source)
    {
        if (source == null) return;

        SpawnBalloonPop(source.position);
    }

    public void SpawnBalloonPop(Vector3 worldPosition)
    {
        EnsureInitialized();

        if (balloonPopPrefabs.Count == 0) return;

        if (effectRoot == null)
        {
            CreateRoot();
        }

        for (int i = 0; i < balloonPopPrefabs.Count; i++)
        {
            SpawnBalloonPopVariant(i, worldPosition);
        }
    }

    private void SpawnBalloonPopVariant(int variantIndex, Vector3 worldPosition)
    {
        ParticleSystem system = GetBalloonPop(variantIndex);
        if (system == null) return;

        system.transform.position = worldPosition;
        system.Play(true);

        activeSystems.Add(system);
        balloonPopVariantLookup[system] = variantIndex;

        StartCoroutine(RecycleWhenFinished(system));
    }

    private ParticleSystem GetBalloonPop(int variantIndex)
    {
        EnsureBalloonPopPoolsCapacity();

        if (variantIndex < 0 || variantIndex >= balloonPopPrefabs.Count)
        {
            return null;
        }

        while (balloonPopPools.Count <= variantIndex)
        {
            balloonPopPools.Add(new Queue<ParticleSystem>());
        }

        Queue<ParticleSystem> pool = balloonPopPools[variantIndex];
        ParticleSystem system = null;

        while (pool.Count > 0 && system == null)
        {
            system = pool.Dequeue();
        }

        ParticleSystem prefab = balloonPopPrefabs[variantIndex];
        if (system == null && prefab != null)
        {
            CreateRoot();
            system = Instantiate(prefab, effectRoot);
        }
        else if (system != null)
        {
            CreateRoot();
            system.transform.SetParent(effectRoot, false);
        }

        if (system == null)
        {
            return null;
        }

        system.gameObject.SetActive(true);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Clear(true);

        return system;
    }

    private void SpawnVariantBurst(int variantIndex, Vector3 worldPosition, ItemType cubeType)
    {
        ParticleSystem system = GetBurst(variantIndex);
        if (system == null) return;

        system.transform.position = worldPosition;

        Color burstColor = GetColorFor(cubeType);

        var main = system.main;
        main.startColor = burstColor;

        var renderer = system.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            colorPropertyBlock.Clear();

            if (ColorPropertyId != -1)
            {
                colorPropertyBlock.SetColor(ColorPropertyId, burstColor);
            }

            if (TintColorPropertyId != -1)
            {
                colorPropertyBlock.SetColor(TintColorPropertyId, burstColor);
            }

            if (GlowColorPropertyId != -1)
            {
                colorPropertyBlock.SetColor(GlowColorPropertyId, burstColor);
            }

            renderer.SetPropertyBlock(colorPropertyBlock);
        }

        system.Play(true);

        activeSystems.Add(system);
        systemVariantLookup[system] = variantIndex;

        StartCoroutine(RecycleWhenFinished(system));
    }

    private ParticleSystem GetBurst(int variantIndex)
    {
        EnsureVariantPoolsCapacity();

        if (variantIndex < 0 || variantIndex >= variantPrefabs.Count)
        {
            return null;
        }

        while (variantPools.Count <= variantIndex)
        {
            variantPools.Add(new Queue<ParticleSystem>());
        }

        Queue<ParticleSystem> pool = variantPools[variantIndex];
        ParticleSystem system = null;

        while (pool.Count > 0 && system == null)
        {
            system = pool.Dequeue();
        }

        ParticleSystem prefab = variantPrefabs[variantIndex];
        if (system == null && prefab != null)
        {
            CreateRoot();
            system = Instantiate(prefab, effectRoot);
        }
        else if (system != null)
        {
            CreateRoot();
            system.transform.SetParent(effectRoot, false);
        }

        if (system == null)
        {
            return null;
        }

        system.gameObject.SetActive(true);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Clear(true);

        return system;
    }

    private IEnumerator RecycleWhenFinished(ParticleSystem system)
    {
        if (system == null) yield break;

        while (system.IsAlive(true))
        {
            yield return null;
        }

        ReturnToPool(system);
    }

    private void ReturnToPool(ParticleSystem system)
    {
        if (system == null) return;

        bool isBalloonPop = balloonPopVariantLookup.TryGetValue(system, out int balloonVariantIndex);
        if (isBalloonPop)
        {
            balloonPopVariantLookup.Remove(system);
        }

        bool isCubeBurst = systemVariantLookup.TryGetValue(system, out int variantIndex);
        if (isCubeBurst)
        {
            systemVariantLookup.Remove(system);
        }

        activeSystems.Remove(system);

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Clear(true);
        system.gameObject.SetActive(false);
        CreateRoot();
        system.transform.SetParent(effectRoot, false);

        if (isBalloonPop && balloonVariantIndex >= 0 && balloonVariantIndex < balloonPopPools.Count)
        {
            balloonPopPools[balloonVariantIndex].Enqueue(system);
        }
        else if (isCubeBurst && variantIndex >= 0 && variantIndex < variantPools.Count)
        {
            variantPools[variantIndex].Enqueue(system);
        }
        else
        {
            Destroy(system.gameObject);
        }
    }

    private Color GetColorFor(ItemType cubeType)
    {
        if (colorLookup.TryGetValue(cubeType, out Color color))
        {
            return color;
        }

        return defaultBurstColor;
    }

    public bool HasActiveParticles()
    {
        if (activeSystems == null) return false;
        for (int i = activeSystems.Count - 1; i >= 0; i--)
        {
            if (activeSystems[i] != null && activeSystems[i].IsAlive(true))
            {
                return true;
            }
        }
        return false;
    }

    public void CleanupAllParticles()
    {
        EnsureInitialized();
        StopAllCoroutines();

        for (int i = activeSystems.Count - 1; i >= 0; i--)
        {
            ReturnToPool(activeSystems[i]);
        }

        activeSystems.Clear();
        systemVariantLookup.Clear();
        balloonPopVariantLookup.Clear();
    }
}
