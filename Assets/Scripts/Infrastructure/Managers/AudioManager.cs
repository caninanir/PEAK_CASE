using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Cube Breaking Sounds (10 variations)")]
    [SerializeField] private AudioClip[] cubeBreakSounds = new AudioClip[10];

    [Header("Obstacle Sounds")]
    [SerializeField] private AudioClip balloonBreakSound;
    [SerializeField] private AudioClip duckBreakSound;
    [SerializeField] private AudioClip duckCollectionSound;

    [Header("Rocket Sounds")]
    [SerializeField] private AudioClip rocketCreationSound;
    [SerializeField] private AudioClip rocketPopSound;
    [SerializeField] private AudioClip comboRocketPopSound;

    [Header("Game Sounds")]
    [SerializeField] private AudioClip cubeFallSound;
    [SerializeField] private AudioClip gameLostSound;
    [SerializeField] private AudioClip gameWonSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip goalCubeBreakSound;

    [Header("Fall Sound Settings")]
    [SerializeField] private float fallSoundVolume = 0.6f;
    [SerializeField] private float fallSoundMinPitch = 0.9f;
    [SerializeField] private float fallSoundMaxPitch = 1.2f;

    [Header("Audio Settings")]
    [SerializeField] private int audioSourcePoolSize = 20;
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float sfxVolume = 0.8f;
    [SerializeField] private float uiVolume = 1f;
    [SerializeField] private float maxRandomDelay = 0.2f;

    [Header("AudioSource Priority (0 = highest, 256 = lowest)")]
    [Range(0,256)]
    [SerializeField] private int sfxPriority = 128;

    private Queue<AudioSource> availableAudioSources = new Queue<AudioSource>();
    private List<AudioSource> activeAudioSources = new List<AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
            InitializeAudioSourcePool();
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

    private void InitializeAudioSourcePool()
    {
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioSourceObj = new GameObject($"AudioSource_{i}");
            audioSourceObj.transform.SetParent(transform);
            audioSourceObj.transform.localPosition = Vector3.zero;
            audioSourceObj.transform.localRotation = Quaternion.identity;
            audioSourceObj.transform.localScale = Vector3.one;
            
            AudioSource audioSource = audioSourceObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.priority = sfxPriority;
            
            availableAudioSources.Enqueue(audioSource);
        }
    }


    private void Update()
    {
        for (int i = activeAudioSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeAudioSources[i];
            if (source != null && !source.isPlaying)
            {
                ReturnAudioSourceToPool(source);
            }
        }
    }

    private AudioSource GetAvailableAudioSource()
    {
        AudioSource audioSource;
        
        if (availableAudioSources.Count > 0)
        {
            audioSource = availableAudioSources.Dequeue();
        }
        else
        {
            GameObject audioSourceObj = new GameObject($"AudioSource_Dynamic_{activeAudioSources.Count}");
            audioSourceObj.transform.SetParent(transform);
            audioSource = audioSourceObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.priority = sfxPriority;
        }
        
        activeAudioSources.Add(audioSource);
        return audioSource;
    }

    private void ReturnAudioSourceToPool(AudioSource audioSource)
    {
        audioSource.Stop();
        audioSource.clip = null;
        activeAudioSources.Remove(audioSource);
        availableAudioSources.Enqueue(audioSource);
    }

    public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        StartCoroutine(PlaySoundDelayed(clip, volume, pitch, Random.Range(0f, maxRandomDelay)));
    }

    private void PlaySoundWithRandomPitch(AudioClip clip, float volume = 1f, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        float randomPitch = Random.Range(minPitch, maxPitch);
        PlaySound(clip, volume, randomPitch);
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float volume, float pitch, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        AudioSource audioSource = GetAvailableAudioSource();
        audioSource.clip = clip;
        audioSource.volume = volume * masterVolume * sfxVolume;
        audioSource.pitch = pitch;
        audioSource.Play();
    }

    public void PlayCubeBreakSound()
    {
        if (cubeBreakSounds.Length == 0) return;
        
        AudioClip randomClip = cubeBreakSounds[Random.Range(0, cubeBreakSounds.Length)];
        PlaySoundWithRandomPitch(randomClip, 0.8f, 0.95f, 1.05f);
    }

    public void PlayObstacleSound(ItemType obstacleType, bool isDestroyed)
    {
        switch (obstacleType)
        {
            case ItemType.Balloon:
                if (isDestroyed) PlaySound(balloonBreakSound, 0.9f);
                break;
            case ItemType.Duck:
                if (isDestroyed) PlaySound(duckBreakSound, 0.9f);
                break;
        }
    }

    public void PlayDuckCollectionSound()
    {
        PlaySound(duckCollectionSound, 0.9f);
    }

    public void PlayGoalCubeBreakSound()
    {
        PlaySoundWithRandomPitch(goalCubeBreakSound, 0.9f, 0.95f, 1.05f);
    }

    public void PlayRocketCreationSound()
    {
        PlaySoundWithRandomPitch(rocketCreationSound, 1f, 0.98f, 1.02f);
    }

    public void PlayRocketPopSound()
    {
        PlaySoundWithRandomPitch(rocketPopSound, 1f, 0.95f, 1.05f);
    }

    public void PlayComboRocketPopSound()
    {
        PlaySoundWithRandomPitch(comboRocketPopSound, 1.2f, 0.9f, 1.1f);
    }

    public void PlayCubeFallSound()
    {
        float randomPitch = Random.Range(fallSoundMinPitch, fallSoundMaxPitch);
        PlaySound(cubeFallSound, fallSoundVolume, randomPitch);
    }

    public void PlayGameLostSound()
    {
        PlaySound(gameLostSound, 1f);
    }

    public void PlayGameWonSound()
    {
        PlaySound(gameWonSound, 1f);
    }

    public void PlayGameWonSoundDelayed(float delay = 0.5f)
    {
        StartCoroutine(PlaySoundDelayed(gameWonSound, 1f, 1f, delay));
    }

    public void PlayGameLostSoundDelayed(float delay = 0.5f)
    {
        StartCoroutine(PlaySoundDelayed(gameLostSound, 1f, 1f, delay));
    }

    public void PlayButtonClickSound()
    {
        PlaySound(buttonClickSound, uiVolume, Random.Range(0.98f, 1.02f));
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
    }

    public int GetActiveAudioSourceCount()
    {
        return activeAudioSources.Count;
    }

    public int GetAvailableAudioSourceCount()
    {
        return availableAudioSources.Count;
    }
} 