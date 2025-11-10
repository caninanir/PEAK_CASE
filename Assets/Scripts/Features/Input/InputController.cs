using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    private InputHandler inputHandler;
    private bool inputLocked = false;
    private Coroutine unlockCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        inputHandler = gameObject.AddComponent<InputHandler>();
        SubscribeToEvents();
    }

    private void Start()
    {
        RefreshCanvasReferences();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void HandleGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState == GameState.GameWon || evt.NewState == GameState.GameLost)
        {
            inputLocked = true;
        }
        else if (evt.NewState == GameState.MainMenu || evt.NewState == GameState.Finished)
        {
            if (unlockCoroutine != null) StopCoroutine(unlockCoroutine);
            inputLocked = true;
            unlockCoroutine = StartCoroutine(UnlockAfterDelay(1f));
        }
        else
        {
            if (unlockCoroutine != null) StopCoroutine(unlockCoroutine);
            inputLocked = false;
        }
    }

    private IEnumerator UnlockAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        inputLocked = false;
        unlockCoroutine = null;
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (!InputValidator.CanProcessInput(
            GameStateController.Instance.CurrentState,
            GameStateController.Instance.IsProcessingMove,
            inputLocked))
        {
            return;
        }
        
        if (graphicRaycaster == null || eventSystem == null)
        {
            RefreshCanvasReferences();
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 screenPosition = Input.mousePosition;
            ProcessTap(screenPosition);
        }
        
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                ProcessTap(touch.position);
            }
        }
    }

    private GraphicRaycaster graphicRaycaster;
    private EventSystem eventSystem;

    private void RefreshCanvasReferences()
    {
        GridController gridController = FindFirstObjectByType<GridController>();
        if (gridController != null)
        {
            inputHandler.RefreshCanvasReferences(gridController.GridContainer);
            graphicRaycaster = inputHandler.GetComponent<GraphicRaycaster>();
            eventSystem = EventSystem.current;
        }
    }

    private void ProcessTap(Vector2 screenPosition)
    {
        BaseItem tappedItem = inputHandler.GetTappedItem(screenPosition);
        
        if (tappedItem != null)
        {
            HandleItemTapped(tappedItem);
        }
    }

    private void HandleItemTapped(BaseItem item)
    {
        if (item is CubeItem cube)
        {
            Vector2Int gridPos = cube.GetGridPosition();
            
            if (gridPos.y >= 0)
            {
                EventBus.Publish(new CubeTappedEvent
                {
                    GridX = gridPos.x,
                    GridY = gridPos.y,
                    CubeType = cube.itemType
                });
            }
        }
        else if (item is RocketItem rocket)
        {
            Vector2Int gridPos = rocket.GetGridPosition();
            
            EventBus.Publish(new RocketTappedEvent
            {
                GridX = gridPos.x,
                GridY = gridPos.y,
                RocketType = rocket.itemType
            });
        }
        else
        {
            item.OnTapped();
        }
    }

    public void OnCanvasChanged()
    {
        RefreshCanvasReferences();
    }
}

