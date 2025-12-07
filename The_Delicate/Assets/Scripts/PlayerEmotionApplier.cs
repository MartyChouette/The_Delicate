using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    public class PlayerEmotionApplier : NetworkBehaviour
    {
        public PlayerAvatar avatar;
        public EmotionState emotionState;
        public MagnetAttachPoint magnetAttach;

        [Header("Fear Physics")]
        public float swayTorque = 5f;
        public float stumbleImpulse = 2f;

        [Header("Helpers")]
        public float nearOtherPlayerRadius = 4f;

        private Rigidbody _rb;

        private void Awake()
        {
            if (avatar == null) avatar = GetComponent<PlayerAvatar>();
            if (emotionState == null) emotionState = GetComponent<EmotionState>();
            if (magnetAttach == null) magnetAttach = GetComponentInChildren<MagnetAttachPoint>();
            _rb = avatar.rb;
        }

        private void FixedUpdate()
        {
            // DISTRIBUTED FIX: I calculate my own emotions physics
            if (!IsOwner || emotionState == null) return;

            float dt = Time.fixedDeltaTime;

            // --------------------------------------------------------
            // 1) Logic: Decide Target Fear based on situation
            // --------------------------------------------------------
            float targetFear = 0.3f; // Base anxiety
            float nearestDist = GetNearestOtherPlayerDistance();

            // Being near other players calms you down
            if (nearestDist > nearOtherPlayerRadius)
                targetFear = 0.8f; // Alone = High Fear
            else
                targetFear = 0.25f; // Together = Low Fear

            // 2) Magnets on this player can reduce fear further
            if (magnetAttach != null)
            {
                foreach (var mag in magnetAttach.Magnets)
                {
                    if (mag == null) continue;

                    // FIX: Use .Value for NetworkVariable
                    switch (mag.wordId.Value)
                    {
                        case MagnetWordId.Warmth: targetFear *= 0.4f; break;
                        case MagnetWordId.Help: targetFear *= 0.7f; break;
                        case MagnetWordId.Sorry: targetFear *= 0.9f; break;
                    }
                }
            }

            // Apply calculation to the Networked State
            emotionState.LerpFear(targetFear, dt);

            // --------------------------------------------------------
            // 3) Apply Physical Forces based on current Emotion Level
            // --------------------------------------------------------
            float fear = emotionState.fearIntensity.Value;
            float anger = emotionState.angerIntensity.Value;

            // Fear Sway (Dizziness)
            if (emotionState.FearDef != null && fear > 0.01f)
            {
                Vector3 randomAxis = new Vector3(0f, 0f, Random.Range(-1f, 1f));
                _rb.AddTorque(randomAxis * swayTorque * fear, ForceMode.Acceleration);
            }

            // Fear Stumble (Random trips)
            if (emotionState.FearDef != null && fear > 0.4f)
            {
                float chance = emotionState.FearDef.stumbleChancePerSecond * fear * dt;
                if (Random.value < chance)
                {
                    // Push player sideways randomly
                    Vector3 sideways = avatar.transform.right * Random.Range(-1f, 1f);
                    _rb.AddForce(sideways.normalized * stumbleImpulse, ForceMode.VelocityChange);
                }
            }

            // Anger Push (Random acceleration bursts)
            if (emotionState.AngerDef != null && anger > 0.1f)
            {
                Vector3 randomImpulse = new Vector3(
                    Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)
                ).normalized * emotionState.AngerDef.randomImpulseStrength * anger * dt;
                _rb.AddForce(randomImpulse, ForceMode.Acceleration);
            }
        }

        private float GetNearestOtherPlayerDistance()
        {
            float nearest = float.MaxValue;
            var players = FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None);

            foreach (var other in players)
            {
                if (other == avatar) continue; // Don't check distance to self

                float d = Vector3.Distance(avatar.transform.position, other.transform.position);
                if (d < nearest) nearest = d;
            }
            return nearest;
        }
    }
}