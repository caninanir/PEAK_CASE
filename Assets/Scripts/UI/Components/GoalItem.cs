using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoalItem : MonoBehaviour, IPoolable
{
    [Header("Goal Item Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image checkmarkImage;
    
    [Header("Sprites")]
    [SerializeField] private Sprite checkmarkSprite;
    [SerializeField] private Sprite balloonSprite;
    [SerializeField] private Sprite duckSprite;
    [SerializeField] private Sprite redCubeSprite;
    [SerializeField] private Sprite greenCubeSprite;
    [SerializeField] private Sprite blueCubeSprite;
    [SerializeField] private Sprite yellowCubeSprite;
    [SerializeField] private Sprite purpleCubeSprite;
    
    private ItemType itemType;
    private int targetCount;

    public void Initialize(ItemType type, int count)
    {
        itemType = type;
        targetCount = count;
        
        EnsureComponents();
        SetupIcon();
        UpdateCount(count);
    }

    private void EnsureComponents()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
        
        if (countText == null)
        {
            countText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (countText == null)
            {
                GameObject countTextObj = new GameObject("CountText");
                countTextObj.transform.SetParent(transform, false);
                countText = countTextObj.AddComponent<TextMeshProUGUI>();
                RectTransform rectTransform = countText.rectTransform;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
        }
        
        if (checkmarkImage == null)
        {
            Transform checkmarkTransform = transform.Find("CheckmarkImage");
            if (checkmarkTransform != null)
            {
                checkmarkImage = checkmarkTransform.GetComponent<Image>();
            }
            
            if (checkmarkImage == null)
            {
                SetupCheckmarkImage();
            }
        }
    }

    private void SetupIcon()
    {
        if (iconImage == null) return;
        
        Sprite obstacleSprite = GetObstacleSprite(itemType);
        if (obstacleSprite != null)
        {
            iconImage.sprite = obstacleSprite;
            iconImage.color = Color.white;
        }
    }

    private Sprite GetObstacleSprite(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Balloon:
                return balloonSprite;
            case ItemType.Duck:
                return duckSprite;
            case ItemType.RedCube:
                return redCubeSprite;
            case ItemType.GreenCube:
                return greenCubeSprite;
            case ItemType.BlueCube:
                return blueCubeSprite;
            case ItemType.YellowCube:
                return yellowCubeSprite;
            case ItemType.PurpleCube:
                return purpleCubeSprite;
            default:
                return null;
        }
    }

    public void UpdateCount(int count)
    {
        EnsureComponents();
        
        if (count <= 0)
        {
            if (countText != null)
            {
                countText.text = "";
                countText.gameObject.SetActive(false);
            }
            
            if (checkmarkImage != null)
            {
                checkmarkImage.sprite = checkmarkSprite;
                checkmarkImage.color = Color.white;
                checkmarkImage.gameObject.SetActive(true);
            }
        }
        else
        {
            if (countText != null)
            {
                countText.text = count.ToString();
                countText.color = Color.white;
                countText.gameObject.SetActive(true);
            }
            
            if (checkmarkImage != null)
            {
                checkmarkImage.gameObject.SetActive(false);
            }
        }
    }

    public void SetupCheckmarkImage()
    {
        if (checkmarkImage == null)
        {
            GameObject checkmarkGO = new GameObject("CheckmarkImage");
            checkmarkGO.transform.SetParent(transform, false);
            
            checkmarkImage = checkmarkGO.AddComponent<Image>();
            
            RectTransform rectTransform = checkmarkImage.rectTransform;
            rectTransform.anchorMin = new Vector2(0.7f, 0);
            rectTransform.anchorMax = new Vector2(1.17f, 0.43f);
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            checkmarkImage.gameObject.SetActive(true);
        }
    }

    public void OnSpawn()
    {
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        gameObject.SetActive(false);
        if (countText != null)
        {
            countText.text = "";
            countText.gameObject.SetActive(false);
        }
        if (checkmarkImage != null)
        {
            checkmarkImage.gameObject.SetActive(false);
        }
    }
}

