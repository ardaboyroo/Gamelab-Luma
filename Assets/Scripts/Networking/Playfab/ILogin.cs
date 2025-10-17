using System;
using PlayFab;
using PlayFab.ClientModels;

namespace Networking.Playfab.Login
{
    public interface ILogin {
        void Login(
            GetPlayerCombinedInfoRequestParams loginInfoParams, 
            Action<LoginResult> loginSuccess, 
            Action<PlayFabError> loginFailure, 
            object loginParams);
    }
}
