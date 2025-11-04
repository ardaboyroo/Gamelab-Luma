using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Networking.Playfab.Database;
using UserModels.Display;
using Rooms.CharacterEdit;
using Unity.Netcode;

namespace Rooms.CharacterEdit
{
    public class CharacterEditUI : MonoBehaviour
    {
        private static CharacterEditUI _instance;
        private VisualElement _root;

        private SliderInt _eyes, _eyecolor, _brows, _nose, _mouth, _hair, _haircolor;
        private Button _finalize;

        private CharacterEditManager _manager;

        private bool _initialized;

        private const byte 
            _eyeMax = 1, 
            _eyeColorMax = 1, 
            _browsMax = 1, 
            _noseMax = 6, 
            _mouthMax = 1, 
            _hairMax = 3,
            _hairColorMax = 1;

        private void Start()
        {
            _instance = this;
            _root = GetComponent<UIDocument>().rootVisualElement;

            _eyes = _root.Q<SliderInt>("eyes");
            _eyecolor = _root.Q<SliderInt>("eyecolor");
            _brows = _root.Q<SliderInt>("brows");
            _nose = _root.Q<SliderInt>("nose");
            _mouth = _root.Q<SliderInt>("mouth");
            _hair = _root.Q<SliderInt>("hair");
            _haircolor = _root.Q<SliderInt>("haircolor");
            _finalize = _root.Q<Button>("finalize");

            RegisterSliderCallbacks();

            _root.schedule.Execute(SetupSliderLimits).StartingIn(0);

            _root.visible = false;
        }
        private void SetupSliderLimits()
        {
            _eyes.lowValue = _eyecolor.lowValue = _brows.lowValue = _nose.lowValue =
                _mouth.lowValue = _hair.lowValue = _haircolor.lowValue = 0;

            _eyes.highValue = _eyeMax;
            _eyecolor.highValue = _eyeColorMax;
            _brows.highValue = _browsMax;
            _nose.highValue = _noseMax;
            _mouth.highValue = _mouthMax;
            _hair.highValue = _hairMax;
            _haircolor.highValue = _hairColorMax;
        }

        private void RegisterSliderCallbacks()
        {
            var sliders = new List<SliderInt> { _eyes, _eyecolor, _brows, _nose, _mouth, _hair, _haircolor };
            foreach (var s in sliders)
                s?.RegisterValueChangedCallback(evt => OnAnyValueChanged());
        }

        public static void Initialize(CharacterEditManager manager)
        {
            if (_instance == null)
                return;

            if (_instance._manager != null)
                _instance._finalize.clicked -= _instance.OnFinalizeClicked;

            _instance._manager = manager;
            _instance._finalize.clicked += _instance.OnFinalizeClicked;
        }

        private void OnFinalizeClicked()
        {
            var data = BuildAvatarData();

            GlobalController.Instance.SaveAvatarServerRpc(data, Networking.Playfab.Login.Constants.Instance.PlayFabID, NetworkManager.Singleton.LocalClientId);
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerDisplay>().RequestApplyAvatar(data);

            Debug.Log("[CharacterEditUI] Avatar finalized and saved.");
            Debug.Log(data.ToString());
            Hide();
            GlobalUI.Show();
        }

        private void OnAnyValueChanged()
        {
            if (!_initialized) return;

            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerDisplay>().Preview(BuildAvatarData());
        }

        private AvatarData BuildAvatarData() => new AvatarData
        {
            EyesID = _eyes.value,
            EyeColorID = _eyecolor.value,
            BrowsID = _brows.value,
            NoseID = _nose.value,
            MouthID = _mouth.value,
            HairID = _hair.value,
            HairColorID = _haircolor.value,
            Initialized = true
        };

        public static void Hide() => _instance._root.visible = false;
        public static void Show()
        {
            if (_instance == null)
                return;

            _instance._root.visible = true;
            _instance.StartCoroutine(_instance.DelayedShowRoutine());
        }

        private System.Collections.IEnumerator DelayedShowRoutine()
        {
            yield return null; 

            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
                yield break;

            var avatar = PlayerDisplay.GetAvatar();
            Debug.Log("[CharacterEditUI] Loaded avatar from network: " + avatar);

            if (avatar.Initialized)
            {
                _initialized = false; // temporarily disable live preview during setup

                _eyes.value = Mathf.Clamp(avatar.EyesID, 0, _eyeMax);
                _eyecolor.value = Mathf.Clamp(avatar.EyeColorID, 0, _eyeColorMax);
                _brows.value = Mathf.Clamp(avatar.BrowsID, 0, _browsMax);
                _nose.value = Mathf.Clamp(avatar.NoseID, 0, _noseMax);
                _mouth.value = Mathf.Clamp(avatar.MouthID, 0, _mouthMax);
                _hair.value = Mathf.Clamp(avatar.HairID, 0, _hairMax);
                _haircolor.value = Mathf.Clamp(avatar.HairColorID, 0, _hairColorMax);

                NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerDisplay>().Preview(avatar);
                _initialized = true;
            }
            else
            {
                _initialized = true;
                Debug.Log("[CharacterEditUI] Avatar not initialized yet, using defaults.");
            }
        }
    }
}

