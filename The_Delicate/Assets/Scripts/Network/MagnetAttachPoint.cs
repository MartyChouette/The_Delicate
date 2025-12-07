using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// Put this on the box + on a child of the player body that should receive magnets.
    /// DISTRIBUTED VERSION:
    /// - Uses RPCs (RequireOwnership=false) so any player can tell the Box "I put a magnet on you".
    /// </summary>
    public class MagnetAttachPoint : NetworkBehaviour
    {
        public EmotionState emotionState;

        private readonly List<Magnet> _magnets = new();

        public IReadOnlyList<Magnet> Magnets => _magnets;

        private void Awake()
        {
            if (emotionState == null)
                emotionState = GetComponentInParent<EmotionState>();
        }

        // Called by Magnet.cs when it hits this object
        public void RegisterMagnet(Magnet magnet)
        {
            // We use an RPC to tell EVERYONE (including the Box Owner) to add this magnet.
            // requireOwnership = false allows any player (who threw the magnet) to call this.
            RegisterMagnetRpc(magnet);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void RegisterMagnetRpc(NetworkBehaviourReference magnetRef)
        {
            // Unwrap the reference to get the actual Magnet script
            if (magnetRef.TryGet(out Magnet magnet))
            {
                if (!_magnets.Contains(magnet))
                {
                    _magnets.Add(magnet);
                    // Optional: Debug.Log($"[MagnetAttach] Added {magnet.wordId} to {name}");
                }
            }
        }

        // Called by Magnet.cs when it falls off
        public void UnregisterMagnet(Magnet magnet)
        {
            UnregisterMagnetRpc(magnet);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void UnregisterMagnetRpc(NetworkBehaviourReference magnetRef)
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