using UnityEngine;
using UnityEngine.UIElements;

public class PortalClientUI : MonoBehaviour
{
    private static PortalClientUI _instance;
    private VisualElement _root;
    private Label _title;
    private Label _desc;
    private Button _soloBtn, _partyBtn, _fullBtn;
    private NetworkRoomPortal _currentPortal;
    private string _roomName;

    private void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;

        _root = GetComponent<UIDocument>().rootVisualElement;
        _title = _root.Q<Label>("ActivityTitle");
        _desc = _root.Q<Label>("ActivityDescription");
        _soloBtn = _root.Q<Button>("SoloButton");
        _partyBtn = _root.Q<Button>("PartyButton");
        _fullBtn = _root.Q<Button>("FullPartyButton");
        _root.visible = false;

        _soloBtn.clicked += () => SelectMode(0);
        _partyBtn.clicked += () => SelectMode(1);
        _fullBtn.clicked += () => SelectMode(2);
    }

    public static void Show(NetworkRoomPortal portal, string roomName, string desc, bool allowSingle, bool allowParty, bool allowFull)
    {
        _instance._roomName = roomName;
        _instance._currentPortal = portal;
        _instance._root.visible = true;
        _instance._root.style.display = DisplayStyle.Flex;
        _instance._title.text = $"Enter {roomName}";
        _instance._desc.text = desc;
        _instance._soloBtn.visible = allowSingle;
        _instance._partyBtn.visible = allowParty;
        _instance._fullBtn.visible = allowFull;
    }

    private void SelectMode(byte mode)
    {
        _root.visible = false;
        _root.style.display = DisplayStyle.None;
        if (_currentPortal == null) return;

        switch (mode)
        {
            case 0: _currentPortal.RequestEnterServerRpc((byte)NetworkRoomPortal.EntryMode.Single); break;
            case 1: _currentPortal.RequestEnterServerRpc((byte)NetworkRoomPortal.EntryMode.Party); break;
            case 2: _currentPortal.RequestEnterServerRpc((byte)NetworkRoomPortal.EntryMode.Full); break;
        }
    }
}