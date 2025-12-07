using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    [RequireComponent(typeof(Rigidbody), typeof(EmotionState))]
    public class BoxPersonalityController : NetworkBehaviour
    {
        [Header("References")]
        public EmotionState emotionState;
        public MagnetAttachPoint attachPoint;
        public Rigidbody rb;
        public Collider boxCollider;

        [Header("Tuning")]
        public float angerPushForce = 15f;
        public float angerPushRadius = 3f;
        public float fearJitterForce = 5f;
        public float denialPhaseInterval = 2.0f;

        [Header("Ghost Settings")]
        [Tooltip("If true, the box stays solid (hittable) even when ghosting/floating.")]
        public bool ghostKeepCollider = true;

        // Internal State
        private float _denialTimer;
        private bool _isGhost;

        private void Awake()
        {
            if (emotionState == null) emotionState = GetComponent<EmotionState>();
            if (attachPoint == null) attachPoint = GetComponent<MagnetAttachPoint>();
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (boxCollider == null) boxCollider = GetComponent<Collider>();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            // --- GATHER WORDS ---
            bool hasSorry = false, hasQuiet = false, hasPlease = false;
            bool hasHelp = false, hasWarmth = false;
            bool hasSafe = false, hasHoldMe = false;
            bool hasStay = false, hasDontLeave = false;

            foreach (var mag in attachPoint.Magnets)
            {
                if (mag == null) continue;
                switch (mag.wordId.Value)
                {
                    case MagnetWordId.Sorry: hasSorry = true; break;
                    case MagnetWordId.Quiet: hasQuiet = true; break;
                    case MagnetWordId.Please: hasPlease = true; break;
                    case MagnetWordId.Help: hasHelp = true; break;
                    case MagnetWordId.Warmth: hasWarmth = true; break;
                    case MagnetWordId.Safe: hasSafe = true; break;
                    case MagnetWordId.HoldMe: hasHoldMe = true; break;
                    case MagnetWordId.Stay: hasStay = true; break;
                    case MagnetWordId.DontLeave: hasDontLeave = true; break;
                }
            }

            // --- ANGER ---
            float anger = emotionState.angerIntensity.Value;
            if (anger > 0.5f && !hasSorry)
            {
                float force = angerPushForce * anger;
                if (hasQuiet || hasPlease) force *= 0.3f;

                Collider[] hits = Physics.OverlapSphere(transform.position, angerPushRadius);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        Rigidbody pRb = hit.GetComponent<Rigidbody>();
                        if (pRb != null)
                        {
                            Vector3 dir = (hit.transform.position - transform.position).normalized;
                            pRb.AddForce(dir * force, ForceMode.Force);
                        }
                    }
                }
            }

            // --- DENIAL (Ghost + Anti-Gravity) ---
            float denial = emotionState.denialIntensity.Value;
            if (denial > 0.5f && !hasHelp)
            {
                _denialTimer += Time.fixedDeltaTime;
                float interval = hasWarmth ? denialPhaseInterval * 2f : denialPhaseInterval;

                if (_denialTimer > interval)
                {
                    _denialTimer = 0f;
                    _isGhost = !_isGhost;

                    // Only turn off collider if you UNCHECKED the "ghostKeepCollider" setting
                    if (!ghostKeepCollider)
                    {
                        boxCollider.enabled = !_isGhost;
                    }
                }

                if (_isGhost)
                {
                    // Anti-Gravity: Float
                    rb.useGravity = false;
                    rb.linearVelocity *= 0.95f;
                    rb.angularVelocity *= 0.95f;
                }
                else
                {
                    rb.useGravity = true;
                }
            }
            else
            {
                // Reset
                boxCollider.enabled = true;
                _isGhost = false;
                rb.useGravity = true;
            }

            // --- FEAR ---
            float fear = emotionState.fearIntensity.Value;
            if (fear > 0.5f && !hasSafe)
            {
                bool isBeingHeld = rb.isKinematic;
                if (hasHoldMe && isBeingHeld) { /* Calm */ }
                else if (!_isGhost)
                {
                    Vector3 randomDir = Random.insideUnitSphere;
                    randomDir.y = 0;
                    rb.AddForce(randomDir.normalized * fearJitterForce * fear, ForceMode.Impulse);
                }
            }

            // --- ABANDONMENT ---
            float abandon = emotionState.abandonmentIntensity.Value;
            if (abandon > 0.5f && !_isGhost)
            {
                if (hasStay) { rb.useGravity = true; rb.mass = 1f; }
                else if (hasDontLeave) { rb.useGravity = true; rb.mass = 10f; }
                else
                {
                    rb.useGravity = false;
                    rb.AddForce(Vector3.up * 2f, ForceMode.Force);
                }
            }
            else if (!_isGhost)
            {
                rb.useGravity = true;
                rb.mass = 1f;
            }
        }
    }
}