using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Networking.Playfab.Login {
    public class StandardLogin : ILogin {
        public class StandardLoginParams {
            public string username;
            public string password;

            public StandardLoginParams(string username, string password) {
                this.username = username;
                this.password = password;
            }
        }
        
        public void Login(GetPlayerCombinedInfoRequestParams loginInfoParams, Action<LoginResult> loginSuccess, Action<PlayFabError> loginFailure, object loginParams) {
            StandardLoginParams emailLoginParams = loginParams as StandardLoginParams;
            if (emailLoginParams == null) {
                loginFailure.Invoke(new PlayFabError());
                Debug.LogError("Login Parameter is null");

                return;
            }
            
            var request = new LoginWithPlayFabRequest {
                TitleId = Constants.Instance.Shared.TitleId,
                Password = emailLoginParams.password,
                Username = emailLoginParams.username,
                InfoRequestParameters = loginInfoParams,
            };

            PlayFabClientAPI.LoginWithPlayFab(request, loginSuccess, loginFailure);
        }
    }
}