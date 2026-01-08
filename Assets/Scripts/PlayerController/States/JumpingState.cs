using UnityEngine;

[System.Serializable]
public class JumpingState :  IPlayerState
{
    private bool isAscending = true;

    public void EnterState(PlayerController player)
    {
        player.verticalVelocity = player.jumpForce;
        player.UnlockYPosition();
        isAscending = true;

        if (player.animator != null)
        {
            player.animator.SetTrigger("Jump");
            player.animator.SetBool("IsJumping", true);
            player. animator.SetBool("IsGrounded", false);
        }
    }

    public void UpdateState(PlayerController player)
    {
        // Detectar cambio de fase (de ascenso a descenso)
        if (isAscending && player.verticalVelocity <= 0)
        {
            isAscending = false;
        }

        // Verificar si el jugador ha llegado al suelo
        if (! isAscending && player.isGrounded && player.verticalVelocity <= 0)
        {
            player.ChangeState(PlayerController.PlayerState.Grounded);
            return;
        }
    }

    public void FixedUpdateState(PlayerController player)
    {
    }

    public void ExitState(PlayerController player)
    {
        if (player.animator != null)
        {
            player.animator. SetBool("IsJumping", false);
        }

        isAscending = true;
    }
}