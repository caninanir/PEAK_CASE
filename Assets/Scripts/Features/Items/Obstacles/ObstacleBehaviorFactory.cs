using UnityEngine;

public static class ObstacleBehaviorFactory
{
    public static ObstacleBehavior CreateBehavior(ItemType obstacleType, GameObject obstacleObject)
    {
        ObstacleBehavior behavior = null;
        ObstacleItem obstacleItem = obstacleObject.GetComponent<ObstacleItem>();
        
        switch (obstacleType)
        {
            case ItemType.Balloon:
                behavior = obstacleObject.AddComponent<BalloonBehavior>();
                behavior.Initialize(obstacleItem, 1);
                break;
                
            case ItemType.Duck:
                behavior = obstacleObject.AddComponent<DuckBehavior>();
                behavior.Initialize(obstacleItem, 1);
                break;
        }
        
        return behavior;
    }

    public static int GetMaxHealth(ItemType obstacleType)
    {
        switch (obstacleType)
        {
            case ItemType.Balloon:
            case ItemType.Duck:
                return 1;
            default:
                return 1;
        }
    }
}




