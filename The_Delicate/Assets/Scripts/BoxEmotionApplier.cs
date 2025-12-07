using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    [RequireComponent(typeof(Rigidbody))]
    public class BoxEmotionApplier : NetworkBehaviour
    {
        public EmotionState emotionState;
        public MagnetAttachPoint magnetAttach;
        public Rigidbody rb;

        [Header("Abandonment Settings")]
        public float baseDriftStrength = 5f;

        [Header("Denial Settings")]
        public Renderer[] renderersToHide;
        public Collider[] collidersToToggle;

        private float _denialTimer;

        // The "Personality" the box was born with (from player votes)
        private float _baseAbandonment = 0.3f;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (emotionState == null) emotionState = GetComponent<EmotionState>();
            if (magnetAttach == null) magnetAttach = GetComponentInChildren<MagnetAttachPoint>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Capture the "Mixture" values set by the GameSessionManager as our baseline.
            // We only do this on the Owner because only the Owner runs the physics logic.
            if (IsOwner)
            {
                // If the box spawned with 0.8 Abandonment because everyone voted for it,
                // we save that as the baseline so we don't accidentally reset it to 0.3f later.
                _baseAbandonment = emotionState.abandonmentIntensity.Value;
            }
        }

        private void FixedUpdate()
        {
            // DISTRIBUTED FIX: The Owner (whoever is holding it or spawned it) runs the physics
            if (!IsOwner || emotionState == null) return;

            float dt = Time.fixedDeltaTime;

            // -----------------------------------------
            // 1) ABANDONMENT DRIFT
            // -----------------------------------------
            // Start with the box's natural personality
            float targetAbandon = _baseAbandonment;

            // Magnets modify/soothe from that baseline
            if (magnetAttach != null)
            {
                foreach (var mag in magnetAttach.Magnets)
                {
                    if (mag == null) continue;

                    // FIX: Must use .Value for NetworkVariables
                    switch (mag.wordId.Value)
                    {
                        case MagnetWordId.Stay:
                        case MagnetWordId.DontLeave:
                            targetAbandon *= 0.3f; // Strong soothing
                            break;
                        case MagnetWordId.Safe:
                            targetAbandon *= 0.6f; // Moderate soothing
                            break;
                    }
                }
            }

            // Smoothly move the actual network variable toward our calculated target
            emotionState.LerpAbandonment(targetAbandon, dt);

            // Apply Physics Forces
            float abandon = emotionState.abandonmentIntensity.Value;
            if (emotionState.AbandonmentDef != null && abandon > 0.01f)
            {
                Vector3 driftDir = GetAbandonmentDirection();
                float strength = baseDriftStrength * abandon;
                rb.AddForce(driftDir * strength, ForceMode.Acceleration);
            }

            // -----------------------------------------
            // 2) DENIAL – phasing/hiding
            // -----------------------------------------
            if (emotionState.DenialDef != null)
            {
                float denial = emotionState.denialIntensity.Value;
                if (denial > 0.05f)
                {
                    _denialTimer += dt;
                    // Higher intensity = Faster blinking
                    float interval = Mathf.Max(0.5f, emotionState.DenialDef.phaseIntervalSeconds / Mathf.Max(denial, 0.01f));

                    if (_denialTimer >= interval)
                    {
                        _denialTimer = 0f;
                        ToggleDenialPhase();
                    }
                }
                else
                {
                    // Ensure visible if denial drops to zero
                    EnsureVisible();
                }
            }

            // -----------------------------------------
            // 3) ANGER – random physical jolt
            // -----------------------------------------
            if (emotionState.AngerDef != null)
            {
                float anger = emotionState.angerIntensity.Value;
                if (anger > 0.1f)
                {
                    float chance = 0.3f * anger * dt;
                    if (Random.value < chance)
                    {
                        Vector3 impulse = new Vector3(
                            Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)
                        ).normalized * emotionState.AngerDef.randomImpulseStrength;

                        rb.AddForce(impulse, ForceMode.Impulse);
                    }
                }
            }
        }

        private Vector3 GetAbandonmentDirection()
        {
            var players = FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None);
            if (players == null || players.Length == 0) return Vector3.zero;

            Vector3 avg = Vector3.zero;
            foreach (var p in players) avg += p.transform.position;
            avg /= players.Length;

            Vector3 dir = (transform.position - avg);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            return dir.normalized;
        }

        private void ToggleDenialPhase()
        {
            bool currentlyEnabled = (collidersToToggle.Length == 0 || collidersToToggle[0].enabled);
            bool newEnabled = !currentlyEnabled;
            foreach (var c in collidersToToggle) c.enabled = newEnabled;
            foreach (var r in renderersToHide) r.enabled = newEnabled;
        }

        private void EnsureVisible()
        {
            if (renderersToHide.Length > 0 && !renderersToHide[0].enabled)
            {
                foreach (var c in collidersToToggle) c.enabled = true;
                foreach (var r in renderersToHide) r.enabled = true;
            }
        }
    }
}