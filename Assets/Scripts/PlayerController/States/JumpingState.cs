using UnityEngine;

[System.Serializable]
public class JumpingState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        player.verticalVelocity = player.jumpForce;
        player.UnlockYPosition();
        
        if (player.animator != null)
        {
            player.animator.SetTrigger("Jump");
            player.animator.SetBool("IsJumping", true);
            player.animator.SetBool("IsGrounded", false);
        }
    }
    
    public void UpdateState(PlayerController player)
    {
        player.ApplyGravityForce();
        
        if (player.verticalVelocity < 0)
        {
            player.ChangeState(PlayerController.PlayerState.Falling);
            return;
        }
        
        player.MoveForward();
        player.SmoothLaneSwitch();
        
        if (player.animator != null)
        {
            player.animator.SetFloat("VerticalVelocity", player.verticalVelocity);
        }
    }
    
    public void FixedUpdateState(PlayerController player)
    {
        player.ApplyVerticalVelocity();
    }
    
    public void ExitState(PlayerController player)
    {
        if (player.animator != null)
        {
            player.animator.SetBool("IsJumping", false);
        }
    }
}