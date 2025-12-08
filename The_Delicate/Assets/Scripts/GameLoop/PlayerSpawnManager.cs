using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// Holds a list of transform positions where players should start.
    /// Assigns them based on their ClientID (0, 1, 2...).
    /// </summary>
    public class PlayerSpawnManager : MonoBehaviour
    {
        public static PlayerSpawnManager Instance;

        [Header("Spawn Locations")]
        [Tooltip("Drag empty GameObjects here. Element 0 = Player 1, Element 1 = Player 2, etc.")]
        public Transform[] spawnPoints;

        private void Awake()
        {
            // Simple Singleton so players can find this easily
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
        }

        public Transform GetSpawnPoint(ulong clientId)
        {
            // 1. Check if we are hitting the fallback
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning($"[SpawnManager] Array Empty! Using Manager Position: {transform.position}");
                return transform;
            }

            int index = (int)(clientId % (ulong)spawnPoints.Length);
            Transform point = spawnPoints[index];

            // 2. Check if the point itself is valid
            if (point == null)
            {
                Debug.LogError($"[SpawnManager] Point at index {index} is Missing/Null! Using Manager Position.");
                return transform;
            }

            Debug.Log($"[SpawnManager] Spawning Player {clientId} at Point {index}: '{point.name}' ({point.position})");
            return point;
        }
    }
}