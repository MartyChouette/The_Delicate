using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

namespace EmotionBank
{
    public class SimpleVivoxManager : MonoBehaviour
    {
        public static SimpleVivoxManager Instance;

        [Header("Settings")]
        public string channelName = "GlobalEmotionLobby";

        [Header("Proximity Settings")]
        public int chatRadius = 15;
        public Transform playerTransform;

        private bool _isVivoxReady = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;

            // Don't destroy this manager when loading scenes
            DontDestroyOnLoad(gameObject);
        }

        async void Start()
        {
            // 1. Initialize Unity Services (Shared by Relay & Vivox)
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            // We do NOT login here anymore. We just make sure Services are ready.
            Debug.Log("[VivoxManager] Services Initialized. Waiting for player to join game...");
        }

        // --- CALL THIS WHEN PLAYER SPAWNS ---
        public async void JoinGameVoice()
        {
            // 1. Ensure we are signed in (Relay likely handled this already!)
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                catch (AuthenticationException)
                {
                    // Relay beat us to it. This is fine.
                    Debug.LogWarning("[Vivox] Auth handled by Relay/NetworkManager.");
                }
            }

            // 2. Initialize Vivox (if not already)
            await VivoxService.Instance.InitializeAsync();

            // 3. Login to Vivox
            if (!VivoxService.Instance.IsLoggedIn)
            {
                LoginOptions options = new LoginOptions
                {
                    DisplayName = "Player_" + UnityEngine.Random.Range(1000, 9999),
                    ParticipantUpdateFrequency = ParticipantPropertyUpdateFrequency.FivePerSecond
                };
                await VivoxService.Instance.LoginAsync(options);
            }

            // 4. Join the 3D Channel
            Channel3DProperties props = new Channel3DProperties(chatRadius, 1, 1.0f, AudioFadeModel.InverseByDistance);
            await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, props);

            Debug.Log($"[Vivox] SUCCESS: Joined Channel {channelName}");
            VivoxService.Instance.UnmuteInputDevice();

            _isVivoxReady = true;
        }

        void Update()
        {
            if (!_isVivoxReady || VivoxService.Instance == null || playerTransform == null) return;
            if (VivoxService.Instance.ActiveChannels.Count == 0) return;

            // FIX: Using the correct Unity 6 signature (GameObject, ChannelName, Active)
            string firstChannel = VivoxService.Instance.ActiveChannels.Keys.FirstOrDefault();
            if (!string.IsNullOrEmpty(firstChannel))
            {
                VivoxService.Instance.Set3DPosition(playerTransform.gameObject, firstChannel, true);
            }
        }

        public void SetLocalPlayer(Transform t)
        {
            playerTransform = t;
        }
    }
}