using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    public class LobbyTriggerZone : NetworkBehaviour
    {
        private List<ulong> playersInZone = new List<ulong>();

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            // Check if player
            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.IsPlayerObject)
            {
                if (!playersInZone.Contains(netObj.OwnerClientId))
                {
                    playersInZone.Add(netObj.OwnerClientId);
                    CheckReady();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer) return;
            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj != null && playersInZone.Contains(netObj.OwnerClientId))
                playersInZone.Remove(netObj.OwnerClientId);
        }

        private void CheckReady()
        {
            // Start if all connected players are in the zone
            int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (playersInZone.Count >= connectedCount && connectedCount > 0)
            {
                GameSessionManager.Instance.StartLobbyCountdown();
            }
        }
    }
}