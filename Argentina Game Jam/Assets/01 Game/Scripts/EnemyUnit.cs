using System.Collections;
using UnityEngine;

public class EnemyUnit : MonoBehaviour
{
    public int health = 2;
    public Tile currentTile;
    public bool IsDead => health <= 0;


    private void Start()
    {
        SnapToTile(currentTile);
    }

    public void SnapToTile(Tile tile)
    {
        currentTile = tile;
        if (tile) transform.position = tile.transform.position;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"Enemy took damage: {amount}. HP: {health}");

        if (health <= 0)
        {
            Debug.Log("Enemy defeated.");
            GameManager.Instance.RemoveEnemy(this);
            Destroy(gameObject);
        }
    }

    public IEnumerator TakeTurnCoroutine()
    {
        // Day 1: do nothing
        yield return null;
    }
}
