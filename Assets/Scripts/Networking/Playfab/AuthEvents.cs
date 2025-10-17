using UnityEngine;
using UnityEngine.UIElements;

namespace Networking.Playfab.Login
{
    public class AuthEvents : MonoBehaviour
    {
        [SerializeField] private Authentification _auth;
        [SerializeField] private UIDocument _doc;

        public TextField Email;
        public TextField Username;
        public TextField Password;
        public TextField RepeatPassword;

        public Button LoginBtn { get; private set; }
        public Button RegisterBtn { get; private set; }
        public Button GoogleLoginBtn { get; private set; }
        public Button ExitBtn { get; private set; }

        private void Awake()
        {
            SwitchToLogin();
        }

        public void SwitchToLogin()
        {
            ClearReferences();

            _doc.rootVisualElement.Q<VisualElement>("Login").style.display = DisplayStyle.Flex;
            _doc.rootVisualElement.Q<VisualElement>("Register").style.display = DisplayStyle.None;

            UpdateReferences(true);
            UpdateEvents(true);
        }

        public void SwitchToRegister()
        {
            ClearReferences();

            _doc.rootVisualElement.Q<VisualElement>("Login").style.display = DisplayStyle.None;
            _doc.rootVisualElement.Q<VisualElement>("Register").style.display = DisplayStyle.Flex;

            UpdateReferences(false);
            UpdateEvents(false);
        }

        private void UpdateReferences(bool login)
        {
            if (login)
            {
                Username        = _doc.rootVisualElement.Q<TextField>("l_uname");
                Password        = _doc.rootVisualElement.Q<TextField>("l_pword");

                LoginBtn        = _doc.rootVisualElement.Q<Button>("l_login");
                RegisterBtn     = _doc.rootVisualElement.Q<Button>("l_register");
                GoogleLoginBtn  = _doc.rootVisualElement.Q<Button>("l_google");
            }
            else
            {
                Email           = _doc.rootVisualElement.Q<TextField>("r_email");
                Username        = _doc.rootVisualElement.Q<TextField>("r_uname");
                Password        = _doc.rootVisualElement.Q<TextField>("r_pword");
                RepeatPassword  = _doc.rootVisualElement.Q<TextField>("r_rpword");

                LoginBtn        = _doc.rootVisualElement.Q<Button>("r_login");
                RegisterBtn     = _doc.rootVisualElement.Q<Button>("r_register");
                GoogleLoginBtn  = _doc.rootVisualElement.Q<Button>("r_google");
            }

            //ExitBtn = _doc.rootVisualElement.Q<Button>("!r_exit");
        }

        private void UpdateEvents(bool login)
        {
            LoginBtn.clicked    -= _auth.Login;
            LoginBtn.clicked    -= SwitchToLogin;
            RegisterBtn.clicked -= _auth.Register;
            RegisterBtn.clicked -= SwitchToRegister;
            //ExitBtn.clicked     -= _auth.Exit;

            if (login)
            {
                LoginBtn.clicked    += _auth.Login;
                RegisterBtn.clicked += SwitchToRegister;
            }
            else
            {
                LoginBtn.clicked    += SwitchToLogin;
                RegisterBtn.clicked += _auth.Register;
            }

            //GoogleLoginBtn.onClick.AddListener(_auth.GoogleAuth);
            //ExitBtn.clicked += _auth.Exit;
        }

        private void ClearReferences()
        {
            Email = Username = Password = RepeatPassword = null;
            LoginBtn = RegisterBtn = GoogleLoginBtn = ExitBtn = null;
        }

    }
}