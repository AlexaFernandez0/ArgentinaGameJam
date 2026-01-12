using UnityEngine;

public class FootstepEmitter : MonoBehaviour
{
    [Tooltip("Tiempo mínimo entre pasos")]
    [SerializeField] private float cooldown = 0.15f;

    private float _lastTime;

    public void Step()
    {
        if (Time.time - _lastTime < cooldown) return;

        _lastTime = Time.time;

        if (AudioManager.Instance != null)
        {
            AudioClip footsStepClip = AudioManager.Instance.footstepClip;
            AudioManager.Instance.PlaySFXPitchVariability(footsStepClip);
        }
    }
}
