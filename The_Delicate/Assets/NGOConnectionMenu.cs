using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro; // if you're using TextMeshPro for the inputs

namespace EmotionBank
{
    /// <summary>
    /// Simple Netcode-for-GO host/client menu that reuses the existing
    /// player name + session name input fields.
    /// Wire your existing buttons to OnHostClicked / OnJoinClicked.
    /// </summary>
    public class NGOConnectionMenu : MonoBehaviour
    {
        [Header("UI")]
        public TMP_InputField sessionNameInput; // optional for now
        public TMP_InputField playerNameInput;  // optional, for your own use

        [Header("Scene Config")]
        [Tooltip("Gameplay scene that contains GameSessionManager, LobbyStartZone, etc.")]
        public string gameplaySceneName = "EmotionGameplay"; // <--- change to your scene name

        private bool _networkStarted;

        private void Awake()
        {
            // Optional: keep menu alive across scene loads
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Called by your existing "Create / Host" button.
        /// </summary>
        public void OnHostClicked()
        {
            if (_networkStarted)
                return;

            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                Debug.LogError("[NGO MENU] No NetworkManager.Singleton in the scene!");
                return;
            }

            bool started = nm.StartHost();
            if (!started)
            {
                Debug.LogError("[NGO MENU] Failed to StartHost.");
                return;
            }

            _networkStarted = true;
            Debug.Log("[NGO MENU] Started HOST. IsServer=" + nm.IsServer + " IsClient=" + nm.IsClient);

            // IMPORTANT: no LoadScene call here, because we already ARE in the gameplay scene.
            // Just hide the menu UI however you like.
        }

        public void OnJoinClicked()
        {
            if (_networkStarted)
                return;

            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                Debug.LogError("[NGO MENU] No NetworkManager.Singleton in the scene!");
                return;
            }

            bool started = nm.StartClient();
            if (!started)
            {
                Debug.LogError("[NGO MENU] Failed to StartClient.");
                return;
            }

            _networkStarted = true;
            Debug.Log("[NGO MENU] Started CLIENT. IsServer=" + nm.IsServer + " IsClient=" + nm.IsClient);

            // Again, no scene load. The host and client both start in this scene.
        }


    }
}
