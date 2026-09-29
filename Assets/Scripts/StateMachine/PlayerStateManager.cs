using System;
using UnityEngine;

public enum PlayerStates
{
    StandardState,
    MenuState,
    DialogueState,
    CutsceneState,
    SpraywayState
}

public class PlayerStateManager : MonoBehaviour
{
    public event Action<PlayerStates> StateChangedEvent;

    private PlayerStates playerState = PlayerStates.StandardState;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void ChangePlayerState(PlayerStates state)
    {
        playerState = state;
        StateChangedEvent?.Invoke(playerState);
    }

    public PlayerStates GetPlayerState()
    {
        return playerState;
    }
}
