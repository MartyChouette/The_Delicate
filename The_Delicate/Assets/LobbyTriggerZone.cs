using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    [RequireComponent(typeof(Collider))]
    public class LobbyTriggerZone : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsPlayerObject)
                return;

            // Only the local player on each client should report its own entry.
            if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                return;

            Debug.Log($"[LOBBY] LOCAL player {netObj.OwnerClientId} ENTERED zone, sending RPC to GameSessionManager.");

            if (GameSessionManager.Instance != null && GameSessionManager.Instance.IsSpawned)
            {
                GameSessionManager.Instance.ReportLobbyZoneStateServerRpc(netObj.OwnerClientId, true);
            }
            else
            {
                Debug.LogWarning("[LOBBY] GameSessionManager.Instance is null or not spawned.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsPlayerObject)
                return;

            if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                return;

            Debug.Log($"[LOBBY] LOCAL player {netObj.OwnerClientId} EXITED zone, sending RPC to GameSessionManager.");

            if (GameSessionManager.Instance != null && GameSessionManager.Instance.IsSpawned)
            {
                GameSessionManager.Instance.ReportLobbyZoneStateServerRpc(netObj.OwnerClientId, false);
            }
        }
    }
}