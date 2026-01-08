using UnityEngine;

[System. Serializable]
public class SlidingState : IPlayerState
{
    private float slideEndTime;
    private float originalHeight;
    private Vector3 originalCenter;
    private bool hasReducedCollider = false;

    public void EnterState(PlayerController player)
    {
        slideEndTime = Time. time + player.slideDuration;
        player.LockYPosition();
        hasReducedCollider = false;

        Debug.Log("DESLIZAMIENTO INICIADO");

        if (player. animator != null)
        {
            player.animator.SetTrigger("Slide");
            player.animator. SetBool("IsSliding", true);
            Debug.Log("Animación de deslizamiento activada");
        }

        // Reducir el collider
        if (player.playerCollider != null)
        {
            originalHeight = player.playerCollider. height;
            originalCenter = player.playerCollider. center;
            
            float newHeight = player.slideHeight;
            
            Debug.Log($"Altura original: {originalHeight}");
            Debug.Log($"Centro original: {originalCenter}");
            Debug.Log($"Nueva altura (slideHeight): {newHeight}");
            
            player.playerCollider.height = newHeight;
            
            // Ajustar el center del collider para que baje
            // El center debe ser la mitad de la nueva altura negativa (para que baje)
            Vector3 newCenter = originalCenter;
            newCenter.y = -(originalHeight - newHeight) / 2f;
            player.playerCollider. center = newCenter;
            
            hasReducedCollider = true;
            Debug.Log($"Collider reducido a altura:  {player.playerCollider.height}");
            Debug.Log($"Nuevo center: {player.playerCollider.center}");
        }
        else
        {
            Debug.LogWarning("playerCollider es NULL en SlidingState. EnterState");
        }
    }

    public void UpdateState(PlayerController player)
    {
        // Verificar si el deslizamiento ha terminado
        if (Time.time >= slideEndTime)
        {
            Debug.Log("Deslizamiento finalizado por tiempo");
            player.ChangeState(PlayerController.PlayerState. Grounded);
            return;
        }

        // Si el personaje se cae mientras se desliza
        if (! player.isGrounded)
        {
            Debug.Log("Personaje cayendo durante el deslizamiento");
            player.ChangeState(PlayerController.PlayerState.Jumping);
            return;
        }

        // Debug continuo
        Debug.Log($"Deslizando...  Tiempo restante: {slideEndTime - Time.time:F2}s");
    }

    public void FixedUpdateState(PlayerController player)
    {
        // Lógica física del deslizamiento (si es necesaria)
    }

    public void ExitState(PlayerController player)
    {
        Debug.Log("Saliendo del estado de deslizamiento");

        // Restaurar el collider a su altura y centro original
        if (player. playerCollider != null && hasReducedCollider)
        {
            player.playerCollider.height = originalHeight;
            player.playerCollider.center = originalCenter;
            
            Debug.Log($"Collider restaurado a altura:  {player.playerCollider.height}");
            Debug.Log($"Centro restaurado: {player.playerCollider. center}");
        }

        if (player.animator != null)
        {
            player.animator.SetBool("IsSliding", false);
        }
    }
}