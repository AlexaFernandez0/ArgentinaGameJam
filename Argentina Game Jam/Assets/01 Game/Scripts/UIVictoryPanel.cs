using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIVictoryPanel : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text messageText;

    private void Awake() => Hide();

    public void Show(string message)
    {
        if (titleText) titleText.text = "VICTORY!";
        if (messageText) messageText.text = string.IsNullOrWhiteSpace(message) ? "You won!" : message;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    public void OnMainMenuPressed()
    {
        // SceneManager.LoadScene("MainMenu");
    }

    public void OnRetryPressed()
    {
        Hide();

        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.RetryLevelFromPanel();
        }
        else
        {
            Debug.LogWarning("LevelTransitionManager.Instance is null. Fallback to GameManager retry.");
            GameManager.Instance?.RetryLevel();
        }
    }
}

