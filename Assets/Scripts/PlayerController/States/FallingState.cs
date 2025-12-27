using UnityEngine;

[System.Serializable]
public class FallingState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        player.UnlockYPosition();
        
        if (player.animator != null)
        {
            player.animator.SetBool("IsFalling", true);
            player.animator.SetBool("IsGrounded", false);
        }
    }
    
    public void UpdateState(PlayerController player)
    {
        player.isGrounded = player.CheckGroundContact();
        player.ApplyGravityForce();
        
        if (player.isGrounded)
        {
            player.ChangeState(PlayerController.PlayerState.Grounded);
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
            player.animator.SetBool("IsFalling", false);
        }
    }
}