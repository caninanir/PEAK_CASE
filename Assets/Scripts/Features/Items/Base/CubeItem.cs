using UnityEngine;

public class CubeItem : BaseItem
{
    [Header("Cube Sprites")]
    [SerializeField] private Sprite redSprite;
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite blueSprite;
    [SerializeField] private Sprite yellowSprite;
    [SerializeField] private Sprite purpleSprite;

    public override void Initialize(ItemType type)
    {
        base.Initialize(type);
    }

    public override void OnTapped()
    {
        Vector2Int gridPos = GetGridPosition();
        
        if (gridPos.y < 0)
        {
            return;
        }
        
        EventBus.Publish(new CubeTappedEvent
        {
            GridX = gridPos.x,
            GridY = gridPos.y,
            CubeType = itemType
        });
    }

    public override Sprite GetSprite()
    {
        switch (itemType)
        {
            case ItemType.RedCube: return redSprite;
            case ItemType.GreenCube: return greenSprite;
            case ItemType.BlueCube: return blueSprite;
            case ItemType.YellowCube: return yellowSprite;
            case ItemType.PurpleCube: return purpleSprite;
            default: return redSprite;
        }
    }

    public ItemType GetCubeColor()
    {
        return itemType;
    }

    public bool IsMatchingColor(CubeItem other)
    {
        return other != null && itemType == other.itemType;
    }

    public override bool CanFall()
    {
        return true;
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
    }
} 