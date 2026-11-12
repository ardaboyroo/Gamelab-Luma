using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
	[SerializeField] private InputActionReference switchAction;

	[SerializeField] private string sceneName;
	private string previousSceneName;

	private bool hasSwitched = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		switchAction.action.performed += OnPerform;
		previousSceneName = SceneManager.GetActiveScene().name;
	}

	private void OnPerform(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
			DoSwitchScene();
		}
	}

	public void DoSwitchScene()
	{
		if (!hasSwitched)
		{
			hasSwitched = true;
			SceneManager.LoadScene(sceneName);
		}
		else
		{
			hasSwitched = false;
			SceneManager.LoadScene(previousSceneName);
		}
	}
}
