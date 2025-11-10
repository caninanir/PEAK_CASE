using System.Collections;
using UnityEngine;

public class PopupController : MonoBehaviour
{
    public static PopupController Instance { get; private set; }
    
    [Header("Popup References")]
    [SerializeField] private LosePopup losePopup;
    [SerializeField] private Transform popupContainer;
    [SerializeField] private GameObject celebrationEffects;
    [SerializeField] private float victoryDuration = 4f;

    private Coroutine celebrationCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<LevelWonEvent>(HandleLevelWon);
        EventBus.Subscribe<LevelLostEvent>(HandleLevelLost);
        EventBus.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<LevelWonEvent>(HandleLevelWon);
        EventBus.Unsubscribe<LevelLostEvent>(HandleLevelLost);
        EventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void HandleLevelWon(LevelWonEvent evt)
    {
        HideAllPopups();
        celebrationEffects.SetActive(true);
        
        if (celebrationCoroutine != null)
        {
            StopCoroutine(celebrationCoroutine);
        }
        celebrationCoroutine = StartCoroutine(ReturnToMenuAfterDelay());
    }

    private IEnumerator ReturnToMenuAfterDelay()
    {
        yield return new WaitForSeconds(victoryDuration);
        GameStateController.Instance.ReturnToMainMenu();
    }

    private void HandleLevelLost(LevelLostEvent evt)
    {
        ShowLosePopup();
    }

    private void HandleGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState == GameState.MainMenu)
        {
            HideAllPopups();
            if (celebrationCoroutine != null)
            {
                StopCoroutine(celebrationCoroutine);
                celebrationCoroutine = null;
            }
        }
    }

    public void ShowLosePopup()
    {
        HideAllPopups();
        losePopup.Show();
    }

    public void HideAllPopups()
    {
        losePopup.Hide();
    }

    public bool IsAnyPopupVisible()
    {
        return losePopup.gameObject.activeInHierarchy;
    }
}

