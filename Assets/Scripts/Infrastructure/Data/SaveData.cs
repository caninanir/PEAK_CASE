using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int currentLevel = 1;
    public bool[] levelCompleted = new bool[999];
    
    public SaveData()
    {
        for (int i = 0; i < levelCompleted.Length; i++)
        {
            levelCompleted[i] = false;
        }
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(this);
        PlayerPrefs.SetString("SaveData", json);
        PlayerPrefs.Save();
    }

    public static SaveData Load()
    {
        if (PlayerPrefs.HasKey("SaveData"))
        {
            string json = PlayerPrefs.GetString("SaveData");
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            
            if (data.levelCompleted == null || data.levelCompleted.Length != 999)
            {
                bool[] oldCompleted = data.levelCompleted;
                data.levelCompleted = new bool[999];
                
                if (oldCompleted != null)
                {
                    int copyLength = Mathf.Min(oldCompleted.Length, data.levelCompleted.Length);
                    System.Array.Copy(oldCompleted, data.levelCompleted, copyLength);
                }
            }
            
            return data;
        }
        return new SaveData();
    }

    public void ResetProgress()
    {
        currentLevel = 1;
        for (int i = 0; i < levelCompleted.Length; i++)
        {
            levelCompleted[i] = false;
        }
        Save();
    }

    public bool IsLevelCompleted(int level)
    {
        int index = level - 1;
        return index >= 0 && index < levelCompleted.Length && levelCompleted[index];
    }

    public void MarkLevelCompleted(int level)
    {
        int index = level - 1;
        if (index >= 0 && index < levelCompleted.Length)
        {
            levelCompleted[index] = true;
        }
    }

    public bool AreAllLevelsCompleted()
    {
        List<int> allLevelNumbers = LevelManager.Instance.GetAllLevelNumbers();
        foreach (int levelNumber in allLevelNumbers)
        {
            if (!IsLevelCompleted(levelNumber))
            {
                return false;
            }
        }
        return allLevelNumbers.Count > 0;
    }
} 