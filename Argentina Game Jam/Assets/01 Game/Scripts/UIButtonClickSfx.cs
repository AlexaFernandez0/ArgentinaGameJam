using UnityEngine;
using UnityEngine.UI;

public class UIButtonClickSfx : MonoBehaviour
{
    [Header("Clip")]
    [SerializeField] private AudioClip clickSfx;

    [Header("Config")]
    [SerializeField] private bool includeInactive = true;

    private void OnEnable()
    {
        HookButtons();
    }

    public void HookButtons()
    {
        if (clickSfx == null)
        {
            Debug.LogWarning("[UIButtonClickSfx] No clickSfx assigned.");
            return;
        }

        var buttons = GetComponentsInChildren<Button>(includeInactive);

        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            // Evita duplicados (por si se llama varias veces)
            btn.onClick.RemoveListener(PlayClick);
            btn.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        if (AudioManager.Instance == null) return;

        // Usa tu método existente
        AudioManager.Instance.PlaySfx(clickSfx);
    }
}

