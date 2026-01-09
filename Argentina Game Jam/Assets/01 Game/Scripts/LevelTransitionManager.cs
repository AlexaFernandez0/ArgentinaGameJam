using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelTransitionManager : MonoBehaviour
{
    public static LevelTransitionManager Instance { get; private set; }

    [Header("Levels (manual order)")]
    public List<LevelDefinition> levels = new ();

    [Header("Transition")]
    public float slideDuration = 0.6f;
    public float slideOffset = 40f; // distance between chunks in world units
    public float fadeTime = 0.2f;
    public Transform gridOrigin;

    [Header("UI Fade (CanvasGroup on a full-screen panel)")]
    public CanvasGroup fadeGroup;

    [Header("Refs")]
    public GameManager gameManager;

    private int _currentLevelIndex = 0;
    private bool _isTransitioning = false;

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

        // Ensure fade is invisible
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
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

        AlignLevelToGridOrigin(level);                  // ✅ 1) primero mover root

        BoardManager.Instance.BuildFromLevelRoot(level.transform);  // ✅ 2) luego rebuild board
        GameManager.Instance.SetupFromLevel(level);                // ✅ 3) luego gameplay
    }


    public void TransitionToNextLevel()
    {
        if (_isTransitioning) return;

        int nextIndex = _currentLevelIndex + 1;
        if (nextIndex >= levels.Count)
        {
            Debug.Log("[LevelTransitionManager] No next level available. You reached the end.");
            // You can call GameManager.Win here if you want:
            // gameManager?.Win("All levels completed."); (Win is private now)
            return;
        }

        StartCoroutine(TransitionRoutine(_currentLevelIndex, nextIndex));
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



    private IEnumerator TransitionRoutine(int fromIndex, int toIndex)
    {
        _isTransitioning = true;

        LevelDefinition fromLevel = levels[fromIndex];
        LevelDefinition toLevel = levels[toIndex];

        if (fromLevel == null || toLevel == null)
        {
            Debug.LogError("[LevelTransitionManager] Missing level references.");
            _isTransitioning = false;
            yield break;
        }

        Debug.Log($"[LevelTransitionManager] Transition start: {fromIndex} -> {toIndex}");

        // 1) Lock gameplay
        if (gameManager != null)
            gameManager.SetBusy(true);

        // 2) Fade in
        yield return Fade(1f);

        // 3) Compute the TRUE center we want on screen (gridOrigin)
        Vector3 center = fromLevel.transform.position; // fallback

        if (BoardManager.Instance != null && BoardManager.Instance.gridOrigin != null)
            center = BoardManager.Instance.gridOrigin.position;
        else if (gridOrigin != null)
            center = gridOrigin.position;

        Transform fromRoot = fromLevel.transform;
        Transform toRoot = toLevel.transform;

        // 4) Activate next level and place it off-screen relative to the REAL center
        toLevel.gameObject.SetActive(true);

        // Keep previous root where it is (in case it wasn't exactly on center)
        Vector3 fromStart = fromRoot.position;

        Vector3 toStart = center + new Vector3(slideOffset, 0f, 0f);
        toRoot.position = toStart;

        // Slide: old moves left from its current position, new goes to center
        Vector3 fromEnd = fromStart + new Vector3(-slideOffset, 0f, 0f);
        Vector3 toEnd = center;

        float t = 0f;
        float dur = Mathf.Max(0.01f, slideDuration);

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);

            fromRoot.position = Vector3.Lerp(fromStart, fromEnd, k);
            toRoot.position = Vector3.Lerp(toStart, toEnd, k);

            yield return null;
        }

        fromRoot.position = fromEnd;
        toRoot.position = toEnd;

        // 5) NOW that the new level is at center, align precisely using anchor->gridOrigin
        // (This prevents "slight offsets" due to anchor choice)
        AlignLevelToGridOrigin(toLevel);

        // 6) Switch gameplay to the new level (board scope + player/enemies)
        // IMPORTANT: LoadLevel / SetupFromLevel should assume the level root is already positioned
        if (gameManager != null)
            gameManager.LoadLevel(toLevel);

        // 7) Disable previous level
        fromLevel.gameObject.SetActive(false);

        // Optional: restore previous chunk root position so it doesn't drift
        fromRoot.position = fromStart;

        // 8) Fade out
        yield return Fade(0f);

        // 9) Unlock gameplay
        if (gameManager != null)
            gameManager.SetBusy(false);

        _currentLevelIndex = toIndex;
        _isTransitioning = false;

        Debug.Log($"[LevelTransitionManager] Transition completed. Current level index = {_currentLevelIndex}");
    }


    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeGroup == null) yield break;

        fadeGroup.blocksRaycasts = true;

        float startAlpha = fadeGroup.alpha;
        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeTime);

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, k);
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
        fadeGroup.blocksRaycasts = targetAlpha > 0.01f;
    }

    public EnemyUnit[] GetEnemies()
    {
        return GetComponentsInChildren<EnemyUnit>(true);
    }

    private void OnEnable()
    {
        GameManager.Instance.GameWon += OnGameWon;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameWon -= OnGameWon;
    }

    private void OnGameWon(string msg)
    {
        LoadLevel(_currentLevelIndex + 1);
    }
}

