using UnityEngine;
using UnityEngine.UI;

public abstract class BaseItem : MonoBehaviour, IPoolable
{
    public ItemType itemType { get; protected set; }
    public GridCell currentCell { get; protected set; }
    
    [SerializeField] public Image itemImage;
    [SerializeField] protected int sortingOrder = 1;

    protected virtual void Awake()
    {
        EnsureImageComponent();
    }

    protected void EnsureImageComponent()
    {
        if (itemImage == null)
            itemImage = GetComponent<Image>();
        
        if (itemImage == null)
            itemImage = gameObject.AddComponent<Image>();
        
        itemImage.raycastTarget = true;
        
        if (itemImage.color.a <= 0f)
        {
            Color imageColor = itemImage.color;
            imageColor.a = 1f;
            itemImage.color = imageColor;
        }
    }

    public virtual void Initialize(ItemType type)
    {
        EnsureImageComponent();
        
        itemType = type;
        
        itemImage.sprite = GetSprite();
        itemImage.raycastTarget = true;
    }

    public virtual void SetCell(GridCell cell)
    {
        currentCell = cell;
        
        if (cell != null)
        {
            RectTransform itemRect = GetComponent<RectTransform>();
            RectTransform cellRect = cell.GetComponent<RectTransform>();
            itemRect.anchoredPosition = cellRect.anchoredPosition;
            Vector2 sizeMultiplier = GridController.Instance.GetScaleForItemType(itemType);
            itemRect.sizeDelta = new Vector2(
                cellRect.sizeDelta.x * sizeMultiplier.x,
                cellRect.sizeDelta.y * sizeMultiplier.y);
            itemRect.anchorMin = cellRect.anchorMin;
            itemRect.anchorMax = cellRect.anchorMax;

            GridController.Instance.UpdateItemSiblingOrder(this);
        }
    }

    public virtual void OnTapped()
    {
    }

    public virtual bool CanFall()
    {
        return true;
    }

    public virtual void OnDestroyed()
    {
        if (currentCell != null)
        {
            currentCell.RemoveItem();
        }
    }

    public virtual void OnReturnToPool()
    {
        if (currentCell != null)
        {
            currentCell.RemoveItem();
            currentCell = null;
        }
        
        itemImage.color = Color.white;
        
        transform.localScale = Vector3.one;
    }

    public virtual void OnSpawn()
    {
    }

    public virtual void OnDespawn()
    {
        OnReturnToPool();
    }

    public abstract Sprite GetSprite();

    public virtual bool MatchesWith(BaseItem other)
    {
        return other != null && itemType == other.itemType;
    }

    public virtual bool IsPhantom()
    {
        return false;
    }

    public Vector2Int GetGridPosition()
    {
        if (currentCell != null)
        {
            int visibleY = currentCell.y - GridController.Instance.BufferRows;
            return new Vector2Int(currentCell.x, Mathf.Max(0, visibleY));
        }
        
        return Vector2Int.zero;
    }

    public bool CanReceiveInput()
    {
        return itemImage.raycastTarget;
    }

    protected virtual void OnDestroy()
    {
        OnDestroyed();
    }
}
