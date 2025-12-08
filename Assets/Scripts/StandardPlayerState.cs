public class StandardPlayerState : PlayerState
{
    public override void OnEnter()
    {
        base.OnEnter();

        PlayerStateMachine.Instance.GetComponent<WorldMovement>().NullifyTarget();
    }

    // Extra logic for this state if you need it later.
    // For now OnEnter/OnExit can stay inherited.
}
