using UnityEngine;

[System. Serializable]
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
            player.animator.SetBool("IsSliding", false);
        }
    }

    public void UpdateState(PlayerController player)
    {
        // No hacer nada aquí, el cambio de estado se maneja en Jump/Slide
    }

    public void FixedUpdateState(PlayerController player)
    {
    }

    public void ExitState(PlayerController player)
    {
    }
}