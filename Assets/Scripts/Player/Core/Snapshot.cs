public struct Snapshot<StateType>
{
    public StateType State;
    public float Timestamp;

    public Snapshot(StateType state, float timestamp)
    {
        State = state;
        Timestamp = timestamp;
    }
}
