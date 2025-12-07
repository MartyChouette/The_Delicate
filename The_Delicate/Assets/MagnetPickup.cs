using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// A physical magnet lying in the world waiting to be picked up.
    /// Distributed Version: Syncs WordID via NetworkVariable.
    /// </summary>
    public class MagnetPickup : NetworkBehaviour
    {
        // Use NetworkVariable so late-joiners see the correct word
        public NetworkVariable<MagnetWordId> wordId = new NetworkVariable<MagnetWordId>();

        [Header("Visuals")]
        public TMP_Text label;

        // If you place these in the editor, you can set the initial word here
        [SerializeField] private MagnetWordId initialWordInEditor;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // If we are the server/host spawning this, set the initial value
            if (IsServer)
            {
                wordId.Value = initialWordInEditor;
            }

            // Update text when the value changes (and immediately for current value)
            wordId.OnValueChanged += OnWordChanged;
            UpdateLabel(wordId.Value);
        }

        public override void OnNetworkDespawn()
        {
            wordId.OnValueChanged -= OnWordChanged;
        }

        private void OnWordChanged(MagnetWordId oldVal, MagnetWordId newVal)
        {
            UpdateLabel(newVal);
        }

        private void UpdateLabel(MagnetWordId id)
        {
            if (label != null)
                label.text = id.ToString();
        }

        /// <summary>
        /// Called by PlayerInteractionController when they click this pickup.
        /// </summary>
        public void OnPickedUp()
        {
            // In Distributed/Host mode, usually the Host controls despawning.
            // If we are the owner (Host) or Server, we can despawn.
            if (IsServer || IsOwner)
            {
                GetComponent<NetworkObject>().Despawn();
            }
            else
            {
                // If a client picks it up, they need to ask the Server/Host to despawn it.
                // You might need a ServerRpc here if you are strictly Client-Side, 
                // but in Distributed with "Allow Destroy", you might be able to just Despawn.
                // For safety, let's use a ServerRpc pattern if we weren't the owner.
                RequestDespawnServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestDespawnServerRpc()
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}