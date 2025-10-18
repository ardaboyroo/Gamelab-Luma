using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private TMP_Text chatBox;

    private void Start()
    {
        sendButton.onClick.AddListener(() =>
        {
            if (!IsClient || !NetworkObject.IsSpawned)
            {
                Debug.LogWarning("Cannot send yet.");
                return;
            }

            var msg = inputField.text.Trim();
            if (!string.IsNullOrEmpty(msg))
                SendMessageServerRpc(msg);
        });
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string message, ServerRpcParams rpc = default)
    {
        var sender = rpc.Receive.SenderClientId;
        var full = $"[{sender}] {message}";
        BroadcastMessageClientRpc(full);
    }

    [ClientRpc]
    private void BroadcastMessageClientRpc(string message)
    {
        chatBox.text += message + "\n";
    }
}