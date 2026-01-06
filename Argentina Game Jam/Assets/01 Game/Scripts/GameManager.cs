using UnityEngine;

public enum GameState { Playing, Won, Lost }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Rules")]
    public int maxHeat = 100;
    public int startingHeat = 0;
    public int startingMoves = 12;
    public int maxConsecutiveBurnTiles = 2;

    [Header("Runtime")]
    public GameState state { get; private set; } = GameState.Playing;
    public int heat { get; private set; }
    public int movesLeft { get; private set; }
    public int consecutiveBurnCount { get; private set; }

    [Header("Win/Lose")]
    public Tile startTile;
    public Tile goalTile;

    [Header("Refs")]
    public PlayerController player;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ResetRun();
    }

    public void ResetRun()
    {
        state = GameState.Playing;
        heat = startingHeat;
        movesLeft = startingMoves;
        consecutiveBurnCount = 0;

        if (player && startTile)
            player.SnapToTile(startTile);
    }

    public bool CanEnterTile(Tile tile)
    {
        if (state != GameState.Playing) return false;
        if (tile == null) return false;
        if (!tile.IsWalkable) return false;
        if (movesLeft <= 0) return false;

        // Regla Burn: no más de N seguidas
        if (tile.type == TileType.Burn && consecutiveBurnCount >= maxConsecutiveBurnTiles)
            return false;

        return true;
    }

    public void OnPlayerEnteredTile(Tile tile)
    {
        if (state != GameState.Playing) return;

        Debug.Log($"Player entered tile: {tile.type} | GridPos: {tile.gridPos}");

        movesLeft--;

        // Burn streak
        if (tile.type == TileType.Burn) consecutiveBurnCount++;
        else consecutiveBurnCount = 0;

        // Heat change
        heat = Mathf.Clamp(heat + tile.heatDeltaOnEnter, 0, maxHeat);

        // Consumibles
        if (tile.type == TileType.Shade || tile.type == TileType.Drink)
            tile.ConsumeIfNeeded();

        // Win / Lose
        if (heat >= maxHeat)
        {
            Lose("You´ve got Burnout");
            return;
        }

        if (tile == goalTile)
        {
            Win("You´ve arrived");
            return;
        }

        if (movesLeft <= 0)
        {
            Lose("No more movements left");
            return;
        }
    }

    private void Win(string msg)
    {
        state = GameState.Won;
        Debug.Log("You have won!");
    }

    private void Lose(string msg)
    {
        state = GameState.Lost;
        Debug.Log("You have lost!");
    }

    public void ShowBlocked(string msg)
    {
        Debug.Log(msg);
    }
}

