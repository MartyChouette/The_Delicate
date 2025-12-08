using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmotionBank
{
    public enum GameState
    {
        Lobby,
        CountdownToSelection, // "3... 2... 1..."
        Selection,            // "SELECTION" - Players vote now
        CountdownToGameplay,  // "3... 2... 1..."
        Gameplay,             // "BEGIN" - Obstacle Gate opens
        GameOver
    }

    public class GameSessionManager : NetworkBehaviour
    {
        public static GameSessionManager Instance;

        [Header("Scene Config")]
        public EmotionState sceneBox; // The box in the scene
        public Transform boxSpawnPoint;
        public float boxMaxHealth = 100f;

        [Header("Gates")]
        public GameObject lobbyDoor;        // Blocks entry to Selection Room
        public GameObject obstacleCourseGate; // Blocks entry to Obstacle Course

        [Header("Game State")]
        public NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.Lobby);
        public NetworkVariable<float> currentBoxHealth = new NetworkVariable<float>(100f);
        public NetworkVariable<float> countdownTimer = new NetworkVariable<float>(0f);
        public NetworkVariable<bool> didWin = new NetworkVariable<bool>(false);

        private readonly HashSet<ulong> playersInLobbyZone = new HashSet<ulong>();


        private float _voteCheckTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"[SESSION] GameSessionManager spawned on " +
                      $"{(IsServer ? "SERVER/HOST" : "CLIENT")} " +
                      $"(OwnerClientId={OwnerClientId}, LocalClientId={NetworkManager.Singleton.LocalClientId})");

            if (IsServer)
            {
                currentBoxHealth.Value = boxMaxHealth;
                ToggleBoxVisuals(false); // Hide box initially
            }
            // Force visual update on join
            UpdateVisualsForState(currentState.Value);
            currentState.OnValueChanged += (oldState, newState) => UpdateVisualsForState(newState);
        }

        private void Update()
        {
            if (!IsServer) return;

            float dt = Time.deltaTime;

            switch (currentState.Value)
            {
                case GameState.CountdownToSelection:
                    countdownTimer.Value -= dt;
                    if (countdownTimer.Value <= 0f)
                    {
                        // 3-2-1 Done -> Open Lobby Door, Start Selection
                        currentState.Value = GameState.Selection;
                    }
                    break;

                case GameState.Selection:
                    CheckVotes();
                    break;

                case GameState.CountdownToGameplay:
                    countdownTimer.Value -= dt;
                    if (countdownTimer.Value <= 0f)
                    {
                        // 3-2-1 Done -> Open Obstacle Gate, Start Game
                        StartGameplay();
                    }
                    break;
            }
        }

        private void UpdateVisualsForState(GameState state)
        {
            // GATE LOGIC:
            // Lobby Door: Active (Closed) ONLY during Lobby & first countdown. Removes during Selection.
            if (lobbyDoor != null)
            {
                bool isClosed = (state == GameState.Lobby || state == GameState.CountdownToSelection);
                lobbyDoor.SetActive(isClosed);
            }

            // Obstacle Gate: Active (Closed) UNTIL Gameplay starts.
            if (obstacleCourseGate != null)
            {
                bool isClosed = (state != GameState.Gameplay && state != GameState.GameOver);
                obstacleCourseGate.SetActive(isClosed);
            }

            // Box Visuals: Only show in Gameplay/GameOver
            if (state == GameState.Gameplay || state == GameState.GameOver)
            {
                ToggleBoxVisuals(true);
            }
        }

        // --- PUBLIC TRIGGERS ---

        public void StartLobbyCountdown()
        {
            if (!IsServer || currentState.Value != GameState.Lobby) return;
            countdownTimer.Value = 3.0f;
            currentState.Value = GameState.CountdownToSelection;
        }

        public void TriggerWin() { if (IsServer) EndGame(true); }
        public void TriggerLoss() { if (IsServer) EndGame(false); }

        public void TakeDamage(float amount)
        {
            if (!IsServer || currentState.Value != GameState.Gameplay) return;
            currentBoxHealth.Value -= amount;
            if (currentBoxHealth.Value <= 0) TriggerLoss();
        }

        private void CheckVotes()
        {
            _voteCheckTimer += Time.deltaTime;
            if (_voteCheckTimer < 0.5f) return;
            _voteCheckTimer = 0f;

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
                ApplyBoxEmotions(votes);
                countdownTimer.Value = 3.0f;
                currentState.Value = GameState.CountdownToGameplay;
            }
        }

        private void ApplyBoxEmotions(List<EmotionType> votes)
        {
            if (sceneBox == null) return;
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

        private void StartGameplay()
        {
            currentState.Value = GameState.Gameplay;
        }

        private void EndGame(bool won)
        {
            didWin.Value = won;
            currentState.Value = GameState.GameOver;
            if (sceneBox != null)
            {
                var rb = sceneBox.GetComponent<Rigidbody>();
                if (rb) rb.isKinematic = true; // Stop box movement
            }
        }

        private void ToggleBoxVisuals(bool isActive)
        {
            if (sceneBox == null) return;
            foreach (var r in sceneBox.GetComponentsInChildren<Renderer>()) r.enabled = isActive;
            foreach (var c in sceneBox.GetComponentsInChildren<Collider>()) c.enabled = isActive;
            var rb = sceneBox.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = !isActive;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RestartGameServerRpc()
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        [ServerRpc(RequireOwnership = false)]
        public void QuitGameServerRpc()
        {
            Application.Quit();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReportLobbyZoneStateServerRpc(ulong playerId, bool inZone)
        {
            if (!IsServer) return;

            if (inZone)
            {
                if (playersInLobbyZone.Add(playerId))
                {
                    Debug.Log($"[LOBBY][SERVER] Player {playerId} ENTERED lobby zone. Count={playersInLobbyZone.Count}");
                }
            }
            else
            {
                if (playersInLobbyZone.Remove(playerId))
                {
                    Debug.Log($"[LOBBY][SERVER] Player {playerId} EXITED lobby zone. Count={playersInLobbyZone.Count}");
                }
            }

            CheckLobbyReady();
        }

        private void CheckLobbyReady()
        {
            if (!IsServer) return;

            if (currentState.Value != GameState.Lobby)
            {
                Debug.Log($"[LOBBY][SERVER] CheckLobbyReady in state {currentState.Value}, ignoring.");
                return;
            }

            int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            int inZoneCount = playersInLobbyZone.Count;

            Debug.Log($"[LOBBY CHECK][SERVER] In Zone: {inZoneCount} / Required: {connectedCount}");

            if (connectedCount > 0 && inZoneCount >= connectedCount)
            {
                Debug.Log("[LOBBY][SERVER] All players present! Starting lobby countdown...");
                StartLobbyCountdown();
            }
        }


    }
}