// ========== INTERFAZ DE ESTADO ==========
public interface IPlayerState
{
    void EnterState(PlayerStateMachine player);
    void UpdateState(PlayerStateMachine player);
    void FixedUpdateState(PlayerStateMachine player);
    void ExitState(PlayerStateMachine player);
}