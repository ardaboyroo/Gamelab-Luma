using UnityEngine;

[RequireComponent(typeof(PlayerStateManager))]
public class Player : MonoBehaviour
{
	private PlayerStateManager _stateManager;

	public PlayerStateManager StateManager => _stateManager;

	private void Awake()
	{
		_stateManager = GetComponent<PlayerStateManager>();
	}

	void Start()
	{
		PlayerService.Instance.RegisterPlayer(this);
		Debug.Log("[Player] registering player in start");
	}

	private void OnDestroy()
	{
		PlayerService.Instance.UnregisterPlayer(this);
	}

	// Update is called once per frame
	void Update()
	{

	}
}
