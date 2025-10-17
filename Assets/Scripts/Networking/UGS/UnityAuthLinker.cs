using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking.UGS
{
    public class UnityAuthLinker : MonoBehaviour
    {
        public async Task InitializeUGSAsync(string playFabId)
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log($"UGS Authenticated. Unity PlayerID: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"Linked PlayFab ID: {playFabId}");
        }
    }

}
