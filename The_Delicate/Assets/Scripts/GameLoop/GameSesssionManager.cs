using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    public enum GameState { Lobby, Selection, Gameplay }

    public class GameSessionManager : NetworkBehaviour
    {
        public static GameSessionManager Instance;

        [Header("Scene References")]
        [Tooltip("Drag the Box that is ALREADY in the scene here.")]
        public EmotionState sceneBox; // <--- CHANGED: Reference existing object
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

            // Ensure the box starts hidden/inactive if we are in the lobby
            if (IsServer && sceneBox != null)
            {
                // We disable the visual/physics but keep the NetworkObject active 
                // so we don't break the connection.
                sceneBox.gameObject.SetActive(true);
                ToggleBoxVisuals(false);
            }
        }

        private void OnStateChanged(GameState oldState, GameState newState)
        {
            UpdateVisualsForState(newState);
        }

        private void UpdateVisualsForState(GameState state)
        {
            if (lobbyDoor != null) lobbyDoor.SetActive(state == GameState.Lobby);
            if (obstacleCourseGate != null) obstacleCourseGate.SetActive(state != GameState.Gameplay);

            // If gameplay started, show the box
            if (state == GameState.Gameplay && sceneBox != null)
            {
                ToggleBoxVisuals(true);
            }
        }

        private void ToggleBoxVisuals(bool isActive)
        {
            if (sceneBox == null) return;

            // Toggle renderers and colliders so it's "invisible" until needed
            foreach (var r in sceneBox.GetComponentsInChildren<Renderer>()) r.enabled = isActive;
            foreach (var c in sceneBox.GetComponentsInChildren<Collider>()) c.enabled = isActive;

            var rb = sceneBox.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = !isActive;
        }

        private void Update()
        {
            if (!IsServer && !IsHost) return;
            if (currentState.Value == GameState.Selection) CheckVotes();
        }

        public void StartSelectionPhase()
        {
            if (IsServer || IsHost) currentState.Value = GameState.Selection;
        }

        private void CheckVotes()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer < 1.0f) return;
            _checkTimer = 0f;

            var players = FindObjectsByType<PlayerVoteState>(FindObjectsSortMode.None);
            if (players.Length == 0) return;

            bool allReady = true;
            List<EmotionType> votes = new List<EmotionType>();

            foreach (var p in players)
            {
                var v = p.GetVote();
                if ((int)v == -1) { allReady = false; break; }
                votes.Add(v);
            }

            if (allReady && players.Length >= NetworkManager.Singleton.ConnectedClients.Count)
            {
                StartGameplayPhase(votes);
            }
        }

        private void StartGameplayPhase(List<EmotionType> votes)
        {
            Debug.Log("All players voted! Activating Box...");
            ApplyBoxEmotions(votes);
            currentState.Value = GameState.Gameplay;
        }

        private void ApplyBoxEmotions(List<EmotionType> votes)
        {
            if (sceneBox == null) return;

            // Since it's a Scene Object, the Server ALWAYS owns it.
            // We don't need SpawnWithOwnership or IsOwner checks anymore.
            // We can just write directly.

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

            sceneBox.fearIntensity.Value = Mathf.Clamp01(fear);
            sceneBox.angerIntensity.Value = Mathf.Clamp01(anger);
            sceneBox.abandonmentIntensity.Value = Mathf.Clamp01(abandon);
            sceneBox.denialIntensity.Value = Mathf.Clamp01(denial);
        }
    }
}