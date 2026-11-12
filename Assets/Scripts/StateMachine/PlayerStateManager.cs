using System;
using UnityEngine;

public enum PlayerStates
{
	StandardState,
	MenuState,
	CharacterEditState,
	DialogueState,
	CutsceneState,
	HouseEditState
}

public class PlayerStateManager : MonoBehaviour
{
	public event Action<PlayerStates> OnStateChanged;

	[SerializeField] private PlayerStates _initialState = PlayerStates.StandardState;
	private PlayerStates _playerState;

	public PlayerStates CurrentState => _playerState;

	// Subscribe in Awake (not Start): Player.Start registers the player, and Unity doesn't guarantee
	// which Start runs first. Every Awake runs before any Start, so the first state is never missed.
	private void Awake()
	{
		Debug.Log("mngr");
		PlayerService.OnPlayerRegistered += HandlePlayerRegistered;
	}

	private void OnDestroy()
	{
		PlayerService.OnPlayerRegistered -= HandlePlayerRegistered;
	}

	private void HandlePlayerRegistered(Player plr, bool isLocalPlayer)
	{
		if (isLocalPlayer && plr.StateManager == this)
		{
			_playerState = _initialState;
			Invoke("DeferredCall", .1f);
			Debug.Log($"[PlayerStateManager] Invoking OnStateChanged: {_playerState}");
		}
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
