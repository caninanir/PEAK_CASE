using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    private GameplayUIController gameplayUI;
    private MenuUIController menuUI;
    private PopupController popupController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
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
        InitializeControllers();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeControllers()
    {
        gameplayUI = FindFirstObjectByType<GameplayUIController>(FindObjectsInactive.Include);
        menuUI = FindFirstObjectByType<MenuUIController>(FindObjectsInactive.Include);
        popupController = FindFirstObjectByType<PopupController>(FindObjectsInactive.Include);
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void HandleGameStateChanged(GameStateChangedEvent evt)
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isMainScene = currentScene == "MainScene";
        bool isLevelScene = currentScene == "LevelScene";
        
        switch (evt.NewState)
        {
            case GameState.Playing:
                if (isLevelScene)
                {
                    EnableGameplayUI();
                }
                if (isMainScene)
                {
                    menuUI = FindFirstObjectByType<MenuUIController>(FindObjectsInactive.Include);
                    if (menuUI != null)
                    {
                        menuUI.gameObject.SetActive(false);
                    }
                }
                break;
            case GameState.GameWon:
            case GameState.GameLost:
                if (isLevelScene)
                {
                    DisableGameplayUI();
                }
                break;
        }
        
        StartCoroutine(HandleGameStateChangedDelayed(evt));
    }

    private System.Collections.IEnumerator HandleGameStateChangedDelayed(GameStateChangedEvent evt)
    {
        yield return new WaitForEndOfFrame();
        
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isMainScene = currentScene == "MainScene";
        bool isLevelScene = currentScene == "LevelScene";
        
        GameState currentState = GameStateController.Instance.CurrentState;
        
        switch (evt.NewState)
        {
            case GameState.MainMenu:
                if (isMainScene)
                {
                    menuUI = FindFirstObjectByType<MenuUIController>(FindObjectsInactive.Include);
                    if (menuUI != null)
                    {
                        menuUI.gameObject.SetActive(true);
                    }
                }
                if (isLevelScene)
                {
                    DisableGameplayUI();
                }
                break;
            case GameState.Playing:
                if (isLevelScene && currentState == GameState.Playing)
                {
                    EnableGameplayUI();
                }
                break;
            case GameState.GameWon:
            case GameState.GameLost:
                if (isLevelScene && (currentState == GameState.GameWon || currentState == GameState.GameLost))
                {
                    DisableGameplayUI();
                }
                break;
        }
    }

    public void EnableGameplayUI()
    {
        gameplayUI = FindFirstObjectByType<GameplayUIController>(FindObjectsInactive.Include);
        if (gameplayUI != null)
        {
            gameplayUI.gameObject.SetActive(true);
        }
    }

    private void DisableGameplayUI()
    {
        gameplayUI = FindFirstObjectByType<GameplayUIController>(FindObjectsInactive.Include);
        if (gameplayUI != null)
        {
            gameplayUI.gameObject.SetActive(false);
        }
    }

    private void ShowGameplayUI()
    {
        EnableGameplayUI();
    }

    private void HideGameplayUI()
    {
        DisableGameplayUI();
    }

    private void ShowMenuUI()
    {
        menuUI = FindFirstObjectByType<MenuUIController>(FindObjectsInactive.Include);
        if (menuUI != null)
        {
            menuUI.gameObject.SetActive(true);
        }
    }

    private void HideMenuUI()
    {
        menuUI = FindFirstObjectByType<MenuUIController>(FindObjectsInactive.Include);
        if (menuUI != null)
        {
            menuUI.gameObject.SetActive(false);
        }
    }
}

