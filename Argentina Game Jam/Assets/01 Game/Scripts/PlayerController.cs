using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;

    public Tile currentTile { get; private set; }
    private bool _isMoving;

    public void SnapToTile(Tile tile)
    {
        currentTile = tile;
        if (tile)
        {
            transform.position = tile.transform.position;
        }
    }

    public void TryMoveTo(Tile target)
    {
        if (_isMoving) return;
        if (currentTile == null || target == null) return;

        if (!BoardManager.Instance.AreAdjacent(currentTile.gridPos, target.gridPos))
        {
            GameManager.Instance.ShowBlocked("Not Allowed. Just adjacents Tiles (No diagonals)");
            return;
        }

        if (!GameManager.Instance.CanEnterTile(target))
        {
            // Mensajes específicos para Burn cap
            if (target.type == TileType.Burn)
                GameManager.Instance.ShowBlocked("No puedes pisar tantas rojas seguidas.");
            else if (!target.IsWalkable)
                GameManager.Instance.ShowBlocked("Bloqueado.");
            else
                GameManager.Instance.ShowBlocked("No puedes moverte ahí por otra razón.");
            return;
        }

        StartCoroutine(MoveRoutine(target));
    }

    private IEnumerator MoveRoutine(Tile target)
    {
        _isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = target.transform.position;

        while ((transform.position - end).sqrMagnitude > 0.0004f)
        {
            transform.position = Vector3.MoveTowards(transform.position, end, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = end;
        currentTile = target;

        GameManager.Instance.OnPlayerEnteredTile(target);

        _isMoving = false;
    }
}

