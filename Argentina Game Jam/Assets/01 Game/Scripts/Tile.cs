using UnityEngine;

public enum TileType
{
    Starting,
    End,
    Sun,
    Burn,
    Shade,
    Drink,
    Blocked
}

public class Tile : MonoBehaviour
{
    [Header("Grid")]
    public Vector2Int gridPos;

    [Header("Type")]
    public TileType type = TileType.Sun;

    [Header("Heat Effects")]
    public int heatDeltaOnEnter = 5;   // NormalSun: +X, Shade/Drink: negativo, Burn: alto, Blocked: 0

    [Header("Consumable")]
    public bool oneShot = false;       // Shade/Drink (si quieres)
    public GameObject oneShotVisual;   // opcional: icono bebida, sombra, etc.

    public bool IsWalkable => type != TileType.Blocked;

    public void ConsumeIfNeeded()
    {
        if (!oneShot) return;

        // Convertimos el tile a normal al consumirse
        oneShot = false;
        type = TileType.Sun;
        heatDeltaOnEnter = Mathf.Max(0, heatDeltaOnEnter); // por si era negativo, lo neutralizamos
        if (oneShotVisual) oneShotVisual.SetActive(false);
    }
}

