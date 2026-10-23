using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerService : MonoBehaviour
{
	public static PlayerService Instance { get; private set; }

	public Player LocalPlayer { get; private set; }

	public Action<Player, bool> OnPlayerRegistered;

	public IReadOnlyList<Player> Players => players;

	private readonly List<Player> players = new();

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
		players.Add(plr);

		OnPlayerRegistered?.Invoke(plr, LocalPlayer == null);

		Debug.Log("Player added, invoking event");

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
