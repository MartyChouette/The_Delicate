using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// Distributed Authority Version.
    /// 1. Updates the Local Player's vote when they touch it.
    /// 2. Lights up if the Local Player has selected this emotion.
    /// </summary>
    public class EmotionSelectionTerminal : MonoBehaviour
    {
        public EmotionType emotionToSelect;

        [Header("Visual Feedback")]
        [Tooltip("The MeshRenderer of the button/pedestal that changes color.")]
        public Renderer indicatorRenderer;

        [Tooltip("Material to use when NOT selected (e.g., Grey).")]
        public Material offMat;

        [Tooltip("Material to use when SELECTED (e.g., Glowing Red).")]
        public Material onMat;

        private void OnTriggerEnter(Collider other)
        {
            // 1. Find the player who stepped inside
            var voter = other.GetComponentInParent<PlayerVoteState>();

            // 2. If it's the LOCAL player (me), update my vote
            if (voter != null && voter.IsOwner)
            {
                voter.SetVote(emotionToSelect);
            }
        }

        private void Update()
        {
            // 3. Visual Feedback Loop
            // Check what the local player currently has selected
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerVoteState>();

                if (localPlayer != null && indicatorRenderer != null)
                {
                    // If I voted for THIS emotion, turn the light ON. Otherwise, OFF.
                    bool amISelected = (localPlayer.GetVote() == emotionToSelect);

                    // Only swap materials if needed (performance optimization)
                    Material targetMat = amISelected ? onMat : offMat;
                    if (indicatorRenderer.sharedMaterial != targetMat)
                    {
                        indicatorRenderer.material = targetMat;
                    }
                }
            }
        }
    }
}