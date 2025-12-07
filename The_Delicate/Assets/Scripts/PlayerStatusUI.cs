using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace EmotionBank
{
    /// <summary>
    /// Attach this to a Canvas under the player (or a central HUD).
    /// Assign a Text or TMP_Text field to show your own magnets.
    /// </summary>
    public class PlayerStatusUI : NetworkBehaviour
    {
        public MagnetAttachPoint playerMagnetAttach;
        public TMP_Text magnetText; // or TMP_Text if you prefer

        private void Awake()
        {
            if (playerMagnetAttach == null)
                playerMagnetAttach = GetComponentInParent<MagnetAttachPoint>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // Only show HUD for local player.
            gameObject.SetActive(IsOwner);
        }

        private void Update()
        {
            if (!IsOwner || magnetText == null || playerMagnetAttach == null) return;

            var list = playerMagnetAttach.Magnets;
            if (list.Count == 0)
            {
                magnetText.text = "";
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append('[').Append(list[i].wordId.ToString()).Append(']');
                if (i < list.Count - 1)
                    sb.Append(' ');
            }
            magnetText.text = sb.ToString();
        }
    }
}