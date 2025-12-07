using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    public enum GameState
    {
        Lobby,
        Selection,
        Gameplay
    }

    public class GameSessionManager : NetworkBehaviour
    {
        public static GameSessionManager Instance;

        [Header("Configuration")]
        public GameObject payloadBoxPrefab;
        public Transform boxSpawnPoint;
        public GameObject lobbyDoor;
        public GameObject obstacleCourseGate;

        public NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.Lobby);

        private float _checkTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            currentState.OnValueChanged += OnStateChanged;
            UpdateVisualsForState(currentState.Value);
        }

        private void OnStateChanged(GameState oldState, GameState newState)
        {
            UpdateVisualsForState(newState);
        }

        private void UpdateVisualsForState(GameState state)
        {
            if (lobbyDoor != null) lobbyDoor.SetActive(state == GameState.Lobby);
            if (obstacleCourseGate != null) obstacleCourseGate.SetActive(state != GameState.Gameplay);
        }

        // ------------------------------------------------
        // Logic Loop (Runs on Host/Owner)
        // ------------------------------------------------

        private void Update()
        {
            // In Distributed, usually Client 0 (Host) manages the flow.
            // Or if purely P2P, we rely on the host to spawn the box.
            if (!IsServer && !IsHost) return;

            if (currentState.Value == GameState.Selection)
            {
                CheckVotes();
            }
        }

        public void StartSelectionPhase()
        {
            // Host triggers this manually (e.g. UI button)
            if (IsServer || IsHost)
            {
                currentState.Value = GameState.Selection;
            }
        }

        private void CheckVotes()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer < 1.0f) return; // Check every second
            _checkTimer = 0f;

            // Find all players
            var players = FindObjectsByType<PlayerVoteState>(FindObjectsSortMode.None);
            if (players.Length == 0) return;

            // Check if everyone has voted
            bool allReady = true;
            List<EmotionType> votes = new List<EmotionType>();

            foreach (var p in players)
            {
                var v = p.GetVote();
                if ((int)v == -1) // -1 means no vote yet
                {
                    allReady = false;
                    break;
                }
                votes.Add(v);
            }

            // Only start if we have at least as many votes as connected clients
            // (Prevents starting if a player is still loading in)
            if (allReady && players.Length >= NetworkManager.Singleton.ConnectedClients.Count)
            {
                StartGameplayPhase(votes);
            }
        }

        private void StartGameplayPhase(List<EmotionType> votes)
        {
            Debug.Log("All players voted! Spawning Box...");
            SpawnPayloadBox(votes);
            currentState.Value = GameState.Gameplay;
        }

        private void SpawnPayloadBox(List<EmotionType> votes)
        {
            if (payloadBoxPrefab == null || boxSpawnPoint == null) return;

            GameObject box = Instantiate(payloadBoxPrefab, boxSpawnPoint.position, boxSpawnPoint.rotation);
            box.GetComponent<NetworkObject>().Spawn();

            EmotionState state = box.GetComponent<EmotionState>();
            if (state != null)
            {
                float fear = 0f, anger = 0f, abandon = 0f, denial = 0f;
                foreach (var v in votes)
                {
                    switch (v)
                    {
                        case EmotionType.Fear: fear += 0.35f; break;
                        case EmotionType.Anger: anger += 0.35f; break;
                        case EmotionType.Abandonment: abandon += 0.35f; break;
                        case EmotionType.Denial: denial += 0.35f; break;
                    }
                }
                state.fearIntensity.Value = Mathf.Clamp01(fear);
                state.angerIntensity.Value = Mathf.Clamp01(anger);
                state.abandonmentIntensity.Value = Mathf.Clamp01(abandon);
                state.denialIntensity.Value = Mathf.Clamp01(denial);
            }
        }
    }
}