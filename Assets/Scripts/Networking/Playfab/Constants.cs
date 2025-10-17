using UnityEngine;

namespace Networking.Playfab.Login
{
    public class Constants : MonoBehaviour
    {
        public static Constants Instance { get; private set; }
        
        [SerializeField] public PlayFabSharedSettings Shared;
        [SerializeField] public string SavedUsername;

        public string PlayFabID { get; private set; }
        public string UGSID { get; private set; }

        public void SetIDs(string pfID, string ugsID)
        {
            PlayFabID = pfID;
            UGSID = ugsID;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }
    }
}
