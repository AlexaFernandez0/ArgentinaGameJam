using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UILevelFinishedPanel : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text messageText;

    private void Awake()
    {
        Hide();
    }

    public void Show(string message)
    {
        if (titleText != null) titleText.text = "YOU WIN!";
        if (messageText != null) messageText.text = string.IsNullOrWhiteSpace(message)
            ? "You arrived safe."
            : message;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnRetryPressed()
    {
        Hide();
        LevelTransitionManager.Instance.RetryLevelFromPanel();
    }

    public void OnNextLevelPressed()
    {
        Hide();
        LevelTransitionManager.Instance.NextLevelFromPanel();
    }
}

