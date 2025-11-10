using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class InputHandler : MonoBehaviour
{
    private GraphicRaycaster graphicRaycaster;
    private EventSystem eventSystem;
    private Canvas gameCanvas;

    public void Initialize(Canvas canvas, GraphicRaycaster raycaster, EventSystem eventSys)
    {
        gameCanvas = canvas;
        graphicRaycaster = raycaster;
        eventSystem = eventSys;
    }

    public void RefreshCanvasReferences(Transform gridContainer)
    {
        gameCanvas = null;
        graphicRaycaster = null;
        
        if (gridContainer != null)
        {
            Canvas containerCanvas = gridContainer.GetComponentInParent<Canvas>();
            
            if (containerCanvas != null)
            {
                GraphicRaycaster raycaster = containerCanvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null && raycaster.enabled)
                {
                    gameCanvas = containerCanvas;
                    graphicRaycaster = raycaster;
                    
                    eventSystem = EventSystem.current;
                    if (eventSystem == null)
                    {
                        eventSystem = FindFirstObjectByType<EventSystem>();
                    }
                    return;
                }
            }
        }
        
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name.Contains("Fade") || canvas.name.Contains("BlackScreen") || 
                canvas.name.Contains("Transition") || canvas.name.Contains("Popup"))
            {
                continue;
            }
            
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null && raycaster.enabled && canvas.gameObject.activeInHierarchy)
            {
                if (gameCanvas == null || canvas.sortingOrder < gameCanvas.sortingOrder)
                {
                    gameCanvas = canvas;
                    graphicRaycaster = raycaster;
                }
            }
        }
        
        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }
    }

    public BaseItem GetTappedItem(Vector2 screenPosition)
    {
        if (graphicRaycaster == null || eventSystem == null)
        {
            return null;
        }
        
        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = screenPosition;
        
        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, results);
        
        foreach (RaycastResult result in results)
        {
            BaseItem item = result.gameObject.GetComponentInParent<BaseItem>();
            if (item != null)
            {
                return item;
            }
        }
        
        return null;
    }
}

