using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    private readonly Dictionary<Vector2Int, Tile> _tiles = new();

    private void Awake()
    {
        Instance = this;
        RegisterAllTilesInChildren();
    }

    private void RegisterAllTilesInChildren()
    {
        _tiles.Clear();
        var tiles = GetComponentsInChildren<Tile>(true);
        foreach (var t in tiles)
        {
            if (_tiles.ContainsKey(t.gridPos))
            {
                Debug.LogWarning($"Tile duplicado en {t.gridPos} -> {t.name}");
                continue;
            }
            _tiles.Add(t.gridPos, t);
        }
    }

    public Tile GetTile(Vector2Int pos)
        => _tiles.TryGetValue(pos, out var t) ? t : null;

    public bool AreAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) == 1; // sin diagonales
    }
}
