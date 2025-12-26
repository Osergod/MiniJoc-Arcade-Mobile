using UnityEngine;

[System.Serializable]
public class GroundedState : IPlayerState
{
    public void EnterState(PlayerStateMachine player)
    {
        player.verticalVelocity = 0f;
        player.FreezeYPosition();
        
        if (player.animator != null)
        {
            player.animator.SetTrigger("Land");
            player.animator.SetBool("IsGrounded", true);
            player.animator.SetBool("IsJumping", false);
            player.animator.SetBool("IsFalling", false);
            player.animator.SetBool("IsSliding", false);
        }
        
        // Programar siguiente salto automático
        if (player.enableAutoJump)
        {
            player.nextAutoJumpTime = Time.time + Random.Range(
                player.autoJumpIntervalMin, 
                player.autoJumpIntervalMax
            );
        }
    }
    
    public void UpdateState(PlayerStateMachine player)
    {
        player.isGrounded = player.CheckGround();
        
        // Verificar transición a Falling
        if (!player.isGrounded)
        {
            player.ChangeState(player.fallingState);
            return;
        }
        
        // Movimiento horizontal
        player.MoveForward();
        player.SmoothLaneSwitch();
        
        // Verificar salto automático
        CheckAutoJump(player);
    }
    
    public void FixedUpdateState(PlayerStateMachine player)
    {
        // Lógica física específica del estado grounded
    }
    
    public void ExitState(PlayerStateMachine player)
    {
        // Limpieza al salir del estado
    }
    
    private void CheckAutoJump(PlayerStateMachine player)
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