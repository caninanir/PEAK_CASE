using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class LevelEditorWindow : EditorWindow
{
    private LevelData currentLevel;
    private int editingLevelNumber = 1;
    private new bool hasUnsavedChanges = false;
    
    private int gridWidth = 6;
    private int gridHeight = 6;
    private int moveCount = 15;
    
    private Vector2 scrollPosition;
    private ItemType selectedTool = ItemType.RedCube;
    private const float CELL_SIZE = 40f;
    private const float GRID_PADDING = 20f;
    
    private Dictionary<ItemType, int> cubeGoals = new Dictionary<ItemType, int>();
    
    private readonly Dictionary<ItemType, Color> itemColors = new Dictionary<ItemType, Color>
    {
        { ItemType.Empty, Color.white },
        { ItemType.RedCube, Color.red },
        { ItemType.GreenCube, Color.green },
        { ItemType.BlueCube, Color.blue },
        { ItemType.YellowCube, Color.yellow },
        { ItemType.PurpleCube, new Color(0.5f, 0f, 1f) },
        { ItemType.RandomCube, Color.white },
        { ItemType.HorizontalRocket, new Color(1f, 0.5f, 0f) },
        { ItemType.VerticalRocket, Color.cyan },
        { ItemType.Balloon, new Color(1f, 0.5f, 0.8f) },
        { ItemType.Duck, new Color(1f, 0.8f, 0f) }
    };
    
    [MenuItem("Peak Games/Level Editor")]
    public static void ShowWindow()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }
    
    private void OnEnable()
    {
        InitializeNewLevel();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHeader();
        DrawLevelControls();
        DrawGridSettings();
        DrawCubeGoals();
        DrawToolPalette();
        DrawGrid();
        DrawFileOperations();
        
        EditorGUILayout.EndScrollView();
        
        if (hasUnsavedChanges)
        {
            EditorGUILayout.HelpBox("Level has unsaved changes!", MessageType.Warning);
        }
    }
    
    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Peak Games Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Editing: Level {editingLevelNumber}", EditorStyles.boldLabel);
        
        if (hasUnsavedChanges)
        {
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("●", GUILayout.Width(20));
            GUI.color = Color.white;
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawLevelControls()
    {
        EditorGUILayout.LabelField("Level Management", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Level Number:", GUILayout.Width(100));
        int newLevelNumber = EditorGUILayout.IntField(editingLevelNumber, GUILayout.Width(50));
        newLevelNumber = Mathf.Clamp(newLevelNumber, 1, 99);
        
        if (newLevelNumber != editingLevelNumber)
        {
            if (hasUnsavedChanges)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes", 
                    "You have unsaved changes. Save before changing levels?", 
                    "Save & Continue", "Discard Changes"))
                {
                    SaveCurrentLevel();
                }
            }
            
            editingLevelNumber = newLevelNumber;
            LoadLevel(editingLevelNumber);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New Level"))
        {
            if (ConfirmUnsavedChanges())
            {
                InitializeNewLevel();
            }
        }
        
        if (GUILayout.Button("Load Level"))
        {
            if (ConfirmUnsavedChanges())
            {
                LoadLevel(editingLevelNumber);
            }
        }
        
        if (GUILayout.Button("Duplicate Level"))
        {
            DuplicateLevel();
        }
        
        if (GUILayout.Button("Insert Level Here"))
        {
            InsertLevelHere();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
    }
    
    private void DrawGridSettings()
    {
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Going larger than 10x13 or smaller than 3x3 may cause issues", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Size:", GUILayout.Width(60));
        int newWidth = EditorGUILayout.IntField(gridWidth, GUILayout.Width(50));
        EditorGUILayout.LabelField("x", GUILayout.Width(10));
        int newHeight = EditorGUILayout.IntField(gridHeight, GUILayout.Width(50));
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);
        EditorGUILayout.EndHorizontal();
        
        if (newWidth != gridWidth || newHeight != gridHeight)
        {
            ResizeGrid(newWidth, newHeight);
        }
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Move Count:", GUILayout.Width(80));
        int newMoveCount = EditorGUILayout.IntField(moveCount);
        newMoveCount = Mathf.Max(1, newMoveCount);
        
        if (newMoveCount != moveCount)
        {
            moveCount = newMoveCount;
            MarkAsChanged();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Grid"))
        {
            if (EditorUtility.DisplayDialog("Clear Grid", "Clear all items from the grid?", "Yes", "Cancel"))
            {
                ClearGrid();
            }
        }
        
        if (GUILayout.Button("Fill Random Cubes"))
        {
            FillRandomCubes();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
    }

    private void DrawCubeGoals()
    {
        EditorGUILayout.LabelField("Cube Goals", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Set target counts for colored cubes. Cubes destroyed through any means will count towards these goals.", MessageType.Info);
        
        ItemType[] cubeTypes = { ItemType.RedCube, ItemType.GreenCube, ItemType.BlueCube, ItemType.YellowCube, ItemType.PurpleCube };
        
        foreach (ItemType cubeType in cubeTypes)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{cubeType}:", GUILayout.Width(100));
            
            int currentValue = cubeGoals.ContainsKey(cubeType) ? cubeGoals[cubeType] : 0;
            int newValue = EditorGUILayout.IntField(currentValue, GUILayout.Width(60));
            newValue = Mathf.Max(0, newValue);
            
            if (newValue != currentValue)
            {
                if (newValue > 0)
                {
                    cubeGoals[cubeType] = newValue;
                }
                else
                {
                    cubeGoals.Remove(cubeType);
                }
                MarkAsChanged();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(10);
    }
    
    private void DrawToolPalette()
    {
        EditorGUILayout.LabelField("Tool Palette", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        DrawToolButton(ItemType.Empty, "Empty");
        DrawToolButton(ItemType.RedCube, "Red");
        DrawToolButton(ItemType.GreenCube, "Green");
        DrawToolButton(ItemType.BlueCube, "Blue");
        DrawToolButton(ItemType.YellowCube, "Yellow");
        DrawToolButton(ItemType.PurpleCube, "Purple");
        DrawToolButton(ItemType.RandomCube, "Random");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        DrawToolButton(ItemType.HorizontalRocket, "H-Rocket");
        DrawToolButton(ItemType.VerticalRocket, "V-Rocket");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        DrawToolButton(ItemType.Balloon, "Balloon");
        DrawToolButton(ItemType.Duck, "Duck");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
    }
    
    private void DrawToolButton(ItemType itemType, string label)
    {
        Color originalColor = GUI.color;
        GUI.color = itemColors[itemType];
        
        if (selectedTool == itemType)
        {
            GUI.color = Color.Lerp(itemColors[itemType], Color.white, 0.5f);
        }
        
        if (GUILayout.Button(label, GUILayout.Height(30)))
        {
            selectedTool = itemType;
        }
        
        GUI.color = originalColor;
    }
    
    private void DrawGrid()
    {
        EditorGUILayout.LabelField("Level Grid (Click to paint)", EditorStyles.boldLabel);
        
        Rect gridRect = GUILayoutUtility.GetRect(
            GRID_PADDING * 2 + gridWidth * CELL_SIZE, 
            GRID_PADDING * 2 + gridHeight * CELL_SIZE);
        
        EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f));
        
        Event current = Event.current;
        if (gridRect.Contains(current.mousePosition))
        {
            if (current.type == EventType.MouseDown || 
                (current.type == EventType.MouseDrag && current.button == 0))
            {
                Vector2 localPos = current.mousePosition - gridRect.position;
                int x = Mathf.FloorToInt((localPos.x - GRID_PADDING) / CELL_SIZE);
                int y = Mathf.FloorToInt((localPos.y - GRID_PADDING) / CELL_SIZE);
                
                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                {
                    int levelY = gridHeight - 1 - y;
                    int index = levelY * gridWidth + x;
                    
                    if (index >= 0 && index < currentLevel.grid.Length)
                    {
                        string newItem = ItemTypeToString(selectedTool);
                        if (currentLevel.grid[index] != newItem)
                        {
                            currentLevel.grid[index] = newItem;
                            MarkAsChanged();
                        }
                    }
                }
                
                current.Use();
                Repaint();
            }
        }
        
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + GRID_PADDING + x * CELL_SIZE,
                    gridRect.y + GRID_PADDING + y * CELL_SIZE,
                    CELL_SIZE - 2,
                    CELL_SIZE - 2);
                
                int levelY = gridHeight - 1 - y;
                int index = levelY * gridWidth + x;
                
                if (index >= 0 && index < currentLevel.grid.Length)
                {
                    ItemType itemType = currentLevel.ParseGridItem(currentLevel.grid[index]);
                    Color cellColor = itemColors[itemType];
                    
                    EditorGUI.DrawRect(cellRect, cellColor);
                    
                    EditorGUI.DrawRect(new Rect(cellRect.x - 1, cellRect.y - 1, cellRect.width + 2, 1), Color.black);
                    EditorGUI.DrawRect(new Rect(cellRect.x - 1, cellRect.y + cellRect.height, cellRect.width + 2, 1), Color.black);
                    EditorGUI.DrawRect(new Rect(cellRect.x - 1, cellRect.y - 1, 1, cellRect.height + 2), Color.black);
                    EditorGUI.DrawRect(new Rect(cellRect.x + cellRect.width, cellRect.y - 1, 1, cellRect.height + 2), Color.black);
                    
                    if (itemType != ItemType.Empty && !IsColoredCube(itemType))
                    {
                        string symbol = GetItemSymbol(itemType);
                        GUI.Label(cellRect, symbol, GetCenteredStyle());
                    }
                }
            }
        }
        
        for (int x = 0; x < gridWidth; x++)
        {
            GUI.Label(new Rect(gridRect.x + GRID_PADDING + x * CELL_SIZE, gridRect.y + 5, CELL_SIZE, 20), 
                x.ToString(), GetCenteredStyle());
        }
        
        for (int y = 0; y < gridHeight; y++)
        {
            int levelY = gridHeight - 1 - y;
            GUI.Label(new Rect(gridRect.x + 5, gridRect.y + GRID_PADDING + y * CELL_SIZE, 20, CELL_SIZE), 
                levelY.ToString(), GetCenteredStyle());
        }
        
        EditorGUILayout.Space(20);
    }
    
    private void DrawFileOperations()
    {
        EditorGUILayout.LabelField("File Operations", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Level"))
        {
            SaveCurrentLevel();
        }
        
        if (GUILayout.Button("Save As New Level"))
        {
            SaveAsNewLevel();
        }
        
        if (GUILayout.Button("Delete Level"))
        {
            DeleteCurrentLevel();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "• Save Level: Overwrites existing level file\n" +
            "• Save As New: Creates level with next available number\n" +
            "• Duplicate: Copies current level to new number\n" +
            "• Insert Level Here: Pushes existing levels up by 1\n" +
            "• Delete: Permanently removes level file", 
            MessageType.Info);
        
        EditorGUILayout.Space(5);
        DrawLevelStats();
    }
    
    private void DrawLevelStats()
    {
        if (currentLevel == null) return;
        
        EditorGUILayout.LabelField("Level Statistics", EditorStyles.boldLabel);
        
        Dictionary<ItemType, int> counts = new Dictionary<ItemType, int>();
        foreach (string item in currentLevel.grid)
        {
            ItemType itemType = currentLevel.ParseGridItem(item);
            counts[itemType] = counts.ContainsKey(itemType) ? counts[itemType] + 1 : 1;
        }
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Cubes:");
        foreach (var kvp in counts)
        {
            if (IsColoredCube(kvp.Key) || kvp.Key == ItemType.RandomCube)
            {
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Obstacles:");
        foreach (var kvp in counts)
        {
            if (kvp.Key == ItemType.Balloon || kvp.Key == ItemType.Duck)
            {
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Rockets:");
        foreach (var kvp in counts)
        {
            if (kvp.Key == ItemType.HorizontalRocket || kvp.Key == ItemType.VerticalRocket)
            {
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }
    
    private void InitializeNewLevel()
    {
        currentLevel = new LevelData
        {
            level_number = editingLevelNumber,
            grid_width = gridWidth,
            grid_height = gridHeight,
            move_count = moveCount,
            grid = new string[gridWidth * gridHeight],
            cube_goals = new CubeGoalEntry[0]
        };
        
        cubeGoals.Clear();
        ClearGrid();
        hasUnsavedChanges = false;
    }
    
    private void LoadLevel(int levelNumber)
    {
        string fileName = $"level_{levelNumber:D2}";
        string path = $"Assets/Resources/levels/{fileName}.json";
        
        if (File.Exists(path))
        {
            string jsonContent = File.ReadAllText(path);
            currentLevel = JsonUtility.FromJson<LevelData>(jsonContent);
            
            gridWidth = currentLevel.grid_width;
            gridHeight = currentLevel.grid_height;
            moveCount = currentLevel.move_count;
            editingLevelNumber = currentLevel.level_number;
            
            cubeGoals.Clear();
            if (currentLevel.cube_goals != null)
            {
                foreach (var entry in currentLevel.cube_goals)
                {
                    ItemType cubeType = currentLevel.ParseGridItem(entry.cube_type);
                    cubeGoals[cubeType] = entry.count;
                }
            }
            
            hasUnsavedChanges = false;
        }
        else
        {
            InitializeNewLevel();
        }
    }
    
    private void SaveCurrentLevel()
    {
        if (currentLevel == null) return;
        
        currentLevel.level_number = editingLevelNumber;
        currentLevel.grid_width = gridWidth;
        currentLevel.grid_height = gridHeight;
        currentLevel.move_count = moveCount;
        
        List<CubeGoalEntry> goalEntries = new List<CubeGoalEntry>();
        foreach (var kvp in cubeGoals)
        {
            if (kvp.Value > 0)
            {
                goalEntries.Add(new CubeGoalEntry
                {
                    cube_type = ItemTypeToString(kvp.Key),
                    count = kvp.Value
                });
            }
        }
        currentLevel.cube_goals = goalEntries.ToArray();
        
        string fileName = $"level_{editingLevelNumber:D2}";
        string path = $"Assets/Resources/levels/{fileName}.json";
        
        string jsonContent = JsonUtility.ToJson(currentLevel, true);
        File.WriteAllText(path, jsonContent);
        
        AssetDatabase.Refresh();
        hasUnsavedChanges = false;
        
        EditorUtility.DisplayDialog("Level Saved", $"Level {editingLevelNumber} saved successfully!", "OK");
    }
    
    private void SaveAsNewLevel()
    {
        int newLevelNumber = GetNextAvailableLevelNumber();
        
        if (EditorUtility.DisplayDialog("Save As New Level", 
            $"Save as level {newLevelNumber}?", "Yes", "Cancel"))
        {
            editingLevelNumber = newLevelNumber;
            SaveCurrentLevel();
        }
    }
    
    private void DuplicateLevel()
    {
        int newLevelNumber = GetNextAvailableLevelNumber();
        
        if (EditorUtility.DisplayDialog("Duplicate Level", 
            $"Duplicate current level as level {newLevelNumber}?", "Yes", "Cancel"))
        {
            LevelData duplicatedLevel = new LevelData
            {
                level_number = newLevelNumber,
                grid_width = currentLevel.grid_width,
                grid_height = currentLevel.grid_height,
                move_count = currentLevel.move_count,
                grid = new string[currentLevel.grid.Length]
            };
            
            System.Array.Copy(currentLevel.grid, duplicatedLevel.grid, currentLevel.grid.Length);
            
            if (currentLevel.cube_goals != null && currentLevel.cube_goals.Length > 0)
            {
                duplicatedLevel.cube_goals = new CubeGoalEntry[currentLevel.cube_goals.Length];
                System.Array.Copy(currentLevel.cube_goals, duplicatedLevel.cube_goals, currentLevel.cube_goals.Length);
            }
            else
            {
                duplicatedLevel.cube_goals = new CubeGoalEntry[0];
            }
            
            string fileName = $"level_{newLevelNumber:D2}";
            string path = $"Assets/Resources/levels/{fileName}.json";
            
            string jsonContent = JsonUtility.ToJson(duplicatedLevel, true);
            File.WriteAllText(path, jsonContent);
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Level Duplicated", $"Level duplicated as level {newLevelNumber}!", "OK");
        }
    }
    
    private void InsertLevelHere()
    {
        if (EditorUtility.DisplayDialog("Insert Level", 
            $"Insert current level at position {editingLevelNumber}?\n\nThis will push existing levels {editingLevelNumber}+ up by 1.", 
            "Yes", "Cancel"))
        {
            int highestLevel = GetHighestLevelNumber();
            
            for (int i = highestLevel; i >= editingLevelNumber; i--)
            {
                string oldFileName = $"level_{i:D2}";
                string newFileName = $"level_{(i + 1):D2}";
                string oldPath = $"Assets/Resources/levels/{oldFileName}.json";
                string newPath = $"Assets/Resources/levels/{newFileName}.json";
                
                if (File.Exists(oldPath))
                {
                    string jsonContent = File.ReadAllText(oldPath);
                    LevelData levelData = JsonUtility.FromJson<LevelData>(jsonContent);
                    levelData.level_number = i + 1;
                    
                    string updatedJson = JsonUtility.ToJson(levelData, true);
                    File.WriteAllText(newPath, updatedJson);
                    
                    File.Delete(oldPath);
                    if (File.Exists(oldPath + ".meta"))
                    {
                        File.Delete(oldPath + ".meta");
                    }
                }
            }
            
            SaveCurrentLevel();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Level Inserted", 
                $"Level inserted at position {editingLevelNumber}! Higher levels have been renumbered.", "OK");
        }
    }
    
    private void DeleteCurrentLevel()
    {
        if (EditorUtility.DisplayDialog("Delete Level", 
            $"Delete level {editingLevelNumber}? This cannot be undone!", "Delete", "Cancel"))
        {
            string fileName = $"level_{editingLevelNumber:D2}";
            string path = $"Assets/Resources/levels/{fileName}.json";
            
            if (File.Exists(path))
            {
                File.Delete(path);
                File.Delete(path + ".meta");
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("Level Deleted", $"Level {editingLevelNumber} deleted!", "OK");
                
                InitializeNewLevel();
            }
        }
    }
    
    private void ResizeGrid(int newWidth, int newHeight)
    {
        string[] oldGrid = currentLevel.grid;
        int oldWidth = gridWidth;
        int oldHeight = gridHeight;
        
        gridWidth = newWidth;
        gridHeight = newHeight;
        
        currentLevel.grid = new string[gridWidth * gridHeight];
        currentLevel.grid_width = gridWidth;
        currentLevel.grid_height = gridHeight;
        
        for (int i = 0; i < currentLevel.grid.Length; i++)
        {
            currentLevel.grid[i] = "";
        }
        
        for (int y = 0; y < Mathf.Min(oldHeight, gridHeight); y++)
        {
            for (int x = 0; x < Mathf.Min(oldWidth, gridWidth); x++)
            {
                int oldIndex = y * oldWidth + x;
                int newIndex = y * gridWidth + x;
                
                if (oldIndex < oldGrid.Length)
                {
                    currentLevel.grid[newIndex] = oldGrid[oldIndex];
                }
            }
        }
        
        MarkAsChanged();
    }
    
    private void ClearGrid()
    {
        for (int i = 0; i < currentLevel.grid.Length; i++)
        {
            currentLevel.grid[i] = "";
        }
        MarkAsChanged();
    }
    
    private void FillRandomCubes()
    {
        ItemType[] cubeTypes = { ItemType.RedCube, ItemType.GreenCube, ItemType.BlueCube, ItemType.YellowCube, ItemType.PurpleCube };
        
        for (int i = 0; i < currentLevel.grid.Length; i++)
        {
            if (string.IsNullOrEmpty(currentLevel.grid[i]))
            {
                ItemType randomCube = cubeTypes[Random.Range(0, cubeTypes.Length)];
                currentLevel.grid[i] = ItemTypeToString(randomCube);
            }
        }
        MarkAsChanged();
    }
    
    private void MarkAsChanged()
    {
        hasUnsavedChanges = true;
    }
    
    private bool ConfirmUnsavedChanges()
    {
        if (!hasUnsavedChanges) return true;
        
        return EditorUtility.DisplayDialog("Unsaved Changes", 
            "You have unsaved changes. Continue without saving?", 
            "Continue", "Cancel");
    }
    
    private int GetNextAvailableLevelNumber()
    {
        for (int i = 1; i <= 99; i++)
        {
            string fileName = $"level_{i:D2}";
            string path = $"Assets/Resources/levels/{fileName}.json";
            if (!File.Exists(path))
            {
                return i;
            }
        }
        return 99;
    }
    
    private int GetHighestLevelNumber()
    {
        int highest = 0;
        for (int i = 1; i <= 99; i++)
        {
            string fileName = $"level_{i:D2}";
            string path = $"Assets/Resources/levels/{fileName}.json";
            if (File.Exists(path))
            {
                highest = i;
            }
        }
        return highest;
    }
    
    private string ItemTypeToString(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.RedCube: return "r";
            case ItemType.GreenCube: return "g";
            case ItemType.BlueCube: return "b";
            case ItemType.YellowCube: return "y";
            case ItemType.PurpleCube: return "p";
            case ItemType.RandomCube: return "rand";
            case ItemType.HorizontalRocket: return "hro";
            case ItemType.VerticalRocket: return "vro";
            case ItemType.Balloon: return "ba";
            case ItemType.Duck: return "du";
            default: return "";
        }
    }
    
    private bool IsColoredCube(ItemType itemType)
    {
        return itemType == ItemType.RedCube || itemType == ItemType.GreenCube || 
               itemType == ItemType.BlueCube || itemType == ItemType.YellowCube ||
               itemType == ItemType.PurpleCube;
    }
    
    private string GetItemSymbol(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.RandomCube: return "✕";
            case ItemType.HorizontalRocket: return "→";
            case ItemType.VerticalRocket: return "↑";
            case ItemType.Balloon: return "○";
            case ItemType.Duck: return "◊";
            default: return "";
        }
    }
    
    private GUIStyle GetCenteredStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;
        return style;
    }
} 