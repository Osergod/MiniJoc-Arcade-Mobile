using UnityEngine;

[System.Serializable]
public class GroundedState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        player.verticalVelocity = 0f;
        player.LockYPosition();
        
        if (player.animator != null)
        {
            player.animator.SetTrigger("Land");
            player.animator.SetBool("IsGrounded", true);
            player.animator.SetBool("IsJumping", false);
            player.animator.SetBool("IsFalling", false);
            player.animator.SetBool("IsSliding", false);
        }
        
        if (player.enableAutoJump)
        {
            player.nextAutoJumpTime = Time.time + Random.Range(
                player.autoJumpIntervalMin, 
                player.autoJumpIntervalMax
            );
        }
    }
    
    public void UpdateState(PlayerController player)
    {
        player.isGrounded = player.CheckGroundContact();
        
        if (!player.isGrounded)
        {
            player.ChangeState(PlayerController.PlayerState.Falling);
            return;
        }
        
        player.MoveForward();
        player.SmoothLaneSwitch();
        
        CheckAutoJump(player);
    }
    
    public void FixedUpdateState(PlayerController player)
    {
        // Lógica física específica del estado grounded
    }
    
    public void ExitState(PlayerController player)
    {
        // Limpieza al salir del estado
    }
    
    private void CheckAutoJump(PlayerController player)
    {
        if (player.enableAutoJump && Time.time >= player.nextAutoJumpTime)
        {
            player.Jump();
            player.nextAutoJumpTime = Time.time + Random.Range(
                player.autoJumpIntervalMin, 
                player.autoJumpIntervalMax
            );
        }
    }
}