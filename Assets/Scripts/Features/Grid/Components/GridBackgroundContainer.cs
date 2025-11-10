using UnityEngine;
using UnityEngine.UI;

public class GridBackgroundContainer : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponentInChildren<GridBackground>() == null)
        {
            GameObject bgObject = new GameObject("GridBackground");
            bgObject.transform.SetParent(transform);
            bgObject.AddComponent<RectTransform>();
            bgObject.AddComponent<Image>();
            bgObject.AddComponent<GridBackground>();
        }
    }
} 