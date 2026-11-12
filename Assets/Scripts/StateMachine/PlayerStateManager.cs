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
		NotifyStateChanged();
	}

	// Calls every listener separately, so one broken listener (e.g. a leftover from an unloaded scene)
	// logs its error but can't stop the others from hearing about the state change.
	private void NotifyStateChanged()
	{
		var handlers = OnStateChanged;
		if (handlers == null)
			return;

		foreach (Action<PlayerStates> handler in handlers.GetInvocationList())
		{
			try
			{
				handler(_playerState);
			}
			catch (Exception e)
			{
				Debug.LogException(e, this);
			}
		}
	}

	void Update()
	{

	}

	public void ChangePlayerState(PlayerStates state)
	{
		_playerState = state;
		NotifyStateChanged();
	}

	public PlayerStates GetPlayerState()
	{
		return _playerState;
	}
}
