using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayTestController : MonoBehaviour
{
    public static GameplayTestController Instance { get; private set; }

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

    private void TestWin()
    {
        GameStateController.Instance.WinLevel();
    }

    private void TestLose()
    {
        GameStateController.Instance.LoseLevel();
    }

    private void TestUseMove()
    {
        GameStateController.Instance.UseMove();
    }

    private void Update()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isLevelScene = currentScene == "LevelScene";
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            TestWin();
        }
        
        if (Input.GetKeyDown(KeyCode.L))
        {
            TestLose();
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestUseMove();
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            DebugGridInfo();
        }
        
        if (!isLevelScene)
        {
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            LoadDebugLevel(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            LoadDebugLevel(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            LoadDebugLevel(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            LoadDebugLevel(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            LoadDebugLevel(5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
        {
            LoadDebugLevel(6);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
        {
            LoadDebugLevel(7);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
        {
            LoadDebugLevel(8);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
        {
            LoadDebugLevel(9);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            LoadDebugLevel(10);
        }
    }

    private void DebugGridInfo()
    {
        if (GridController.Instance != null)
        {
            Vector2Int gridSize = GridController.Instance.DataService.GetGridSize();
            
            for (int x = 0; x < Mathf.Min(3, gridSize.x); x++)
            {
                for (int y = 0; y < Mathf.Min(3, gridSize.y); y++)
                {
                    BaseItem item = GridController.Instance.DataService.GetItem(x, y);
                }
            }
        }
    }

    private void LoadDebugLevel(int levelNumber)
    {
        GameStateController.Instance.StartLevel(levelNumber);
    }
} 