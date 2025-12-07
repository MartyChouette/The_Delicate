using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// Attach to Player Prefab.
    /// Moves the player to their assigned spawn point immediately upon connection.
    /// </summary>
    public class PlayerInitialSpawn : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            // In Distributed Authority, only the OWNER moves themselves.
            // We don't want to move other people's avatars on our screen.
            if (!IsOwner) return;

            if (PlayerSpawnManager.Instance != null)
            {
                // Get assigned point based on our ID
                Transform point = PlayerSpawnManager.Instance.GetSpawnPoint(OwnerClientId);

                // Teleport
                transform.position = point.position;
                transform.rotation = point.rotation;

                // Kill any physics momentum from the spawn process
                if (TryGetComponent(out Rigidbody rb))
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // Sync this new position to everyone else immediately
                // (Forces the NetworkTransform to update)
                if (TryGetComponent(out NetworkTransform netTransform))
                {
                    netTransform.Teleport(transform.position, transform.rotation, transform.localScale);
                }

                Debug.Log($"[Spawn] Moved local player {OwnerClientId} to {point.name}");
            }
            else
            {
                Debug.LogWarning("No PlayerSpawnManager found in scene!");
            }
        }
    }
}