using System.Collections.Generic;
using PlayFab;
using UnityEngine;
using UserModels.Display;
using PlayFab.ServerModels;
using System.Threading.Tasks;

namespace Networking.Playfab.Database
{
    public static class AvatarStorage
    {
        public static async void SaveAvatar(AvatarData avatar, string playfabId)
        {
#if ENABLE_PLAYFABSERVER_API
            avatar.Initialized = true;

            var json = JsonUtility.ToJson(avatar);

            await PlayFabWrapperAPI.UpdateUserDataAsync(new UpdateUserDataRequest
            {
                PlayFabId = playfabId,
                Data = new Dictionary<string, string> { { "AvatarData", json } }
            });
#endif
        }
    }
}