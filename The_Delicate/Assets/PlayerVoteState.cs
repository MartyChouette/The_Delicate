using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// Holds the current "Vote" for this player.
    /// In Distributed mode, we use Owner-Write permissions so the player 
    /// can update their own vote without needing a ServerRPC.
    /// </summary>
    public class PlayerVoteState : NetworkBehaviour
    {
        // -1 = None, 0 = Fear, 1 = Abandonment, etc.
        // We use int because generic Enums in NetworkVariables can be tricky in some versions.
        public NetworkVariable<int> currentVote = new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner // <--- Critical for Distributed
        );

        public void SetVote(EmotionType emotion)
        {
            if (!IsOwner) return;
            currentVote.Value = (int)emotion;
            Debug.Log($"[Vote] I voted for {emotion}");
        }

        public EmotionType GetVote()
        {
            if (currentVote.Value == -1) return (EmotionType)(-1); // Invalid/None
            return (EmotionType)currentVote.Value;
        }
    }
}