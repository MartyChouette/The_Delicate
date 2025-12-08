using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    public class EmotionSelectionTerminal : MonoBehaviour
    {
        public EmotionType emotionToSelect;

        [Header("Visual Feedback")]
        public Renderer indicatorRenderer;
        public Material offMat;
        public Material onMat;

        private void OnTriggerEnter(Collider other)
        {
            var voter = other.GetComponentInParent<PlayerVoteState>();
            if (voter != null && voter.IsOwner)
            {
                voter.SetVote(emotionToSelect);
            }
        }

        private void Update()
        {
            // SAFETY CHECK 1: Is Netcode running?
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

            // SAFETY CHECK 2: Does the local player exist yet?
            if (NetworkManager.Singleton.SpawnManager == null) return;
            var localPlayerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

            if (localPlayerObj == null) return; // Player hasn't spawned -> Stop here.

            // Now it is safe to get the component
            var localPlayer = localPlayerObj.GetComponent<PlayerVoteState>();

            if (localPlayer != null && indicatorRenderer != null)
            {
                bool amISelected = (localPlayer.GetVote() == emotionToSelect);
                Material targetMat = amISelected ? onMat : offMat;
                if (indicatorRenderer.sharedMaterial != targetMat)
                {
                    indicatorRenderer.material = targetMat;
                }
            }
        }
    }
}