using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    private LevelData currentLevelData;
    private Dictionary<int, LevelData> allLevels = new Dictionary<int, LevelData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
            LoadAllLevels();
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

    private void LoadAllLevels()
    {
        TextAsset[] levelAssets = Resources.LoadAll<TextAsset>("Levels");
        
        foreach (TextAsset asset in levelAssets)
        {
            string fileName = asset.name;
            
            if (fileName.StartsWith("level_"))
            {
                string levelNumberStr = fileName.Substring(6);
                if (int.TryParse(levelNumberStr, out int levelNumber))
                {
                    try
                    {
                        LevelData levelData = JsonUtility.FromJson<LevelData>(asset.text);
                        if (levelData != null)
                        {
                            allLevels[levelNumber] = levelData;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
    }

    private LevelData LoadLevelFromJSON(int levelNumber)
    {
        string fileName = $"level_{levelNumber:D2}";
        TextAsset jsonFile = Resources.Load<TextAsset>($"Levels/{fileName}");
        
        if (jsonFile != null)
        {
            try
            {
                LevelData levelData = JsonUtility.FromJson<LevelData>(jsonFile.text);
                return levelData;
            }
            catch
            {
            }
        }
        
        return null;
    }

    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }

    public LevelData GetLevelData(int levelNumber)
    {
        if (allLevels.ContainsKey(levelNumber))
        {
            return allLevels[levelNumber];
        }
        
        return null;
    }

    public void SetCurrentLevel(int levelNumber)
    {
        currentLevelData = GetLevelData(levelNumber);
    }

    public bool IsLastLevel()
    {
        return currentLevelData != null && currentLevelData.level_number >= 10;
    }

    public bool IsValidLevel(int levelNumber)
    {
        return allLevels.ContainsKey(levelNumber);
    }

    public int GetTotalLevels()
    {
        return allLevels.Count;
    }
    
    public int GetNextLevelAfter(int currentLevel)
    {
        for (int i = currentLevel + 1; i <= 999; i++)
        {
            if (allLevels.ContainsKey(i))
            {
                return i;
            }
        }
        return -1;
    }
    
    public int GetFirstLevel()
    {
        for (int i = 1; i <= 999; i++)
        {
            if (allLevels.ContainsKey(i))
            {
                return i;
            }
        }
        return 1;
    }
    
    public List<int> GetAllLevelNumbers()
    {
        List<int> levelNumbers = new List<int>(allLevels.Keys);
        levelNumbers.Sort();
        return levelNumbers;
    }
    
    public bool HasMoreLevelsAfter(int currentLevel)
    {
        return GetNextLevelAfter(currentLevel) != -1;
    }
} 