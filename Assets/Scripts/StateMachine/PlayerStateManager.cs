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

	public PlayerStates CurrentState => _playerState;

	private void Start()
	{
		Debug.Log("mngr");
		PlayerService.Instance.OnPlayerRegistered += (Player plr, bool isLocalPlayer) =>
		{
			if (isLocalPlayer)
			{
				_playerState = _initialState;
				Invoke("DeferredCall", .1f);
				Debug.Log($"Invoking OnStateChanged: {_playerState}");
			}
		};
	}

	private void DeferredCall()
	{
		OnStateChanged?.Invoke(_playerState);
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
