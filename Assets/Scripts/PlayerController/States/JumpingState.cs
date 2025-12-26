using UnityEngine;

[System.Serializable]
public class JumpingState : IPlayerState
{
    public void EnterState(PlayerStateMachine player)
    {
        player.verticalVelocity = player.jumpForce;
        player.UnfreezeYPosition();
        
        if (player.animator != null)
        {
            player.animator.SetTrigger("Jump");
            player.animator.SetBool("IsJumping", true);
            player.animator.SetBool("IsGrounded", false);
        }
    }
    
    public void UpdateState(PlayerStateMachine player)
    {
        player.ApplyGravity();
        
        // Verificar transición a Falling
        if (player.verticalVelocity < 0)
        {
            player.ChangeState(player.fallingState);
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
            player.animator.SetBool("IsJumping", false);
        }
    }
}