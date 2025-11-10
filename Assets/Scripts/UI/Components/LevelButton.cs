using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LevelButton : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image buttonImage;
    
    [Header("Button States")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color finishedColor = Color.white;

    private void Awake()
    {
        EnsureComponents();
    }

    private void Start()
    {
        EnsureComponents();
        button.onClick.AddListener(OnButtonClicked);
        StartCoroutine(DelayedUpdateButtonText());
    }

    private void OnEnable()
    {
        EnsureComponents();
        StartCoroutine(DelayedUpdateButtonText());
    }

    private IEnumerator DelayedUpdateButtonText()
    {
        yield return null;
        
        EnsureComponents();
        UpdateButtonText();
    }

    private void EnsureComponents()
    {
        button = GetComponent<Button>();
        
        levelText = GetComponentInChildren<TextMeshProUGUI>(true);
        
        buttonImage = GetComponent<Image>();
    }

    public void UpdateButtonText()
    {
        EnsureComponents();
        
        if (SaveManager.Instance == null)
            return;
        
        int currentLevel = SaveManager.Instance.GetCurrentLevel();
        bool hasMoreLevels = LevelManager.Instance.IsValidLevel(currentLevel);
        
        if (!hasMoreLevels)
        {
            levelText.text = "Finished";
            SetButtonColor(finishedColor);
            SetInteractable(false);
        }
        else
        {
            levelText.text = $"Level {currentLevel}";
            SetButtonColor(normalColor);
            SetInteractable(true);
        }
    }

    private void SetButtonColor(Color color)
    {
        buttonImage.color = color;
        
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.pressedColor = color;
        colors.selectedColor = color;
        colors.disabledColor = color;
        button.colors = colors;
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }

    private void OnButtonClicked()
    {
        AudioManager.Instance.PlayButtonClickSound();
        
        int currentLevel = SaveManager.Instance.GetCurrentLevel();
        
        SceneTransitionManager.Instance.LoadLevelScene(currentLevel);
    }
} 