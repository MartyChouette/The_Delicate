using System;
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
        public int chatRadius = 15; // How far you can hear (meters)
        public Transform playerTransform; // Assign local player here after spawn

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
        }

        async void Start()
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            await VivoxService.Instance.InitializeAsync();

            // Login
            LoginOptions options = new LoginOptions
            {
                DisplayName = "Player_" + UnityEngine.Random.Range(1000, 9999),
                ParticipantUpdateFrequency = ParticipantPropertyUpdateFrequency.FivePerSecond
            };
            await VivoxService.Instance.LoginAsync(options);

            // CHANGED: Join a POSITIONAL (3D) Channel
            Channel3DProperties props = new Channel3DProperties(chatRadius, 1, 1.0f, AudioFadeModel.InverseByDistance);
            await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, props);

            Debug.Log($"Joined Proximity Channel: {channelName}");

            // CHANGED: Unmute immediately so we can talk freely
            VivoxService.Instance.UnmuteInputDevice();
        }

        private void Update()
        {
            // PROXIMITY LOGIC: Update our position every frame
            if (VivoxService.Instance.IsLoggedIn && playerTransform != null)
            {
                // FIXED: Added the required 'channelName' parameter to the Set3DPosition method call
                VivoxService.Instance.Set3DPosition(playerTransform.position, playerTransform.position, playerTransform.forward, playerTransform.up, channelName, true);
            }
        }

        // Helper to let the player register themselves when they spawn
        public void SetLocalPlayer(Transform t)
        {
            playerTransform = t;
        }
    }
}