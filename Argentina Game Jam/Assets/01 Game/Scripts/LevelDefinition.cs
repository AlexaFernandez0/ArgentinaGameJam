using UnityEngine;

public class LevelDefinition : MonoBehaviour
{
    [Header("Level Tiles")]
    [Tooltip("Tile where the player starts in this level")]
    public Tile startTile;

    [Tooltip("Tile that ends the level")]
    public Tile goalTile;

    [Header("Level Enemies")]
    [Tooltip("Optional: Enemies that belong to this level. If empty, they will be auto-detected.")]
    public EnemyUnit[] enemies;

    [Header("Grid Origin")]
    public Transform anchor; // empty child, define where (0,0) should be

    [Header("Debug")]
    public bool autoCollectEnemiesOnAwake = true;

    private void Awake()
    {
        // This is NOT game logic, just data convenience
        if (autoCollectEnemiesOnAwake && (enemies == null || enemies.Length == 0))
        {
            enemies = GetComponentsInChildren<EnemyUnit>(true);
        }

        if (startTile != null)
        {
            anchor = startTile.transform;
        }
    }
}

