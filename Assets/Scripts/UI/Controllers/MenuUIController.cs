using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private LevelButton levelButton;
    [SerializeField] private Image backgroundImage;
    
    [Header("Background")]
    [SerializeField] private Sprite menuBackgroundSprite;

    private void Start()
    {
        SetupBackground();
        UpdateUI();
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetDebugLevel(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SetDebugLevel(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SetDebugLevel(3);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SetDebugLevel(4);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) SetDebugLevel(5);
            else if (Input.GetKeyDown(KeyCode.Alpha6)) SetDebugLevel(6);
            else if (Input.GetKeyDown(KeyCode.Alpha7)) SetDebugLevel(7);
            else if (Input.GetKeyDown(KeyCode.Alpha8)) SetDebugLevel(8);
            else if (Input.GetKeyDown(KeyCode.Alpha9)) SetDebugLevel(9);
            else if (Input.GetKeyDown(KeyCode.Alpha0)) SetDebugLevel(10);
            else if (Input.GetKeyDown(KeyCode.F)) SetDebugFinished();
        }
#endif
    }

    private void SetDebugLevel(int levelNumber)
    {
        SaveManager.Instance.ResetProgress();
        SaveManager.Instance.SetCurrentLevel(levelNumber);
        UpdateUI();
    }
    
    private void SetDebugFinished()
    {
        if (LevelManager.Instance != null)
        {
            List<int> allLevelNumbers = LevelManager.Instance.GetAllLevelNumbers();
            foreach (int levelNumber in allLevelNumbers)
            {
                SaveManager.Instance.MarkLevelCompleted(levelNumber);
            }
        }
        else
        {
            for (int i = 1; i <= 10; i++)
            {
                SaveManager.Instance.MarkLevelCompleted(i);
            }
        }
        
        UpdateUI();
    }

    private void SetupBackground()
    {
        if (backgroundImage != null && menuBackgroundSprite != null)
        {
            backgroundImage.sprite = menuBackgroundSprite;
            backgroundImage.preserveAspect = false;
        }
    }

    public void UpdateUI()
    {
        if (levelButton != null)
        {
            levelButton.UpdateButtonText();
        }
    }
}

