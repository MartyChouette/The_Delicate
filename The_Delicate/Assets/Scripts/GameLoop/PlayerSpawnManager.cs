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
            if (spawnPoints == null || spawnPoints.Length == 0)
                return transform; // Fallback to this object's position

            // Use Modulo (%) so if we have 2 points but 3 players, 
            // Player 3 loops back to the first point.
            int index = (int)(clientId % (ulong)spawnPoints.Length);

            return spawnPoints[index];
        }
    }
}