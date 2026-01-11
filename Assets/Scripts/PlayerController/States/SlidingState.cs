using UnityEngine;

[System.Serializable]
public class SlidingState : IPlayerState
{
    private float slideEndTime;
    private float originalHeight;
    private Vector3 originalCenter;
    private bool hasReducedCollider = false;

    public void EnterState(PlayerController player)
    {
        slideEndTime = Time.time + player.slideDuration;
        player.LockYPosition();
        hasReducedCollider = false;

        Debug.Log("DESLIZAMIENTO INICIADO");

        if (player.animators != null)
        {
            foreach (Animator anim in player.animators)
            {
                if (anim == null) continue;

                anim.SetTrigger("Slide");
                anim.SetBool("IsSliding", true);
            }
        }

        if (player.playerCollider != null)
        {
            originalHeight = player.playerCollider.height;
            originalCenter = player.playerCollider.center;

            float newHeight = player.slideHeight;
            player.playerCollider.height = newHeight;

            Vector3 newCenter = originalCenter;
            newCenter.y = -(originalHeight - newHeight) / 2f;
            player.playerCollider.center = newCenter;

            hasReducedCollider = true;
        }
    }

    public void UpdateState(PlayerController player)
    {
        if (Time.time >= slideEndTime)
        {
            player.ChangeState(PlayerController.PlayerState.Grounded);
            return;
        }

        if (!player.isGrounded)
        {
            player.ChangeState(PlayerController.PlayerState.Jumping);
            return;
        }
    }

    public void FixedUpdateState(PlayerController player) { }

    public void ExitState(PlayerController player)
    {
        if (player.playerCollider != null && hasReducedCollider)
        {
            player.playerCollider.height = originalHeight;
            player.playerCollider.center = originalCenter;
        }

        if (player.animators != null)
        {
            foreach (Animator anim in player.animators)
            {
                if (anim == null) continue;

                anim.SetBool("IsSliding", false);
            }
        }
    }
}
