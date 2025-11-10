using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelMusic
{
    public AudioClip clip;
    public List<int> levels = new List<int>();
}

public enum MusicTransitionState
{
    Idle,
    FadingIn,
    FadingOut,
    Crossfading
}

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Tracks")]
    [SerializeField] private AudioClip mainMenuClip;
    [SerializeField] private List<LevelMusic> levelTracks = new List<LevelMusic>();
    [SerializeField] private AudioClip gameWonMusic;
    [SerializeField] private AudioClip gameLostMusic;
    [SerializeField] private AudioClip gameFinishedMusic;

    [Header("Settings")]
    [SerializeField] private float musicVolume = 1f;
    [SerializeField] private float crossfadeDuration = 1.5f;
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private AudioSource primarySource;
    private AudioSource secondarySource;
    private AudioSource currentActiveSource;
    
    private MusicTransitionState transitionState = MusicTransitionState.Idle;
    private Coroutine activeTransition;
    private AudioClip targetClip;
    private AudioClip currentClip;
    
    private AudioClip queuedClip;
    private bool queuedIsEndGame;
    private bool queuedWon;
    
    private Coroutine gameFinishedMusicCoroutine;
    private bool isPlayingGameFinishedMusic = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
            InitializeAudioSources();
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

    private void Start()
    {
        SubscribeToEvents();
        HandleGameState(GameStateController.Instance.CurrentState);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);
        EventBus.Subscribe<LevelStartedEvent>(HandleLevelStarted);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);
        EventBus.Unsubscribe<LevelStartedEvent>(HandleLevelStarted);
    }

    private void HandleGameStateChanged(GameStateChangedEvent evt)
    {
        HandleGameState(evt.NewState);
    }

    private void HandleLevelStarted(LevelStartedEvent evt)
    {
        AudioClip levelClip = GetClipForLevel(evt.LevelNumber);
        PlayMusicClip(levelClip);
    }

    private void InitializeAudioSources()
    {
        GameObject primaryObj = new GameObject("PrimaryAudioSource");
        primaryObj.transform.SetParent(transform);
        primaryObj.transform.localPosition = Vector3.zero;
        primaryObj.transform.localRotation = Quaternion.identity;
        primaryObj.transform.localScale = Vector3.one;
        primarySource = primaryObj.AddComponent<AudioSource>();
        
        GameObject secondaryObj = new GameObject("SecondaryAudioSource");
        secondaryObj.transform.SetParent(transform);
        secondaryObj.transform.localPosition = Vector3.zero;
        secondaryObj.transform.localRotation = Quaternion.identity;
        secondaryObj.transform.localScale = Vector3.one;
        secondarySource = secondaryObj.AddComponent<AudioSource>();
        
        ConfigureAudioSource(primarySource);
        ConfigureAudioSource(secondarySource);
        
        currentActiveSource = primarySource;
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.loop = true;
        source.volume = 0f;
        source.playOnAwake = false;
    }

    private void HandleGameState(GameState state)
    {
        if (state == GameState.MainMenu)
        {
            if (!isPlayingGameFinishedMusic)
            {
                PlayMusicClip(mainMenuClip);
            }
        }
        else if (state == GameState.Finished)
        {
            PlayGameFinishedMusic();
        }
    }

    private AudioClip GetClipForLevel(int levelNumber)
    {
        foreach (LevelMusic levelMusic in levelTracks)
        {
            if (levelMusic.clip != null && levelMusic.levels.Contains(levelNumber))
            {
                return levelMusic.clip;
            }
        }
        return null;
    }

    private void PlayMusicClip(AudioClip newClip)
    {
        if (transitionState != MusicTransitionState.Idle)
        {
            queuedClip = newClip;
            queuedIsEndGame = false;
            return;
        }

        ExecuteMusicTransition(newClip, false);
    }

    public void PlayEndGameMusic(bool won)
    {
        AudioClip endClip = won ? gameWonMusic : gameLostMusic;

        if (transitionState != MusicTransitionState.Idle)
        {
            queuedClip = endClip;
            queuedIsEndGame = true;
            queuedWon = won;
            return;
        }

        ExecuteMusicTransition(endClip, true);
    }

    private void ExecuteMusicTransition(AudioClip newClip, bool isEndGame)
    {
        if (newClip == currentClip && currentActiveSource.isPlaying)
        {
            ProcessQueue();
            return;
        }

        targetClip = newClip;
        StopActiveTransition();

        if (!currentActiveSource.isPlaying)
        {
            StartDirectPlay(newClip, isEndGame);
        }
        else
        {
            StartCrossfade(newClip, isEndGame);
        }
    }

    private void StartDirectPlay(AudioClip clip, bool isEndGame)
    {
        transitionState = MusicTransitionState.FadingIn;
        currentActiveSource.clip = clip;
        currentActiveSource.loop = !isEndGame;
        currentActiveSource.volume = 0f;
        currentActiveSource.Play();
        
        currentClip = clip;
        activeTransition = StartCoroutine(DirectFadeIn());
    }

    private void StartCrossfade(AudioClip clip, bool isEndGame)
    {
        transitionState = MusicTransitionState.Crossfading;
        
        AudioSource inactiveSource = GetInactiveSource();
        inactiveSource.clip = clip;
        inactiveSource.loop = !isEndGame;
        inactiveSource.volume = 0f;
        inactiveSource.Play();
        
        activeTransition = StartCoroutine(CrossfadeTransition(inactiveSource));
    }

    private IEnumerator DirectFadeIn()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInDuration && transitionState == MusicTransitionState.FadingIn)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeInDuration);
            float curveValue = fadeInCurve.Evaluate(progress);
            
            currentActiveSource.volume = curveValue * musicVolume;
            
            yield return null;
        }

        if (transitionState == MusicTransitionState.FadingIn)
        {
            currentActiveSource.volume = musicVolume;
        }

        CompleteTransition();
    }

    private IEnumerator CrossfadeTransition(AudioSource incomingSource)
    {
        AudioSource outgoingSource = currentActiveSource;
        float elapsedTime = 0f;
        float startVolume = outgoingSource.volume;
        
        while (elapsedTime < crossfadeDuration && transitionState == MusicTransitionState.Crossfading)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / crossfadeDuration);
            
            float inVolume = fadeInCurve.Evaluate(progress) * musicVolume;
            float outVolume = fadeOutCurve.Evaluate(progress) * startVolume;
            
            incomingSource.volume = inVolume;
            outgoingSource.volume = outVolume;
            
            yield return null;
        }

        if (transitionState == MusicTransitionState.Crossfading)
        {
            incomingSource.volume = musicVolume;
            currentActiveSource = incomingSource;
            currentClip = incomingSource.clip;
        }
        
        outgoingSource.volume = 0f;
        outgoingSource.Stop();
        outgoingSource.clip = null;

        CompleteTransition();
    }

    private void CompleteTransition()
    {
        transitionState = MusicTransitionState.Idle;
        activeTransition = null;
        targetClip = null;
        
        ProcessQueue();
    }

    private void ProcessQueue()
    {
        if (queuedClip != null)
        {
            AudioClip nextClip = queuedClip;
            bool nextIsEndGame = queuedIsEndGame;
            bool nextWon = queuedWon;
            
            queuedClip = null;
            queuedIsEndGame = false;
            queuedWon = false;
            
            ExecuteMusicTransition(nextClip, nextIsEndGame);
        }
    }

    private AudioSource GetInactiveSource()
    {
        return currentActiveSource == primarySource ? secondarySource : primarySource;
    }

    private void StopActiveTransition()
    {
        if (activeTransition != null)
        {
            StopCoroutine(activeTransition);
            activeTransition = null;
        }
        
        transitionState = MusicTransitionState.Idle;
        targetClip = null;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        
        currentActiveSource.volume = musicVolume;
    }

    public void StopMusic()
    {
        StopActiveTransition();
        
        currentActiveSource.Stop();
        currentActiveSource.volume = 0f;
        
        currentClip = null;
        queuedClip = null;
        
        if (gameFinishedMusicCoroutine != null)
        {
            StopCoroutine(gameFinishedMusicCoroutine);
            gameFinishedMusicCoroutine = null;
            isPlayingGameFinishedMusic = false;
        }
    }
    
    private void PlayGameFinishedMusic()
    {
        if (gameFinishedMusicCoroutine != null)
        {
            StopCoroutine(gameFinishedMusicCoroutine);
        }
        
        ExecuteMusicTransition(gameFinishedMusic, true);
        isPlayingGameFinishedMusic = true;
        
        gameFinishedMusicCoroutine = StartCoroutine(WaitForGameFinishedMusicToEnd());
    }
    
    private IEnumerator WaitForGameFinishedMusicToEnd()
    {
        yield return new WaitUntil(() => currentActiveSource.isPlaying && currentActiveSource.clip == gameFinishedMusic);
        
        while (currentActiveSource.isPlaying && currentActiveSource.clip == gameFinishedMusic)
        {
            yield return null;
        }
        
        isPlayingGameFinishedMusic = false;
        PlayMusicClip(mainMenuClip);
        gameFinishedMusicCoroutine = null;
    }
}