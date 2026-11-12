using UnityEngine;
using FMODUnity;
using System.Collections;

public class WorldMovement : MonoBehaviour
{
	[Tooltip("The state this script is active on")]
	[SerializeField] private PlayerStates _workingState = PlayerStates.StandardState;

	[Header("Movement")]
	[SerializeField] private Rigidbody _rigidbody;
	[SerializeField] private float _moveSpeed = 5f;
	[SerializeField] private float _skateSpeed = 10f;
	[SerializeField] private float _rotationSpeed = 360f;
	[SerializeField] private GameObject _skateboard;
	[SerializeField] private Transform _modelTransform;
	[SerializeField] private Vector3 _skateboardModelOffset;
	private bool _isSkating = false;

	[Header("Touch Joystick")]
	[SerializeField] private FloatingJoystick _joystick;

	[Header("Ground Raycast")]
	[SerializeField] private LayerMask _groundMask;
	[SerializeField] private float _rayLength = 1000f;

	[Header("Audio Settings")]
	[SerializeField] private EventReference _footstepEvent;
	[SerializeField] private float _stepDistance = 1.8f;

	[Space(10)]
	[SerializeField] private Animator _animator;

	private float _currentStepTracker = 0f;

	private bool _isActive = false;

	private void Awake()
	{		
        PlayerService.OnPlayerRegistered += (Player plr, bool isLocalPlayer) =>
		{
			if (isLocalPlayer)
			{
				plr.StateManager.OnStateChanged += HandleStateChanged;
			}
		};
	}

	private void Start()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_rigidbody.freezeRotation = true;

		StartCoroutine(IdleActiveAnimation());
	}

	private void OnDisable()
	{
		PlayerService.Instance.LocalPlayer.StateManager.OnStateChanged -= HandleStateChanged;
	}

	public void ToggleSkateBoard()
	{
		_isSkating = !_isSkating;

		if (_isSkating) ActivateSkateboard();
		else DeactivateSkateboard();
	}

	private void ActivateSkateboard()
	{
		_skateboard.SetActive(true);
		_modelTransform.localPosition = _skateboardModelOffset;
	}

	private void DeactivateSkateboard()
	{
		_skateboard.SetActive(false);
		_modelTransform.localPosition = Vector3.zero;
	}

	private void HandleStateChanged(PlayerStates states)
	{
		if (states == _workingState)
		{
			_isActive = true;
		}
		else
		{
			_isActive = false;
		}
	}

	private void Update()
	{
		if (!_isActive)
			return;

		if (Input.GetKeyDown(KeyCode.F1))
		{
			_animator.SetTrigger("dance_1");
		}

		if (Input.GetKeyDown(KeyCode.F2))
		{
			_animator.SetTrigger("dance_2");
		}
	}

	private void FixedUpdate()
	{
		if (_rigidbody == null)
			return;

		Vector2 joystickInput = _joystick.Direction;

		// Get camera-relative directions
		Transform cameraTransform = Camera.main.transform;

		Vector3 cameraForward = cameraTransform.forward;
		Vector3 cameraRight = cameraTransform.right;

		// Flatten directions onto the ground
		cameraForward.y = 0f;
		cameraRight.y = 0f;

		cameraForward.Normalize();
		cameraRight.Normalize();

		Vector3 movementDirection =
			cameraRight * joystickInput.x +
			cameraForward * joystickInput.y;

		// No joystick input
		if (movementDirection.sqrMagnitude < 0.01f)
		{
			HandleIdle();
			return;
		}

		if (_isActive)
		{
			movementDirection.Normalize();
			HandleMovement(movementDirection);
		}
	}

	private void HandleMovement(Vector3 movementDirection)
	{
		_animator.SetFloat("speed", 1f);

		// Normalize only for rotation
		Vector3 direction = movementDirection.normalized;

		Quaternion desiredRotation = Quaternion.LookRotation(
			direction,
			Vector3.up
		);

		Quaternion newRotation = Quaternion.RotateTowards(
			_rigidbody.rotation,
			desiredRotation,
			_rotationSpeed * Time.fixedDeltaTime
		);

		_rigidbody.MoveRotation(newRotation);

		// Preserve joystick magnitude for variable movement speed
		Vector3 move = movementDirection *
					   (_isSkating ? _skateSpeed : _moveSpeed) *
					   Time.fixedDeltaTime;

		_rigidbody.MovePosition(_rigidbody.position + move);

		PlayFootstepIfMoved(move.magnitude);
	}

	private void HandleIdle()
	{
		_currentStepTracker = 0f;

		// Face opposite the camera direction while idle
		Vector3 idleDirection;

		if (Camera.main != null)
		{
			idleDirection = -Camera.main.transform.forward;
			idleDirection.y = 0f;

			if (idleDirection.sqrMagnitude < 0.0001f)
			{
				idleDirection = -transform.forward;
			}
			else
			{
				idleDirection.Normalize();
			}
		}
		else
		{
			idleDirection = -transform.forward;
		}

		Quaternion desiredIdleRotation = Quaternion.LookRotation(
			idleDirection,
			Vector3.up
		);

		Quaternion newIdleRotation = Quaternion.RotateTowards(
			_rigidbody.rotation,
			desiredIdleRotation,
			_rotationSpeed * Time.fixedDeltaTime
		);

		_rigidbody.MoveRotation(newIdleRotation);

		_animator.SetFloat("speed", 0f);
	}

	private void PlayFootstepIfMoved(float distanceMoved)
	{
		_currentStepTracker += distanceMoved;

		if (_currentStepTracker >= _stepDistance)
		{
			if (!_footstepEvent.IsNull)
			{
				RuntimeManager.PlayOneShot(
					_footstepEvent,
					transform.position
				);
			}

			_currentStepTracker = 0f;
		}
	}

	private IEnumerator IdleActiveAnimation()
	{
		while (_animator != null)
		{
			_animator.SetTrigger("idle_active");

			yield return new WaitForSeconds(
				Random.Range(7, 15)
			);
		}
	}
}