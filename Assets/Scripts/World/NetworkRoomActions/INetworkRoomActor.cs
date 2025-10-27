using System;

public interface INetworkRoomActor
{
    public void OnClientEntering(ulong clientId);
    public void OnClientAwake(ulong clientId);
    public void OnClientStart(ulong clientId);
    public void OnClientUpdate(ulong clientId);
    public void OnClientExiting(ulong clientId);
}
