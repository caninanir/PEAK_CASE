using UnityEngine;

public class PhantomItem : BaseItem
{
    public override void Initialize(ItemType type)
    {
        EnsureImageComponent();
        
        itemType = ItemType.Empty;
        
        itemImage.enabled = false;
    }

    public override Sprite GetSprite()
    {
        return null;
    }

    public override bool CanFall()
    {
        return false;
    }

    public override bool IsPhantom()
    {
        return true;
    }
} 