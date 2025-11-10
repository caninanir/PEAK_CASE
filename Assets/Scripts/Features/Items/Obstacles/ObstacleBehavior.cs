using UnityEngine;

public abstract class ObstacleBehavior : MonoBehaviour
{
    protected ObstacleItem obstacleItem;
    protected int currentHealth;
    protected int maxHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => currentHealth <= 0;

    public virtual void Initialize(ObstacleItem item, int health)
    {
        obstacleItem = item;
        maxHealth = health;
        currentHealth = health;
    }

    public abstract bool CanTakeDamageFrom(DamageSource source);
    public abstract bool CanFall();
    public abstract Sprite GetSprite();
    
    public virtual void TakeDamage(int damage = 1)
    {
        int previousHealth = currentHealth;
        currentHealth -= damage;
        
        UpdateVisuals(previousHealth);
        
        if (currentHealth <= 0)
        {
            DestroyObstacle();
        }
    }

    protected virtual void UpdateVisuals(int previousHealth)
    {
        if (obstacleItem.itemImage != null)
        {
            obstacleItem.itemImage.sprite = GetSprite();
        }
    }

    protected virtual void DestroyObstacle()
    {
        ItemType itemType = obstacleItem.itemType;
        Vector2Int gridPos = obstacleItem.GetGridPosition();
        
        if (itemType == ItemType.Balloon && ParticleEffectManager.Instance != null)
        {
            RectTransform rectTransform = obstacleItem.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                ParticleEffectManager.Instance.SpawnBalloonPop(rectTransform);
            }
        }
        
        AudioManager.Instance.PlayObstacleSound(itemType, true);
        
        EventBus.Publish(new ObstacleDestroyedEvent
        {
            ObstacleType = itemType,
            GridX = gridPos.x,
            GridY = gridPos.y
        });
        
        obstacleItem.OnDestroyed();
        Destroy(obstacleItem.gameObject);
    }
}




