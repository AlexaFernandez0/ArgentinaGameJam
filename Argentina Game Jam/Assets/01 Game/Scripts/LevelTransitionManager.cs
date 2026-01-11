using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelTransitionManager : MonoBehaviour
{
    public static LevelTransitionManager Instance { get; private set; }

    [Header("Levels (manual order)")]
    public List<LevelDefinition> levels = new();

    [Header("UI")]
    public UILevelFinishedPanel levelFinishedPanel; // tu panel con Retry/Next

    [Header("Refs")]
    public GameManager gameManager;

    private int _currentLevelIndex = 0;

    // heat con el que se ENTRÓ al nivel actual (para Retry)
    private int _heatAtLevelStart = 0;

    public event Action<string> LevelFinished; // no último nivel
    public event Action<string> GameWon;       // último nivel

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate LevelTransitionManager detected. Destroying the new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Ojo: si GameManager aún no existe en OnEnable, también lo hacemos en Start.
        if (GameManager.Instance != null)
            GameManager.Instance.GoalReached += OnGoalReached;

        // UI events (opcional si tu panel llama directo a methods públicos)
        LevelFinished += OnLevelFinished;
        GameWon += OnGameWon;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoalReached -= OnGoalReached;

        LevelFinished -= OnLevelFinished;
        GameWon -= OnGameWon;
    }

    private void Start()
    {
        if (gameManager == null) gameManager = GameManager.Instance;

        if (levels == null || levels.Count == 0)
        {
            Debug.LogError("[LevelTransitionManager] No levels assigned.");
            return;
        }

        // Por si OnEnable corrió antes de que existiera el GM
        if (gameManager != null)
            gameManager.GoalReached -= OnGoalReached;
        if (gameManager != null)
            gameManager.GoalReached += OnGoalReached;

        // Activar solo el primero
        _currentLevelIndex = Mathf.Clamp(_currentLevelIndex, 0, levels.Count - 1);
        SetOnlyThisLevelActive(_currentLevelIndex);

        // Guardar heat inicial del nivel (al inicio del juego normalmente 0)
        _heatAtLevelStart = gameManager != null ? gameManager.heat : 0;

        // Cargar nivel 0 con ese heat inicial
        LoadLevelInternal(_currentLevelIndex, _heatAtLevelStart, recordStartHeat: true);

        Debug.Log("[LevelTransitionManager] Initialized. Current level index = " + _currentLevelIndex);
    }

    // ---------------- PUBLIC API (buttons) ----------------

    public void RetryLevelFromPanel()
    {
        if (gameManager == null) gameManager = GameManager.Instance;

        // Retry = mismo nivel con heat del inicio del nivel
        LoadLevelInternal(_currentLevelIndex, _heatAtLevelStart, recordStartHeat: false);

        // Desbloquear gameplay
        gameManager?.SetBusy(false);
    }

    public void NextLevelFromPanel()
    {
        if (gameManager == null) gameManager = GameManager.Instance;

        int nextIndex = _currentLevelIndex + 1;
        if (nextIndex >= levels.Count)
        {
            Debug.Log("[LevelTransitionManager] No next level. End of game.");
            return;
        }

        // carry = heat ACTUAL al terminar nivel
        int carryHeat = gameManager != null ? gameManager.heat : 0;

        _currentLevelIndex = nextIndex;

        // Entrar al siguiente nivel con carryHeat y guardar eso como heat de inicio del nuevo nivel
        LoadLevelInternal(_currentLevelIndex, carryHeat, recordStartHeat: true);

        // Desbloquear gameplay
        gameManager?.SetBusy(false);
    }

    // Si quieres cargar por índice desde debug/otros lados
    public void LoadLevel(int index)
    {
        if (gameManager == null) gameManager = GameManager.Instance;

        // Si lo llamas manualmente, lo normal es mantener heat actual
        int carryHeat = gameManager != null ? gameManager.heat : 0;
        _currentLevelIndex = index;

        LoadLevelInternal(_currentLevelIndex, carryHeat, recordStartHeat: true);
    }

    // ---------------- CORE ----------------

    private void LoadLevelInternal(int index, int heatToStart, bool recordStartHeat)
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogError("[LevelTransitionManager] No levels assigned.");
            return;
        }

        if (index < 0 || index >= levels.Count || levels[index] == null)
        {
            Debug.LogError($"[LevelTransitionManager] LoadLevelInternal: invalid index {index}");
            return;
        }

        var level = levels[index];

        SetOnlyThisLevelActive(index);
        AlignLevelToGridOrigin(level);

        if (recordStartHeat)
            _heatAtLevelStart = heatToStart;

        // Cargar el nivel con el heat que toca
        if (gameManager == null) gameManager = GameManager.Instance;
        gameManager?.LoadLevel(level, heatToStart);

        Debug.Log($"[LevelTransitionManager] Loaded level {index} with heat {heatToStart} (heatAtLevelStart={_heatAtLevelStart})");
    }

    private void SetOnlyThisLevelActive(int activeIndex)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] == null) continue;
            levels[i].gameObject.SetActive(i == activeIndex);
        }
    }

    private void AlignLevelToGridOrigin(LevelDefinition level)
    {
        if (level == null) return;
        if (BoardManager.Instance == null || BoardManager.Instance.gridOrigin == null) return;

        Transform levelRoot = level.transform;
        Transform anchor = level.anchor != null ? level.anchor : levelRoot;

        Vector3 target = BoardManager.Instance.gridOrigin.position;
        Vector3 delta = target - anchor.position;

        levelRoot.position += delta;
    }

    // ---------------- EVENTS ----------------

    private void OnGoalReached(string msg)
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        
        gameManager?.SetBusy(true);

        bool isLast = (_currentLevelIndex >= levels.Count - 1);

        if (isLast)
            GameWon?.Invoke(msg);
        else
            LevelFinished?.Invoke(msg);
    }

    private void OnLevelFinished(string msg)
    {
        if (levelFinishedPanel != null)
            levelFinishedPanel.Show(msg);
        else
            Debug.LogWarning("[LevelTransitionManager] levelFinishedPanel not assigned.");
    }

    private void OnGameWon(string msg)
    {
        // Aquí muestra el panel final de victoria total (puede ser otro panel distinto)
        // Por ahora, reutilizo el mismo si quieres:
        if (levelFinishedPanel != null)
            levelFinishedPanel.Show("GAME WON!\n" + msg);

        Debug.Log("[LevelTransitionManager] GAME WON: " + msg);
    }
}


