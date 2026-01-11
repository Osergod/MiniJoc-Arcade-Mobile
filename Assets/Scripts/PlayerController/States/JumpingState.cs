using UnityEngine;

[System.Serializable]
public class JumpingState : IPlayerState
{
    private bool isAscending = true;

    public void EnterState(PlayerController player)
    {
        player.verticalVelocity = player.jumpForce;
        player.UnlockYPosition();
        isAscending = true;

        if (player.animators != null)
        {
            foreach (Animator anim in player.animators)
            {
                if (anim == null) continue;

                anim.SetTrigger("Jump");
                anim.SetBool("IsJumping", true);
                anim.SetBool("IsGrounded", false);
            }
        }
    }

    public void UpdateState(PlayerController player)
    {
        if (isAscending && player.verticalVelocity <= 0)
            isAscending = false;

        if (!isAscending && player.isGrounded && player.verticalVelocity <= 0)
            player.ChangeState(PlayerController.PlayerState.Grounded);
    }

    public void FixedUpdateState(PlayerController player) { }

    public void ExitState(PlayerController player)
    {
        if (player.animators != null)
        {
            foreach (Animator anim in player.animators)
            {
                if (anim == null) continue;

                anim.SetBool("IsJumping", false);
            }
        }

        isAscending = true;
    }
}
