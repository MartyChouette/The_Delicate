// BoxHealth.cs
using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    public class BoxHealth : NetworkBehaviour
    {
        public float damageThreshold = 4.0f;
        public float damageMultiplier = 5.0f;

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer) return;
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Magnet")) return;

            float impact = collision.relativeVelocity.magnitude;
            if (impact > damageThreshold)
            {
                float dmg = (impact - damageThreshold) * damageMultiplier;
                GameSessionManager.Instance.TakeDamage(dmg);
            }
        }
    }
}
