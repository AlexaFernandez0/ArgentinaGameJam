using UnityEngine;

public class AutoDisableOnAnimationEnd : MonoBehaviour
{
    public void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}
