using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }
    
    [Header("Scene Names")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string levelSceneName = "LevelScene";
    
    [Header("Transition Settings")]
    [SerializeField] private float preLoadDelay = 0.2f;
    [SerializeField] private float postLoadDelay = 0.5f;
    
    private bool isTransitioning = false;

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
    }

    public void LoadMainScene()
    {
        if (!isTransitioning)
        {
            StartCoroutine(LoadSceneWithTransition(mainSceneName));
        }
    }

    public void LoadLevelScene(int levelNumber)
    {
        if (!isTransitioning)
        {
            StartCoroutine(LoadSceneWithTransition(levelSceneName, levelNumber));
        }
    }

    private IEnumerator LoadSceneWithTransition(string sceneName, int levelNumber = 0)
    {
        if (isTransitioning) yield break;
        
        isTransitioning = true;
        
        bool fadeOutComplete = false;
        TransitionController.Instance.FadeOut(() => fadeOutComplete = true);
        
        while (!fadeOutComplete)
        {
            yield return null;
        }
        
        yield return new WaitForSecondsRealtime(preLoadDelay);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        
        asyncLoad.allowSceneActivation = true;
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        yield return new WaitForSecondsRealtime(postLoadDelay);
        
        if (sceneName == levelSceneName)
        {
            GameStateController.Instance.StartLevel(levelNumber);
            yield return new WaitForSecondsRealtime(0.2f);
        }
        
        yield return new WaitForEndOfFrame();
        
        InputController.Instance.OnCanvasChanged();
        
        bool fadeInComplete = false;
        TransitionController.Instance.FadeIn(() => 
        {
            fadeInComplete = true;
            OnTransitionComplete();
        });
        
        while (!fadeInComplete)
        {
            yield return null;
        }
        
        isTransitioning = false;
    }

    private void OnTransitionComplete()
    {
        InputController.Instance.OnCanvasChanged();
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    public bool IsInMainScene()
    {
        return GetCurrentSceneName() == mainSceneName;
    }

    public bool IsInLevelScene()
    {
        return GetCurrentSceneName() == levelSceneName;
    }

    public void QuickLoadMainScene()
    {
        if (!isTransitioning)
        {
            StartCoroutine(QuickTransition(mainSceneName));
        }
    }

    public void QuickLoadLevelScene(int levelNumber)
    {
        if (!isTransitioning)
        {
            StartCoroutine(QuickTransition(levelSceneName, levelNumber));
        }
    }

    private IEnumerator QuickTransition(string sceneName, int levelNumber = 0)
    {
        isTransitioning = true;
        
        bool fadeComplete = false;
        TransitionController.Instance.QuickFadeOut(() => fadeComplete = true);
        
        while (!fadeComplete)
        {
            yield return null;
        }
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        if (sceneName == levelSceneName)
        {
            GameStateController.Instance.StartLevel(levelNumber);
            yield return new WaitForSecondsRealtime(0.2f);
        }
        
        bool fadeInComplete = false;
        TransitionController.Instance.FadeIn(() => 
        {
            fadeInComplete = true;
            OnTransitionComplete();
        });
        
        while (!fadeInComplete)
        {
            yield return null;
        }
        
        isTransitioning = false;
    }
}
