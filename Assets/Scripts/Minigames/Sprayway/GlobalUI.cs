using UnityEngine.UIElements;
using System.Collections.Generic;
using Networking.Playfab.Database;
using Unity.Netcode;
using UserModels.Display;


public class GlobalUI : NetworkBehaviour
{
    private static GlobalUI _instance;
    private VisualElement _root;

    private Button _back, _characteredit, _profile;
    private VisualElement _profileview;

    private void Awake()
    {
        _instance = this;
        _root = GetComponent<UIDocument>().rootVisualElement;

        _profileview = _root.Q<VisualElement>("profileview");
        _profile = _root.Q<Button>("profile");
        _back = _root.Q<Button>("back");
        _characteredit = _root.Q<Button>("characteredit");

        _profile.clicked += () => ShowProfile();
        _back.clicked += () => HideProfile();
        _characteredit.clicked += () => ToCharacterEdit();
    }

    private void ToCharacterEdit()
    {
        GlobalController.Instance.GoToCharacterEditServerRpc(NetworkManager.Singleton.LocalClientId);
        HideProfile();
        Hide();
    }

    public void ShowProfile()
    {
        _profileview.style.display = DisplayStyle.Flex;
    }

    public void HideProfile()
    {
        _profileview.style.display = DisplayStyle.None;
    }

    public static void Hide() => _instance._root.visible = false;
    public static void Show() => _instance._root.visible = true;
}