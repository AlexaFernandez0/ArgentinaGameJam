using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference pointAction;
    public InputActionReference clickAction;

    [Header("Raycast")]
    public Camera cam;
    public LayerMask tileLayer;

    [Header("Refs")]
    public PlayerController player;

    private void OnEnable()
    {
        pointAction.action.Enable();
        clickAction.action.Enable();
        clickAction.action.performed += OnClickPerformed;
    }

    private void OnDisable()
    {
        clickAction.action.performed -= OnClickPerformed;
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance.state != TurnState.PlayerTurn ) return;

        Vector2 screenPos = pointAction.action.ReadValue<Vector2>();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, tileLayer))
        {
            if (hit.collider.TryGetComponent<Tile>(out var tile))
            {
                // If tile has enemy: attack if possible
                var enemy = GameManager.Instance.GetEnemyOnTile(tile);

                if (enemy != null)
                {
                    if (GameManager.Instance.CanAttackEnemyOnTile(tile))
                    {
                        GameManager.Instance.AttackEnemyOnTile(tile);
                    }
                    else
                    {
                        Debug.Log("Enemy on tile, but attack is not possible.");
                    }

                    return; // IMPORTANT: don't attempt movement onto enemy tile
                }

                // Otherwise, treat click as movement
                player.TryMoveTo(tile);
            }
        }
    }
}

