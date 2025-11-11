using UnityEngine;

public class CameraScaler : MonoBehaviour
{
    [SerializeField] private float targetOrthographicSize = 5f;

    private void Start()
    {
        if (LetterboxManager.Instance != null)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = targetOrthographicSize;
            }
        }
    }
}
