using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Peak Games/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Grid Settings")]
    public float cubeSize = 1f;
    public float gridSpacing = 0.1f;
    
    [Header("Animation Settings")]
    public float fallSpeed = 5f;
    public float explosionDelay = 0.2f;
    public float rocketCreationDuration = 0.5f;
    public float cubeDestroyDuration = 0.3f;
    
    [Header("Game Settings")]
    public int maxLevels = 10;
    public float inputDelay = 0.1f;
    
    [Header("Animation Curves")]
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve explosionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
} 