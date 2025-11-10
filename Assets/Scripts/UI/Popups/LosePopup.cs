using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LosePopup : BasePopup
{
    [Header("Lose Popup Elements")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI failureText;

    protected override void Awake()
    {
        base.Awake();
        
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    protected override void OnShow()
    {
        base.OnShow();
        
        int currentLevel = GameStateController.Instance.CurrentLevel;
        failureText.text = $"Level {currentLevel} Failed!";
    }

    private void OnRetryClicked()
    {
        AudioManager.Instance.PlayButtonClickSound();
        Hide();
        GameStateController.Instance.RestartLevel();
    }

    private void OnMainMenuClicked()
    {
        AudioManager.Instance.PlayButtonClickSound();
        Hide();
        GameStateController.Instance.ReturnToMainMenu();
    }
} 