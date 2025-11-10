using UnityEngine;

public class ObstacleItem : BaseItem
{
    [Header("Obstacle Sprites")]
    [SerializeField] private Sprite duckSprite;
    [SerializeField] private Sprite balloonSprite;

    private ObstacleBehavior behavior;

    public ObstacleBehavior Behavior => behavior;
    public int CurrentHealth => behavior != null ? behavior.CurrentHealth : 0;
    public int MaxHealth => behavior != null ? behavior.MaxHealth : 0;
    public bool IsDestroyed => behavior != null && behavior.IsDestroyed;

    public Sprite GetDuckSprite() => duckSprite;
    public Sprite GetBalloonSprite() => balloonSprite;

    public override void Initialize(ItemType type)
    {
        base.Initialize(type);
        
        behavior = ObstacleBehaviorFactory.CreateBehavior(type, gameObject);
        
        if (itemImage != null)
        {
            itemImage.sprite = GetSprite();
        }
    }

    public override void OnTapped()
    {
    }

    public override Sprite GetSprite()
    {
        return behavior != null ? behavior.GetSprite() : null;
    }

    public override bool CanFall()
    {
        return behavior != null && behavior.CanFall();
    }

    public void TakeDamage(int damage = 1)
    {
        behavior?.TakeDamage(damage);
    }

    public bool CanTakeDamageFrom(DamageSource source)
    {
        return behavior != null && behavior.CanTakeDamageFrom(source);
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
        
        if (behavior != null)
        {
            Destroy(behavior);
            behavior = null;
        }
    }
}




