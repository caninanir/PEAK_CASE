using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RocketItem : BaseItem
{
    [Header("Rocket Visuals")]
    [SerializeField] private Image primaryImage;
    [SerializeField] private Image secondaryImage;
    
    private readonly Dictionary<RectTransform, Vector3> visualBaseScales = new Dictionary<RectTransform, Vector3>();

    protected override void Awake()
    {
        itemImage = primaryImage;
        
        base.Awake();
    }

    public override void Initialize(ItemType type)
    {
        itemImage = primaryImage;
        
        base.Initialize(type);
        
        ApplyOrientation();
        ResetVisuals();
    }

    public override void OnTapped()
    {
        if (GameStateController.Instance.IsProcessingMove)
        {
            return;
        }
        
        Vector2Int gridPos = GetGridPosition();
        
        EventBus.Publish(new RocketTappedEvent
        {
            GridX = gridPos.x,
            GridY = gridPos.y,
            RocketType = itemType
        });
    }

    public override Sprite GetSprite()
    {
        return itemImage.sprite;
    }

    public override bool CanFall()
    {
        return true;
    }

    public bool IsHorizontal()
    {
        return itemType == ItemType.HorizontalRocket;
    }

    public bool IsVertical()
    {
        return itemType == ItemType.VerticalRocket;
    }

    public Vector2Int GetExplosionDirection()
    {
        if (IsHorizontal())
            return Vector2Int.right;
        else
            return Vector2Int.up;
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
        
        transform.localRotation = Quaternion.identity;
        ResetVisuals();
    }

    private void ApplyOrientation()
    {
        float rotationZ = itemType == ItemType.VerticalRocket ? 90f : 0f;
        
        transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
    }

    public void SetVisualScale(float scale)
    {
        foreach (RectTransform rect in EnumerateVisualRects())
        {
            if (rect != null)
            {
                if (!visualBaseScales.ContainsKey(rect))
                {
                    visualBaseScales[rect] = rect.localScale;
                }
                
                rect.localScale = visualBaseScales[rect] * scale;
            }
        }
    }

    public void SetVisualActive(bool isActive)
    {
        foreach (Image image in EnumerateVisualImages())
        {
            if (image != null)
            {
                image.enabled = isActive;
            }
        }
    }

    public void ResetVisuals()
    {
        SetVisualActive(true);
        foreach (RectTransform rect in EnumerateVisualRects())
        {
            if (rect != null)
            {
                if (!visualBaseScales.ContainsKey(rect))
                {
                    visualBaseScales[rect] = rect.localScale;
                }
                
                rect.localScale = visualBaseScales[rect];
            }
        }
    }

    public Image GetPrimaryImage()
    {
        return primaryImage;
    }

    public Image GetSecondaryImage()
    {
        return secondaryImage;
    }

    private IEnumerable<Image> EnumerateVisualImages()
    {
        if (primaryImage != null) yield return primaryImage;
        if (secondaryImage != null) yield return secondaryImage;
    }

    private IEnumerable<RectTransform> EnumerateVisualRects()
    {
        foreach (Image image in EnumerateVisualImages())
        {
            if (image != null)
            {
                yield return image.rectTransform;
            }
        }
    }
} 