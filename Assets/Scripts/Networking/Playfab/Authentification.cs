using Networking.UGS;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking.Playfab.Login
{

    public class Authentification : MonoBehaviour 
    {
        [SerializeField] private AuthEvents _display;
        [SerializeField] private UnityAuthLinker _unityAuthLinker;

        private async void Awake() => await UnityServices.InitializeAsync();

        public void Start() {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            _display.Username.value = PlayerPrefs.GetString("SAVED USERNAME", "");

            if (PlayFabClientAPI.IsClientLoggedIn()) {
                Debug.LogWarning("User is already logged in");
            }
        }

        private void Login(ILogin loginMethod, object loginParams) {
            loginMethod.Login(_loginInfoParams, OnLoginSuccess, OnLoginFailure, loginParams);

            //loginInProgress.SetActive(true);
        }

        #region Standard Login

        public void Login() {
            if (ValidateLoginData()) {
                Login(new StandardLogin(), new StandardLogin.StandardLoginParams(_display.Username.value, _display.Password.value));
            }
        }

        private bool ValidateLoginData() {
            //Validating data
            string errorMessage = "";

            if (_display.Username.value.Length < 3) {
                errorMessage = "Username must be at least 3 characters";
            } else if (_display.Password.value.Length < 8) {
                errorMessage = "Password must be at least 8 characters";
            }

            if (errorMessage.Length > 0) {
                Debug.LogError(errorMessage);
                return false;
            }

            return true;
        }

        #endregion

        #region Google Login
        #endregion

        #region Google Register
        #endregion

        #region Standard Register

        public void Register() {
            if (!ValidateRegisterData()) return;

            var request = new RegisterPlayFabUserRequest {
                TitleId = Constants.Instance.Shared.TitleId,
                Email = _display.Email.value,
                Password = _display.Password.value,
                Username = _display.Username.value,
                InfoRequestParameters = _loginInfoParams,
            };

            PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnLoginFailure);

            //loginInProgress.SetActive(true);
        }

        bool ValidateRegisterData() {
            //Validating data
            string errorMessage = "";

            if (!_display.Email.value.Contains("@")) {
                errorMessage = "E-mail is not valid";
            } else if (_display.Email.value.Length < 5) {
                errorMessage = "E-mail is not valid";
            } else if (_display.Username.value.Length < 3) {
                errorMessage = "Username must be at least 3 characters";
            } else if (_display.Password.value.Length < 8) {
                errorMessage = "Password must be at least 8 characters";
            } else if (!_display.Password.value.Equals(_display.RepeatPassword.value)) {
                errorMessage = "Password doesn't match Repeat password";
            }

            if (errorMessage.Length > 0) {
                Debug.LogError(errorMessage);
                return false;
            }

            return true;
        }

        private async void OnRegisterSuccess(RegisterPlayFabUserResult result) {
            Debug.Log("Register Success!");

            PlayerPrefs.SetString("USERNAME", _display.Username.value);
            PlayerPrefs.SetString("PASSWORD", _display.Password.value);

            Debug.Log(result.PlayFabId);
            Debug.Log(result.Username);
            //loginInProgress.SetActive(false);

            await _unityAuthLinker.InitializeUGSAsync(result.PlayFabId);

            _display.SwitchToLogin();
        }

        #endregion

        private async void OnLoginSuccess(LoginResult result) {
            Debug.Log("PlayFab Login Success!");

            PlayerPrefs.SetString("SAVED USERNAME", _display.Username.value);

            //loginInProgress.SetActive(false);

            await _unityAuthLinker.InitializeUGSAsync(result.PlayFabId);
            Constants.Instance.SetIDs(result.PlayFabId, AuthenticationService.Instance.PlayerId);
            PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> {{ "UnityPlayerId", AuthenticationService.Instance.PlayerId }}
            }, result =>
            {
                Debug.Log("Synced Unity Player ID with PlayFab.");
                SceneManager.LoadSceneAsync(1);

            }, Debug.LogError);
        }

        private readonly GetPlayerCombinedInfoRequestParams _loginInfoParams =
            new GetPlayerCombinedInfoRequestParams {
                GetUserAccountInfo = true,
                GetUserData = true,
                GetUserInventory = true,
                GetUserVirtualCurrency = true,
                GetUserReadOnlyData = true
            };

        private void OnLoginFailure(PlayFabError error) {
            Debug.LogError("Login failure: " + error.Error + "  " + error.ErrorDetails + error + "  " +
                           error.ApiEndpoint + "  " + error.ErrorMessage);
            //loginInProgress.SetActive(false);
        }

        public void Exit() {
            Application.Quit();
        }
    }
}