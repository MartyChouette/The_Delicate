using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// Put this on the box + on a child of the player body that should receive magnets.
    /// DISTRIBUTED VERSION (Unity 6 Compatible):
    /// - Uses Server Relay pattern so any player can register a magnet.
    /// </summary>
    public class MagnetAttachPoint : NetworkBehaviour
    {
        public EmotionState emotionState;

        // The local list of magnets stuck to this object
        private readonly List<Magnet> _magnets = new();
        public IReadOnlyList<Magnet> Magnets => _magnets;

        private void Awake()
        {
            if (emotionState == null)
                emotionState = GetComponentInParent<EmotionState>();
        }

        // ------------------------------------------------------------------------
        // 1. REGISTER LOGIC (Sticking)
        // ------------------------------------------------------------------------

        /// <summary>
        /// Called by Magnet.cs when it detects a collision with this object.
        /// </summary>
        public void RegisterMagnet(Magnet magnet)
        {
            // Step 1: Client tells Server "I added a magnet"
            RegisterMagnetServerRpc(magnet);
        }

        [Rpc(SendTo.Server)]
        private void RegisterMagnetServerRpc(NetworkBehaviourReference magnetRef)
        {
            // Step 2: Server tells Everyone "Add this magnet to your list"
            RegisterMagnetBroadcastRpc(magnetRef);
        }

        [Rpc(SendTo.Everyone)]
        private void RegisterMagnetBroadcastRpc(NetworkBehaviourReference magnetRef)
        {
            // Step 3: Everyone updates their local list
            if (magnetRef.TryGet(out Magnet magnet))
            {
                if (!_magnets.Contains(magnet))
                {
                    _magnets.Add(magnet);
                    // Debug.Log($"[MagnetAttach] Added {magnet.wordId.Value}");
                }
            }
        }

        // ------------------------------------------------------------------------
        // 2. UNREGISTER LOGIC (Unsticking)
        // ------------------------------------------------------------------------

        /// <summary>
        /// Called by Magnet.cs when it is grabbed/thrown off.
        /// </summary>
        public void UnregisterMagnet(Magnet magnet)
        {
            UnregisterMagnetServerRpc(magnet);
        }

        [Rpc(SendTo.Server)]
        private void UnregisterMagnetServerRpc(NetworkBehaviourReference magnetRef)
        {
            UnregisterMagnetBroadcastRpc(magnetRef);
        }

        [Rpc(SendTo.Everyone)]
        private void UnregisterMagnetBroadcastRpc(NetworkBehaviourReference magnetRef)
        {
            if (magnetRef.TryGet(out Magnet magnet))
            {
                if (_magnets.Contains(magnet))
                {
                    _magnets.Remove(magnet);
                }
            }
        }
    }
}