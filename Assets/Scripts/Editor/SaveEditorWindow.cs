using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SaveEditorWindow : EditorWindow
{
    private int selectedLevel = 1;
    private Vector2 scrollPosition;
    
    [MenuItem("Peak Games/Save Editor")]
    public static void ShowWindow()
    {
        SaveEditorWindow window = GetWindow<SaveEditorWindow>("Save Editor");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Peak Games Save Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        DrawCurrentLevelInfo();
        EditorGUILayout.Space(15);
        
        DrawLevelSelector();
        EditorGUILayout.Space(15);
        
        DrawLevelControls();
        EditorGUILayout.Space(15);
        
        DrawProgressControls();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawCurrentLevelInfo()
    {
        EditorGUILayout.LabelField("Current Save Information", EditorStyles.boldLabel);
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Save information is only available during play mode or after game has been run at least once.", MessageType.Info);
            return;
        }
        
        if (SaveManager.Instance != null && LevelManager.Instance != null)
        {
            int currentLevel = SaveManager.Instance.GetCurrentLevel();
            bool allCompleted = SaveManager.Instance.AreAllLevelsCompleted();
            int totalLevels = LevelManager.Instance.GetTotalLevels();
            
            EditorGUILayout.LabelField($"Current Level: {currentLevel}");
            EditorGUILayout.LabelField($"Total Levels Available: {totalLevels}");
            EditorGUILayout.LabelField($"All Levels Completed: {(allCompleted ? "Yes" : "No")}");
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Level Completion Status:");
            
            List<int> allLevelNumbers = LevelManager.Instance.GetAllLevelNumbers();
            int displayCount = Mathf.Min(20, allLevelNumbers.Count);
            
            for (int i = 0; i < displayCount; i++)
            {
                int levelNumber = allLevelNumbers[i];
                bool completed = SaveManager.Instance.IsLevelCompleted(levelNumber);
                string status = completed ? "✓" : "✗";
                EditorGUILayout.LabelField($"  Level {levelNumber}: {status}");
            }
            
            if (allLevelNumbers.Count > displayCount)
            {
                EditorGUILayout.LabelField($"  ... and {allLevelNumbers.Count - displayCount} more levels");
            }
        }
        else
        {
            EditorGUILayout.LabelField("SaveManager or LevelManager not available");
        }
    }
    
    private void DrawLevelSelector()
    {
        EditorGUILayout.LabelField("Set Current Level", EditorStyles.boldLabel);
        
        if (Application.isPlaying && LevelManager.Instance != null)
        {
            List<int> availableLevels = LevelManager.Instance.GetAllLevelNumbers();
            
            if (availableLevels.Count > 0)
            {
                string[] levelOptions = new string[availableLevels.Count];
                int currentIndex = 0;
                
                for (int i = 0; i < availableLevels.Count; i++)
                {
                    levelOptions[i] = $"Level {availableLevels[i]}";
                    if (availableLevels[i] == selectedLevel)
                    {
                        currentIndex = i;
                    }
                }
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Available Levels:", GUILayout.Width(120));
                int newIndex = EditorGUILayout.Popup(currentIndex, levelOptions);
                if (newIndex != currentIndex)
                {
                    selectedLevel = availableLevels[newIndex];
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No levels found! Create levels using the Level Editor.", MessageType.Warning);
            }
        }
        else
        {
            selectedLevel = EditorGUILayout.IntField("Level Number", selectedLevel);
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button($"Set Current Level to {selectedLevel}"))
        {
            SetCurrentLevel(selectedLevel);
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("This sets the player's current level. The level button in the main menu will show this level.", MessageType.Info);
    }
    
    private void DrawLevelControls()
    {
        EditorGUILayout.LabelField("Individual Level Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Complete Current Level"))
        {
            CompleteCurrentLevel();
        }
        if (GUILayout.Button("Uncomplete Current Level"))
        {
            UncompleteCurrentLevel();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
    }
    
    private void DrawProgressControls()
    {
        EditorGUILayout.LabelField("Progress Management", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset All Progress"))
        {
            if (EditorUtility.DisplayDialog("Reset Progress", 
                "Are you sure you want to reset all progress? This will set the current level to 1 and mark all levels as incomplete.", 
                "Yes", "Cancel"))
            {
                ResetProgress();
            }
        }
        
        if (GUILayout.Button("Complete All Levels"))
        {
            if (EditorUtility.DisplayDialog("Complete All Levels", 
                "Are you sure you want to mark all levels as completed?", 
                "Yes", "Cancel"))
            {
                CompleteAllLevels();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Changes are saved immediately to PlayerPrefs. These changes will persist between editor and build sessions.", MessageType.Warning);
        
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Game is running - UI changes will be applied in real-time to MainMenu and Level scenes.", MessageType.Info);
        }
    }
    
    private void SetCurrentLevel(int level)
    {
        level = Mathf.Max(1, level);
        
        if (Application.isPlaying && SaveManager.Instance != null)
        {
            SaveManager.Instance.SetCurrentLevel(level);
        }
        else
        {
            SaveData saveData = SaveData.Load();
            saveData.currentLevel = level;
            saveData.Save();
        }
        
        RefreshGameUI();
        
        EditorUtility.DisplayDialog("Level Set", $"Current level set to {level}", "OK");
        Repaint();
    }
    
    private void CompleteCurrentLevel()
    {
        int currentLevel = selectedLevel;
        
        SaveData saveData = SaveData.Load();
        saveData.MarkLevelCompleted(currentLevel);
        saveData.Save();
        
        if (Application.isPlaying && SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkLevelCompleted(currentLevel);
        }
        
        RefreshGameUI();
        Repaint();
    }
    
    private void UncompleteCurrentLevel()
    {
        int currentLevel = selectedLevel;
        
        SaveData saveData = SaveData.Load();
        int index = currentLevel - 1;
        if (index >= 0 && index < saveData.levelCompleted.Length)
        {
            saveData.levelCompleted[index] = false;
        }
        saveData.Save();
        
        RefreshGameUI();
        Repaint();
    }
    
    private void ResetProgress()
    {
        SaveData saveData = SaveData.Load();
        saveData.ResetProgress();
        
        if (Application.isPlaying && SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetProgress();
        }
        
        selectedLevel = 1;
        RefreshGameUI();
        Repaint();
    }
    
    private void CompleteAllLevels()
    {
        SaveData saveData = SaveData.Load();
        
        if (Application.isPlaying && LevelManager.Instance != null)
        {
            List<int> availableLevels = LevelManager.Instance.GetAllLevelNumbers();
            
            foreach (int levelNumber in availableLevels)
            {
                saveData.MarkLevelCompleted(levelNumber);
                
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.MarkLevelCompleted(levelNumber);
                }
            }
            
            if (availableLevels.Count > 0)
            {
                saveData.currentLevel = availableLevels[availableLevels.Count - 1] + 1;
            }
        }
        else
        {
            string levelsPath = "Assets/Resources/levels/";
            if (Directory.Exists(levelsPath))
            {
                string[] levelFiles = Directory.GetFiles(levelsPath, "level_*.json");
                List<int> availableLevels = new List<int>();
                
                foreach (string filePath in levelFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (fileName.StartsWith("level_"))
                    {
                        string levelNumberStr = fileName.Substring(6);
                        if (int.TryParse(levelNumberStr, out int levelNumber))
                        {
                            availableLevels.Add(levelNumber);
                        }
                    }
                }
                
                availableLevels.Sort();
                
                foreach (int levelNumber in availableLevels)
                {
                    saveData.MarkLevelCompleted(levelNumber);
                }
                
                if (availableLevels.Count > 0)
                {
                    saveData.currentLevel = availableLevels[availableLevels.Count - 1] + 1;
                }
            }
            else
            {
                for (int i = 1; i <= 10; i++)
                {
                    saveData.MarkLevelCompleted(i);
                }
                saveData.currentLevel = 11;
            }
        }
        
        saveData.Save();
        
        if (Application.isPlaying && SaveManager.Instance != null)
        {
            SaveManager.Instance.SetCurrentLevel(saveData.currentLevel);
        }
        
        RefreshGameUI();
        Repaint();
    }
    
    private void RefreshGameUI()
    {
        if (!Application.isPlaying) return;
        
        MenuUIController mainMenuUI = Object.FindFirstObjectByType<MenuUIController>();
        if (mainMenuUI != null)
        {
            mainMenuUI.UpdateUI();
        }
        
        GameplayUIController gameplayUI = Object.FindFirstObjectByType<GameplayUIController>();
        if (gameplayUI != null && GameStateController.Instance != null)
        {
            int currentGameLevel = GameStateController.Instance.CurrentLevel;
            int saveLevel = SaveManager.Instance?.GetCurrentLevel() ?? 1;
            
            if (currentGameLevel != saveLevel)
            {
                GameStateController.Instance.StartLevel(saveLevel);
            }
        }
    }
} 