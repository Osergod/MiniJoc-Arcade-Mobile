using UnityEngine;

[System.Serializable]
public class FallingState : IPlayerState
{
    public void EnterState(PlayerStateMachine player)
    {
        player.UnfreezeYPosition();
        
        if (player.animator != null)
        {
            player.animator.SetBool("IsFalling", true);
            player.animator.SetBool("IsGrounded", false);
        }
    }
    
    public void UpdateState(PlayerStateMachine player)
    {
        player.isGrounded = player.CheckGround();
        player.ApplyGravity();
        
        // Verificar transición a Grounded
        if (player.isGrounded)
        {
            player.ChangeState(player.groundedState);
            return;
        }
        
        // Movimiento horizontal
        player.MoveForward();
        player.SmoothLaneSwitch();
        
        if (player.animator != null)
        {
            player.animator.SetFloat("VerticalVelocity", player.verticalVelocity);
        }
    }
    
    public void FixedUpdateState(PlayerStateMachine player)
    {
        // El movimiento vertical se maneja en FixedUpdate del PlayerStateMachine
    }
    
    public void ExitState(PlayerStateMachine player)
    {
        if (player.animator != null)
        {
            player.animator.SetBool("IsFalling", false);
        }
    }
}