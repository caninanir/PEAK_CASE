using UnityEngine;

public class CameraScaler : MonoBehaviour
{
    public float targetAspect = 9f / 16f;

    public float targetOrthographicSize = 5f; 

    void Update()
    {
        float currentAspect = (float)Screen.width / (float)Screen.height;
        
        float aspectFactor = currentAspect / targetAspect;

        float adjustedSize = targetOrthographicSize * Mathf.Max(1f, aspectFactor);

        GetComponent<Camera>().orthographicSize = adjustedSize;
    }
}
