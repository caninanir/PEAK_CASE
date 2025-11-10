using UnityEngine;

public class EffectController : MonoBehaviour
{
    public static EffectController Instance { get; private set; }

    private TransitionController transitionController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
            InitializeControllers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void MarkParentAsDontDestroy()
    {
        if (transform.parent != null)
        {
            DontDestroyOnLoad(transform.parent.gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void InitializeControllers()
    {
        transitionController = GetComponentInChildren<TransitionController>() ?? FindFirstObjectByType<TransitionController>();
        if (transitionController == null)
        {
            GameObject transitionObj = new GameObject("TransitionController");
            transitionObj.transform.SetParent(transform);
            transitionObj.transform.localPosition = Vector3.zero;
            transitionObj.transform.localRotation = Quaternion.identity;
            transitionObj.transform.localScale = Vector3.one;
            transitionController = transitionObj.AddComponent<TransitionController>();
        }
    }

}

