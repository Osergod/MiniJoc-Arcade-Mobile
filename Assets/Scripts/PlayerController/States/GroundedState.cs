using UnityEngine;

[System.Serializable]
public class GroundedState : IPlayerState
{
    public void EnterState(PlayerController player)
    {
        player.verticalVelocity = 0f;
        player.LockYPosition();

        if (player.animators != null)
        {
            foreach (Animator anim in player.animators)
            {
                if (anim == null) continue;

                anim.SetTrigger("Land");
                anim.SetBool("IsGrounded", true);
                anim.SetBool("IsJumping", false);
                anim.SetBool("IsSliding", false);
            }
        }
    }

    public void UpdateState(PlayerController player) { }

    public void FixedUpdateState(PlayerController player) { }

    public void ExitState(PlayerController player) { }
}
