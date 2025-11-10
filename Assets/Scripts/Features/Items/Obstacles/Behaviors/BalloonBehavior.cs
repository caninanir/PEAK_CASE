using UnityEngine;

public class BalloonBehavior : ObstacleBehavior
{
    public override void Initialize(ObstacleItem item, int health)
    {
        base.Initialize(item, health);
    }

    public override bool CanTakeDamageFrom(DamageSource source)
    {
        return source == DamageSource.AdjacentBlast || source == DamageSource.Rocket;
    }

    public override bool CanFall()
    {
        return true;
    }

    public override Sprite GetSprite()
    {
        return obstacleItem != null ? obstacleItem.GetBalloonSprite() : null;
    }
}

