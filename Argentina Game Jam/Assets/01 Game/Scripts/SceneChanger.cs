using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneChanger : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Nombre exacto de la escena a cargar (debe estar en Build Settings)")]
    public string sceneName;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        
        // Conectar automáticamente el botón al método
        _button.onClick.AddListener(ChangeScene);
    }

    private void ChangeScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    // Método para cerrar el juego (útil para botón de salir)
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}