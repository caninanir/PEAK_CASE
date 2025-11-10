using UnityEngine;

public class DuckBehavior : ObstacleBehavior
{
    public override void Initialize(ObstacleItem item, int health)
    {
        base.Initialize(item, health);
    }

    public override bool CanTakeDamageFrom(DamageSource source)
    {
        return false;
    }

    public override bool CanFall()
    {
        return true;
    }

    public override Sprite GetSprite()
    {
        return obstacleItem != null ? obstacleItem.GetDuckSprite() : null;
    }
}

