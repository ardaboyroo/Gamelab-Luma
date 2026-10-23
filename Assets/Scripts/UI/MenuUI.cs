using UnityEngine;
using UnityEngine.UIElements;

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
        playerStateManager.OnStateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        playerStateManager.OnStateChanged -= OnStateChanged;
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
