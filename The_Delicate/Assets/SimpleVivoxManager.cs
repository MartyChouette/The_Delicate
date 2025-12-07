using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

namespace EmotionBank
{
    public class SimpleVivoxManager : MonoBehaviour
    {
        [Header("Settings")]
        public string channelName = "GlobalEmotionLobby";

        public static SimpleVivoxManager Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
        }

        public void SetMute(bool isMuted)
        {
            if (VivoxService.Instance == null) return;

            if (isMuted)
                VivoxService.Instance.MuteInputDevice();
            else
                VivoxService.Instance.UnmuteInputDevice();

            Debug.Log($"[Vivox] Mic Muted: {isMuted}");
        }

        async void Start()
        {
            // 1. Initialize Unity Services
            await UnityServices.InitializeAsync();

            // 2. Sign in Anonymously
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // 3. Initialize Vivox
            await VivoxService.Instance.InitializeAsync();

            // --- FIX: SUBSCRIBE HERE (After Initialization) ---
            VivoxService.Instance.LoggedIn += OnLoggedIn;
            VivoxService.Instance.LoggedOut += OnLoggedOut;

            // 4. Log in to Vivox
            LoginOptions options = new LoginOptions
            {
                DisplayName = "Player_" + UnityEngine.Random.Range(1000, 9999),
                ParticipantUpdateFrequency = ParticipantPropertyUpdateFrequency.FivePerSecond
            };
            await VivoxService.Instance.LoginAsync(options);

            Debug.Log("Vivox Logged In");

            // 5. Join the Channel
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);

            Debug.Log($"Joined Channel: {channelName}");

            // 6. Mute immediately
            VivoxService.Instance.MuteInputDevice();
            Debug.Log("Mic Muted (Waiting for Hand Grab)");
        }

        // Use OnDestroy to clean up since we subscribed in Start
        private void OnDestroy()
        {
            // Check if Instance exists before trying to unsubscribe to avoid errors on shutdown
            if (VivoxService.Instance != null)
            {
                VivoxService.Instance.LoggedIn -= OnLoggedIn;
                VivoxService.Instance.LoggedOut -= OnLoggedOut;
            }
        }

        // Removed OnEnable/OnDisable to prevent the race condition error
        private void OnLoggedIn() { Debug.Log("Vivox: User Logged In"); }
        private void OnLoggedOut() { Debug.Log("Vivox: User Logged Out"); }
    }
}