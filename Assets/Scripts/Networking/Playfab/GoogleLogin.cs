using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Networking.Playfab.Login
{
    public class GoogleLogin : ILogin {
        public void Login(GetPlayerCombinedInfoRequestParams loginInfoParams, Action<LoginResult> loginSuccess,
            Action<PlayFabError> loginFailure, object loginParams) 
        {
        }
    }
}