using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    public class FinishZone : NetworkBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            if (other.GetComponent<BoxHealth>() != null || other.GetComponentInParent<BoxHealth>() != null)
            {
                GameSessionManager.Instance.TriggerWin();
            }
        }
    }
}