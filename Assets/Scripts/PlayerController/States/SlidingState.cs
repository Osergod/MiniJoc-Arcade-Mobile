using UnityEngine;

[System.Serializable]
public class SlidingState : IPlayerState
{
    private float slideEndTime;
    
    public void EnterState(PlayerController player)
    {
        slideEndTime = Time.time + player.slideDuration;
        player.LockYPosition();
        
        if (player.animator != null)
        {
            player.animator.SetTrigger("Slide");
            player.animator.SetBool("IsSliding", true);
        }
        
        if (player.playerCollider != null)
        {
            player.playerCollider.height = player.slideHeight;
        }
    }
    
    public void UpdateState(PlayerController player)
    {
        player.isGrounded = player.CheckGroundContact();
        
        if (Time.time >= slideEndTime)
        {
            player.ChangeState(PlayerController.PlayerState.Grounded);
            return;
        }
        
        if (!player.isGrounded)
        {
            player.ChangeState(PlayerController.PlayerState.Falling);
            return;
        }
        
        player.MoveForward();
        player.SmoothLaneSwitch();
    }
    
    public void FixedUpdateState(PlayerController player)
    {
        // Lógica física del slide
    }
    
    public void ExitState(PlayerController player)
    {
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