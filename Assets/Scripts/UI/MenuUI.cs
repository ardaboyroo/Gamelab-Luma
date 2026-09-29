using UnityEngine;

public class MenuUI : MonoBehaviour
{
    //temporary
    [SerializeField]
    private PlayerStateManager playerStateManager;

    void Start()
    {
        
    }

    private void OnEnable()
    {
        playerStateManager.StateChangedEvent += OnStateChanged;
    }

    private void OnDisable()
    {
        playerStateManager.StateChangedEvent -= OnStateChanged;
    }

    void Update()
    {
        
    }

    //temporary
    public void ShowMenuUI()
    {
        playerStateManager.ChangePlayerState(PlayerStates.MenuState);
    }

    //temporary
    public void HideMenuUI()
    {
        playerStateManager.ChangePlayerState(PlayerStates.StandardState);
    }

    private void OnStateChanged(PlayerStates state)
    {

    }
}
