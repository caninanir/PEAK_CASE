using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    
    private SaveData currentSave;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
            LoadSave();
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

    public SaveData LoadSave()
    {
        currentSave = SaveData.Load();
        return currentSave;
    }

    public void SaveGame()
    {
        if (currentSave != null)
        {
            currentSave.Save();
        }
    }

    public int GetCurrentLevel()
    {
        return currentSave?.currentLevel ?? 1;
    }

    public void SetCurrentLevel(int level)
    {
        if (currentSave != null)
        {
            currentSave.currentLevel = Mathf.Max(1, level);
            SaveGame();
        }
    }

    public bool IsLevelCompleted(int level)
    {
        return currentSave?.IsLevelCompleted(level) ?? false;
    }

    public void MarkLevelCompleted(int level)
    {
        if (currentSave != null)
        {
            currentSave.MarkLevelCompleted(level);
            
            if (level >= currentSave.currentLevel)
            {
                int nextLevel = LevelManager.Instance.GetNextLevelAfter(level);
                if (nextLevel != -1)
                {
                    currentSave.currentLevel = nextLevel;
                }
                else
                {
                    currentSave.currentLevel = level + 1;
                }
            }
            
            SaveGame();
        }
    }

    public bool AreAllLevelsCompleted()
    {
        return currentSave?.AreAllLevelsCompleted() ?? false;
    }

    public void ResetProgress()
    {
        if (currentSave != null)
        {
            currentSave.ResetProgress();
        }
    }

    public SaveData GetCurrentSave()
    {
        return currentSave;
    }
} 