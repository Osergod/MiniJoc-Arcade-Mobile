using UnityEngine;

[System.Serializable]
public class SlidingState : IPlayerState
{
    private float slideEndTime;
    
    public void EnterState(PlayerStateMachine player)
    {
        slideEndTime = Time.time + player.slideDuration;
        player.FreezeYPosition();
        
        if (player.animator != null)
        {
            player.animator.SetTrigger("Slide");
            player.animator.SetBool("IsSliding", true);
        }
        
        // Ajustar altura del collider
        if (player.playerCollider != null)
        {
            player.playerCollider.height = player.slideHeight;
        }
    }
    
    public void UpdateState(PlayerStateMachine player)
    {
        player.isGrounded = player.CheckGround();
        
        // Verificar fin del slide
        if (Time.time >= slideEndTime)
        {
            player.ChangeState(player.groundedState);
            return;
        }
        
        // Verificar si deja de estar grounded durante el slide
        if (!player.isGrounded)
        {
            player.ChangeState(player.fallingState);
            return;
        }
        
        // Movimiento horizontal
        player.MoveForward();
        player.SmoothLaneSwitch();
    }
    
    public void FixedUpdateState(PlayerStateMachine player)
    {
        // Lógica física del slide
    }
    
    public void ExitState(PlayerStateMachine player)
    {
        // Restaurar altura del collider
        if (player.playerCollider != null)
        {
            player.playerCollider.height = player.originalHeight;
        }
        
        if (player.animator != null)
        {
            player.animator.SetBool("IsSliding", false);
        }
    }
}