using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    public class OutOfBoundsRespawn : NetworkBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            // 1. Is it the Box? -> LOSE
            if (other.GetComponent<BoxHealth>() != null || other.GetComponentInParent<BoxHealth>() != null)
            {
                GameSessionManager.Instance.TriggerLoss();
                return;
            }

            // 2. Is it a Player? -> RESPAWN
            var player = other.GetComponentInParent<PlayerAvatar>();
            if (player != null)
            {
                // Find spawn point
                Transform spawn = PlayerSpawnManager.Instance.GetSpawnPoint(player.OwnerClientId);

                // Teleport via CharacterController or Transform
                player.transform.position = spawn.position;
                player.rb.linearVelocity = Vector3.zero; // Stop falling

                // Sync Transform
                var netTransform = player.GetComponent<Unity.Netcode.Components.NetworkTransform>();
                if (netTransform != null) netTransform.Teleport(spawn.position, spawn.rotation, Vector3.one);
            }
        }
    }
}