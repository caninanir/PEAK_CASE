using UnityEngine;

public class GameStateController : MonoBehaviour
{
    public static GameStateController Instance { get; private set; }
    
    [SerializeField] private GameConfig config;
    
    private GameState currentState = GameState.MainMenu;
    private int currentLevel = 1;
    private int movesRemaining = 0;
    private bool isProcessingMove = false;
    private int activeAnimationCount = 0;
    private bool isGravityRunning = false;
    private System.Action<GravityStartedEvent> gravityStartedHandler;
    private System.Action<GravityCompletedEvent> gravityCompletedHandler;

    public GameState CurrentState => currentState;
    public int CurrentLevel => currentLevel;
    public int MovesRemaining => movesRemaining;
    public bool IsProcessingMove => isProcessingMove || activeAnimationCount > 0 || isGravityRunning || HasActiveParticles() || HasActiveProjectiles();
    public GameConfig Config => config;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
            SubscribeToEvents();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        gravityStartedHandler = _ => isGravityRunning = true;
        gravityCompletedHandler = _ => isGravityRunning = false;
        EventBus.Subscribe(gravityStartedHandler);
        EventBus.Subscribe(gravityCompletedHandler);
    }

    private void UnsubscribeFromEvents()
    {
        if (gravityStartedHandler != null)
        {
            EventBus.Unsubscribe(gravityStartedHandler);
            gravityStartedHandler = null;
        }
        if (gravityCompletedHandler != null)
        {
            EventBus.Unsubscribe(gravityCompletedHandler);
            gravityCompletedHandler = null;
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
        InitializeGameState();
    }

    private void InitializeGameState()
    {
        currentLevel = SaveManager.Instance.GetCurrentLevel();
        
        bool allLevelsCompleted = SaveManager.Instance.AreAllLevelsCompleted();
        
        if (allLevelsCompleted)
        {
            ChangeGameState(GameState.Finished);
        }
        else
        {
            if (!LevelManager.Instance.IsValidLevel(currentLevel))
            {
                int firstLevel = LevelManager.Instance.GetFirstLevel();
                SaveManager.Instance.SetCurrentLevel(firstLevel);
                currentLevel = firstLevel;
            }
            
            ChangeGameState(GameState.MainMenu);
        }
    }

    public void ChangeGameState(GameState newState)
    {
        if (currentState != newState)
        {
            GameState previousState = currentState;
            currentState = newState;
            
            EventBus.Publish(new GameStateChangedEvent
            {
                PreviousState = previousState,
                NewState = newState
            });
        }
    }

    public void StartLevel(int levelNumber)
    {
        CleanupVisualEffects();
        
        LevelData levelData = LevelManager.Instance.GetLevelData(levelNumber);
        
        currentLevel = levelNumber;
        movesRemaining = levelData.move_count;
        isProcessingMove = false;
        
        LevelManager.Instance.SetCurrentLevel(levelNumber);
        
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene == "LevelScene" && UIController.Instance != null)
        {
            UIController.Instance.EnableGameplayUI();
        }
        
        ChangeGameState(GameState.Playing);
        
        EventBus.Publish(new LevelStartedEvent { LevelNumber = currentLevel });
        EventBus.Publish(new MovesChangedEvent { MovesRemaining = movesRemaining });
    }
    
    private void CleanupVisualEffects()
    {
        RocketProjectileService projectileService = FindFirstObjectByType<RocketProjectileService>();
        if (projectileService != null)
        {
            projectileService.CleanupAllProjectiles();
        }
        
        ParticleEffectManager.Instance.CleanupAllParticles();
    }

    public void UseMove()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }
        
        movesRemaining = Mathf.Max(0, movesRemaining - 1);
        
        EventBus.Publish(new MovesChangedEvent { MovesRemaining = movesRemaining });
    }

    public bool CheckWinCondition()
    {
        return ObstacleController.Instance != null && ObstacleController.Instance.AreAllGoalsCleared();
    }

    public bool CheckLoseCondition()
    {
        return movesRemaining <= 0 && currentState == GameState.Playing && !CheckWinCondition();
    }

    public void WinLevel()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        MusicManager.Instance.PlayEndGameMusic(true);
        AudioManager.Instance.PlayGameWonSoundDelayed();

        SaveManager.Instance.MarkLevelCompleted(currentLevel);
        bool finished = SaveManager.Instance.AreAllLevelsCompleted();

        ChangeGameState(finished ? GameState.Finished : GameState.GameWon);

        EventBus.Publish(new LevelWonEvent { LevelNumber = currentLevel });
    }

    public void LoseLevel()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        MusicManager.Instance.PlayEndGameMusic(false);
        AudioManager.Instance.PlayGameLostSoundDelayed();
        
        ChangeGameState(GameState.GameLost);
        EventBus.Publish(new LevelLostEvent { LevelNumber = currentLevel });
    }

    public void RestartLevel()
    {
        StartLevel(currentLevel);
    }

    public void NextLevel()
    {
        int nextLevel = LevelManager.Instance.GetNextLevelAfter(currentLevel);
        if (nextLevel != -1)
        {
            StartLevel(nextLevel);
        }
        else
        {
            if (SaveManager.Instance.AreAllLevelsCompleted())
            {
                ChangeGameState(GameState.Finished);
            }
            
            ReturnToMainMenu();
        }
    }

    public void ReturnToMainMenu()
    {
        if (currentState != GameState.Finished)
        {
            ChangeGameState(GameState.MainMenu);
        }
        
        SceneTransitionManager.Instance.LoadMainScene();
    }

    public void SetProcessingMove(bool processing)
    {
        isProcessingMove = processing;
    }

    public void IncrementAnimationCount()
    {
        activeAnimationCount++;
    }

    public void DecrementAnimationCount()
    {
        activeAnimationCount = Mathf.Max(0, activeAnimationCount - 1);
    }

    private bool HasActiveParticles()
    {
        if (ParticleEffectManager.Instance == null) return false;
        return ParticleEffectManager.Instance.HasActiveParticles();
    }

    private bool HasActiveProjectiles()
    {
        RocketProjectileService projectileService = FindFirstObjectByType<RocketProjectileService>();
        return projectileService != null && projectileService.HasActiveProjectiles();
    }

    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }
}



