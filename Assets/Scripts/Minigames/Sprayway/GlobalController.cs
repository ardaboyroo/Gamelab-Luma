using UnityEngine;
using Unity.Netcode;
using Networking.Playfab.Database;
using UserModels.Display;
using System.Threading.Tasks;

public class GlobalController : NetworkBehaviour 
{
    public static GlobalController Instance;

    private void Awake()
    {
        Instance = this;
    }



    [ServerRpc(RequireOwnership = false)]
    public void GoToCharacterEditServerRpc(ulong clientId)
    {
        Debug.Log("Going to character edit " + clientId);
        NetworkRoomPortal.ForceAssign(clientId, "character_edit", 0);
    }

    [ServerRpc(RequireOwnership = false)]
    public void GoToHubServerRpc(ulong clientId)
    {
        Debug.Log("Going to character hub " + clientId);
        NetworkRoomPortal.ForceAssign(clientId, "hub", 0);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SaveAvatarServerRpc(AvatarData data, string playfabID, ulong clientId)
    {
        AvatarStorage.SaveAvatar(data, playfabID);
        NetworkRoomPortal.ForceAssign(clientId, "hub", 0);
    }

}
