using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.CullingGroup;

public class PlayerService : MonoBehaviour
{
	public static PlayerService Instance { get; private set; }

	public Player LocalPlayer { get; private set; }

	public static Action<Player, bool> OnPlayerRegistered;

	public IReadOnlyList<Player> Players => _players;

	private readonly List<Player> _players = new();

	private void Awake()
	{
		Debug.Log("plrsrv");
		if (!Instance)
			Instance = this;
		else
		{
			Debug.LogWarning("A PlayerService already exists, deleting this instance");
			Destroy(this);
		}
	}

	public void RegisterPlayer(Player plr)
	{
		_players.Add(plr);

		OnPlayerRegistered?.Invoke(plr, LocalPlayer == null);

		Debug.Log("[PlayerService] Player added, invoking OnPlayerRegistered event");

		// We assume that the first registered player is the localplayer		
		if (!LocalPlayer)
			LocalPlayer = plr;
	}

    public void UnregisterPlayer()
	{

	}

	public Player GetPlayer(ulong id)
	{

		return null;
	}
}
