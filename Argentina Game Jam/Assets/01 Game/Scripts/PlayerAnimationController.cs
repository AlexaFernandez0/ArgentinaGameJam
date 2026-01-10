using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;  // Arrastra aquí el Animator del hijo (la maya)
    
    [Header("Animation Parameters")]
    private static readonly int IsMoving = Animator.StringToHash("isMoving");
    private static readonly int Attack = Animator.StringToHash("attack");
    private static readonly int Die = Animator.StringToHash("die");
    
    private PlayerController _playerController;
    
    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("No se encontró Animator en los hijos del Player.");
            }
        }
    }
    
    private void Update()
    {
        if (animator == null || _playerController == null) return;
        
        // Actualizar el estado de movimiento
        animator.SetBool(IsMoving, _playerController.IsMoving);
    }
    
    public void PlayAttack()
    {
        if (animator == null) return;
        animator.SetTrigger(Attack);
    }
    
    public void PlayDeath()
    {
        if (animator == null) return;
        animator.SetTrigger(Die);
    }

    // Reset total del Animator al reiniciar nivel
    public void ResetToIdle()
    {
        if (animator == null) return;

        // 0) Cortar cualquier cosa rara
        animator.enabled = false;
        animator.enabled = true;

        // 1) Reset total del Animator
        animator.Rebind();
        animator.Update(0f);

        // 2) Reset parámetros (triggers + bools típicos)
        animator.ResetTrigger(Attack);
        animator.ResetTrigger(Die);

        animator.SetBool(IsMoving, false);

        // Si tienes algún bool tipo IsDead / Dead / Knocked / Stunned, ponlo a false aquí
        // animator.SetBool(IsDead, false);

        // 3) Forzar Idle
        animator.Play("Idle", 0, 0f);
        animator.Update(0f);

        Debug.Log("✅ Player animation reset to Idle (NUCLEAR).");
    }

}