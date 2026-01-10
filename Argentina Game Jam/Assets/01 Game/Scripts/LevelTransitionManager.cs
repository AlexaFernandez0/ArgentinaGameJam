using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelTransitionManager : MonoBehaviour
{
    public static LevelTransitionManager Instance { get; private set; }

    [Header("Levels (manual order)")]
    public List<LevelDefinition> levels = new ();

    [Header("UI")]
    public UIWinPanel levelFinishedPanel;

    [Header("Refs")]
    public GameManager gameManager;

    private int _currentLevelIndex = 0;
    private bool _isTransitioning = false;
    private int _heatAtLevelStart = 0;

    LevelDefinition Current => levels[_currentLevelIndex];

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

    private void Start()
    {
        if (gameManager == null) gameManager = GameManager.Instance;

        if (levels == null || levels.Count == 0)
        {
            Debug.LogError("[LevelTransitionManager] No levels assigned.");
            return;
        }

        // Ensure only current level is active at start
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] == null) continue;
            levels[i].gameObject.SetActive(i == _currentLevelIndex);
        }

        _currentLevelIndex = 0;
        SetOnlyThisLevelActive(_currentLevelIndex);

        if (gameManager == null) gameManager = GameManager.Instance;
        gameManager?.LoadLevel(levels[_currentLevelIndex]);

        Debug.Log("[LevelTransitionManager] Initialized. Current level index = " + _currentLevelIndex);
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= levels.Count || levels[index] == null)
        {
            Debug.LogError($"LoadLevel: invalid index {index}");
            return;
        }

        _currentLevelIndex = index;
        LevelDefinition level = levels[_currentLevelIndex];

        SetOnlyThisLevelActive(_currentLevelIndex);
        AlignLevelToGridOrigin(level);

        // 🔑 heat al entrar a este nivel:
        if (gameManager == null) gameManager = GameManager.Instance;
        _heatAtLevelStart = gameManager != null ? gameManager.heat : 0;

        // Cargar nivel manteniendo heat actual (por defecto)
        gameManager.LoadLevel(level, _heatAtLevelStart);
    }

    public void RetryLevel()
    {
        LoadLevel(_currentLevelIndex);
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

        Debug.Log($"[LevelTransition] Level aligned by delta {delta}");
    }

    public void RetryLevelFromPanel()
    {
        if (gameManager == null) gameManager = GameManager.Instance;

        LevelDefinition level = levels[_currentLevelIndex];
        SetOnlyThisLevelActive(_currentLevelIndex);
        AlignLevelToGridOrigin(level);

        // 👇 retry con heat del inicio del nivel
        gameManager.LoadLevel(level, _heatAtLevelStart);
        gameManager.SetBusy(false);
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

        // 👇 heat actual (al terminar el nivel) se mantiene
        int carryHeat = gameManager.heat;

        _currentLevelIndex = nextIndex;

        LevelDefinition nextLevel = levels[_currentLevelIndex];
        SetOnlyThisLevelActive(_currentLevelIndex);
        AlignLevelToGridOrigin(nextLevel);

        // 🔑 al entrar a nuevo nivel, su "heat inicial" será el carryHeat
        _heatAtLevelStart = carryHeat;

        gameManager.LoadLevel(nextLevel, carryHeat);
        gameManager.SetBusy(false);
    }

    public EnemyUnit[] GetEnemies()
    {
        return GetComponentsInChildren<EnemyUnit>(true);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LevelFinished += OnLevelFinished;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LevelFinished -= OnLevelFinished;
    }

    private void OnLevelFinished(string msg)
    {
        if (levelFinishedPanel != null)
        {
            levelFinishedPanel.Show(msg);
        }
        else
        {
            Debug.LogWarning("[LevelTransitionManager] levelFinishedPanel not assigned.");
        }
    }
}

