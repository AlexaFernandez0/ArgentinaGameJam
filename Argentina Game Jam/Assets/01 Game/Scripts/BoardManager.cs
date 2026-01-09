using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    private readonly Dictionary<Vector2Int, Tile> _tiles = new();

    [Header("Auto GridPos")]
    public Transform gridOrigin;
    public float cellSize = 1f;

    [Header("Runtime")]
    [SerializeField] private Transform activeLevelRoot; // debug/inspector

    public int TileCount => _tiles.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate BoardManager detected. Destroying the new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ------------------- NEW KEY METHOD -------------------
    public void BuildFromLevelRoot(Transform levelRoot)
    {
        if (levelRoot == null)
        {
            Debug.LogError("BuildFromLevelRoot: levelRoot is null.");
            return;
        }

        // 1) Auto gridPos ONLY inside this level
        AutoGridPositionsInTiles(levelRoot);

        // 2) Register ONLY tiles inside this level
        RegisterTilesInDictionary(levelRoot);

        Debug.Log($"BuildFromLevelRoot: Registered {_tiles.Count} tiles from '{levelRoot.name}'.");
    }

    // ------------------- INTERNALS -------------------
    private void RegisterTilesInDictionary(Transform root)
    {
        _tiles.Clear();

        var tiles = root.GetComponentsInChildren<Tile>(true);
        foreach (var t in tiles)
        {
            if (_tiles.ContainsKey(t.gridPos))
            {
                Debug.LogWarning($"Duplicate gridPos detected: {t.gridPos} (Tile: {t.name})");
                continue;
            }
            _tiles.Add(t.gridPos, t);
        }
    }

    private void AutoGridPositionsInTiles(Transform root)
    {
        if (gridOrigin == null)
        {
            Debug.LogWarning("Grid origin is not assigned. Auto gridPos skipped.");
            return;
        }

        var tiles = root.GetComponentsInChildren<Tile>(true);
        Vector3 o = gridOrigin.position;

        for (int i = 0; i < tiles.Length; i++)
        {
            var t = tiles[i];
            Vector3 p = t.transform.position;

            int gx = Mathf.RoundToInt((p.x - o.x) / cellSize);
            int gy = Mathf.RoundToInt((p.z - o.z) / cellSize);

            t.gridPos = new Vector2Int(gx, gy);
        }
    }

    // ------------------- API -------------------
    public Tile GetTile(Vector2Int pos)
        => _tiles.TryGetValue(pos, out var t) ? t : null;

    public bool AreAdjacent4D(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) == 1;
    }

    public bool AreAdjacent8D(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        if (dx == 0 && dy == 0) return false;
        return dx <= 1 && dy <= 1;
    }
}
