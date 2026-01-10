using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyActions))]
public class EnemyUnit : MonoBehaviour
{
    [Header("Stats")]
    public int health = 2;

    [Header("Turn Frequency")]
    [Tooltip("Cada cuántos turnos este enemigo toma acción (1 = cada turno, 2 = cada 2 turnos, etc.)")]
    public int StepsPerTurn = 1;

    [Header("Attack")]
    public int attackHeatDamage = 5;
    public GameObject attackEffectPrefab;
    public float attackEffectDuration = 5f;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 720f;

    [Header("Runtime")]
    public Tile currentTile;
    public bool IsDead => health <= 0;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private GameObject _visualMesh;
    private EnemyActions _actions;
    private EnemyAnimationController _animController;

    private bool _isExecutingTurn;

    private void Awake()
    {
        _actions = GetComponent<EnemyActions>();
        _animController = GetComponent<EnemyAnimationController>();

        if (_animController == null)
        {
            DebugLog("WARNING: No EnemyAnimationController found.");
        }

        if (transform.childCount > 0)
        {
            _visualMesh = transform.GetChild(0).gameObject;
        }
        else
        {
            Debug.LogWarning($"EnemyUnit '{name}' has no children. Visual mesh should be a child GameObject.");
        }
    }

    private void Start()
    {
        if (currentTile == null)
            DebugLog("WARNING: currentTile not set on Start.");
        else
            SnapToTile(currentTile);
    }

    public void SnapToTile(Tile tile)
    {
        currentTile = tile;
        if (tile != null)
            transform.position = tile.transform.position;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        DebugLog($"Enemy took damage: {amount}. HP: {health}");

        if (health <= 0)
        {
            DebugLog("Enemy defeated.");
            if (GameManager.Instance != null)
                GameManager.Instance.RemoveEnemy(this);

            if (_visualMesh != null) _visualMesh.SetActive(false);
            else gameObject.SetActive(false);
        }
    }

    public void ResetEnemy(int initialHealth)
    {
        health = initialHealth;
        _isExecutingTurn = false;

        if (_visualMesh != null) _visualMesh.SetActive(true);
        else gameObject.SetActive(true);

        if (_animController != null)
        {
            _animController.ResetToIdle();
        }

        DebugLog($"Enemy '{name}' reset with {health} HP. Turn frequency: {StepsPerTurn}");
    }

    public IEnumerator TakeTurnCoroutine()
    {
        if (_isExecutingTurn)
        {
            DebugLog("WARNING: Turn already executing. Aborting.");
            yield break;
        }

        if (IsDead)
        {
            DebugLog("INFO: Enemy is dead. Skipping turn.");
            yield break;
        }

        // NUEVA REGLA:
        // 0 = no se mueve nunca
        // 1 = 1 paso
        // 2 = 2 pasos
        int stepsThisTurn = Mathf.Max(0, StepsPerTurn);

        if (stepsThisTurn == 0)
        {
            DebugLog("TurnFrequency=0 -> enemy does NOT move this turn.");
            yield return new WaitForSeconds(0.05f);
            yield break;
        }

        if (currentTile == null)
        {
            DebugLog("ERROR: currentTile is NULL. Cannot take turn.");
            yield break;
        }

        var gm = GameManager.Instance;
        if (gm == null)
        {
            DebugLog("ERROR: GameManager.Instance is NULL. Cannot take turn.");
            yield break;
        }

        var player = gm.player;
        if (player == null || player.currentTile == null)
        {
            DebugLog("ERROR: Player or Player.currentTile is NULL. Cannot take turn.");
            yield break;
        }

        if (_actions == null)
        {
            DebugLog("ERROR: EnemyActions component is missing. Cannot take turn.");
            yield break;
        }

        _isExecutingTurn = true;

        // Para evitar loops raros si algo falla
        int executedSteps = 0;

        while (executedSteps < stepsThisTurn)
        {
            // Si ya no tenemos tile actual válida, abort
            if (currentTile == null)
            {
                DebugLog("ERROR: currentTile became NULL mid-turn.");
                break;
            }

            Vector2Int myPos = currentTile.gridPos;
            Vector2Int playerPos = player.currentTile.gridPos;

            // Si ya está adyacente en 4D, NO atacamos.
            // Puedes elegir: o se queda quieto, o intenta reposicionarse.
            // Para tu idea de "tag al final del turno del jugador", lo más coherente es: se queda quieto.
            if (BoardManager.Instance != null && BoardManager.Instance.AreAdjacent4D(myPos, playerPos))
            {
                DebugLog("Adjacent to player (4D) -> no attack. Staying still.");
                break;
            }

            bool IsBlocked(Vector2Int pos)
            {
                Tile t = BoardManager.Instance.GetTile(pos);
                return t != null && IsTileOccupiedByOtherEnemy(t);
            }

            if (AStarPathfinder.TryGetNextStepTowardPlayerAdj(
                    start: myPos,
                    playerPos: playerPos,
                    isBlocked: IsBlocked,
                    nextStep: out Vector2Int nextStep,
                    pathLength: out int pathLen))
            {
                // Seguridad extra: nunca moverse al tile del player
                if (nextStep == playerPos)
                {
                    DebugLog("Safety: nextStep equals playerPos. Aborting movement.");
                    break;
                }

                Tile nextTile = BoardManager.Instance.GetTile(nextStep);
                if (nextTile != null)
                {
                    DebugLog($"Step {executedSteps + 1}/{stepsThisTurn} -> Moving to {nextStep} (pathLen={pathLen})");
                    yield return _actions.MoveToTileCoroutine(nextTile);
                    executedSteps++;

                    // mini pausa visual entre pasos si hace 2 pasos
                    if (executedSteps < stepsThisTurn)
                        yield return new WaitForSeconds(0.05f);
                }
                else
                {
                    DebugLog("ERROR: Next step tile resolved to NULL. Stopping.");
                    break;
                }
            }
            else
            {
                DebugLog("No valid A* move found. Stopping.");
                break;
            }
        }

        DebugLog($"Turn end. Steps executed: {executedSteps}/{stepsThisTurn}");
        _isExecutingTurn = false;
    }

    private bool IsTileOccupiedByOtherEnemy(Tile tile)
    {
        var gm = GameManager.Instance;
        if (gm == null || tile == null) return false;

        foreach (var enemy in gm.enemies)
        {
            if (enemy == null || enemy.IsDead || enemy == this) continue;
            if (enemy.currentTile == tile) return true;
        }

        return false;
    }

    public void DebugLog(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[{name}] {message}");
    }
}