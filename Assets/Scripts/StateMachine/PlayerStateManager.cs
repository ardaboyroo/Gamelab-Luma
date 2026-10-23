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
	public event Action<PlayerStates> OnStateChanged;

	[SerializeField] private PlayerStates _initialState = PlayerStates.StandardState;
	private PlayerStates _playerState;

	private void Start()
	{
		Debug.Log("mngr");
		PlayerService.Instance.OnPlayerRegistered += (Player plr, bool isLocalPlayer) =>
		{
			if (isLocalPlayer)
			{
				_playerState = _initialState;
				OnStateChanged?.Invoke(_playerState);
				Debug.Log("Invoking OnStateChanged");
			}
		};
	}

	void Update()
	{

	}

	public void ChangePlayerState(PlayerStates state)
	{
		_playerState = state;
		OnStateChanged?.Invoke(_playerState);
	}

	public PlayerStates GetPlayerState()
	{
		return _playerState;
	}
}
