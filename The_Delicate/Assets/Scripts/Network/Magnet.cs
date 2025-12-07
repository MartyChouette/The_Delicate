using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// DISTRIBUTED VERSION.
    /// - Collision logic runs on Owner.
    /// - WordID synced via NetworkVariable.
    /// - Visuals update via TMP_Text.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(NetworkObject))]
    public class Magnet : NetworkBehaviour
    {
        public NetworkVariable<MagnetWordId> wordId = new NetworkVariable<MagnetWordId>();

        [Header("Visuals")]
        public TMP_Text label;

        [Header("Physics")]
        public Rigidbody rb;
        [Tooltip("Layers that this magnet sticks to (e.g., The Box). NOT the Player.")]
        public LayerMask stickMask;

        [Tooltip("How hard you have to hit the box to stick. 0 = touching is enough.")]
        public float stickImpactThreshold = 0.5f;

        // State
        private bool _isStuck;
        private Transform _stuckTo;
        private Vector3 _localPos;
        private Quaternion _localRot;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            wordId.OnValueChanged += OnWordIdChanged;
            UpdateVisuals();
        }

        public override void OnNetworkDespawn()
        {
            wordId.OnValueChanged -= OnWordIdChanged;
        }

        public void SetWord(MagnetWordId newWord)
        {
            if (IsOwner || IsServer) wordId.Value = newWord;
        }

        private void OnWordIdChanged(MagnetWordId previous, MagnetWordId current)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (label != null) label.text = wordId.Value.ToString();
        }

        // ----------------- PHYSICS -----------------

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            // If stuck, force position/rotation to stay relative to the parent
            // This is "Manual Parenting" which is smoother for Netcode than actual Transform parenting
            if (_isStuck && _stuckTo != null)
            {
                transform.position = _stuckTo.TransformPoint(_localPos);
                transform.rotation = _stuckTo.rotation * _localRot;

                // Kill physics while stuck
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            else if (_isStuck && _stuckTo == null)
            {
                // Box was destroyed or lost
                Unstick();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsOwner) return;

            if (!_isStuck)
            {
                // 1. Check Mask (Is this the Box?)
                if (((1 << collision.gameObject.layer) & stickMask.value) != 0)
                {
                    // 2. Check Impact (Did we throw it or just nudge it?)
                    if (collision.relativeVelocity.magnitude >= stickImpactThreshold)
                    {
                        TryStickTo(collision);
                    }
                }
            }
        }

        private void TryStickTo(Collision collision)
        {
            ContactPoint contact = collision.GetContact(0);
            Transform target = collision.transform;

            _stuckTo = target;

            // 1. POSITION: Stick exactly where we hit
            // Calculate position relative to the box center/rotation
            _localPos = target.InverseTransformPoint(contact.point);

            // 2. ROTATION: Rotate to lie flat on the surface
            // contact.normal points OUT of the box. 
            // We want the magnet's "Back" to face the box, so "Forward" faces the normal.
            Quaternion lookRot = Quaternion.LookRotation(contact.normal);

            // Apply this rotation immediately so we can calculate the offset
            transform.rotation = lookRot;

            // Save the relative rotation so it turns with the box
            _localRot = Quaternion.Inverse(target.rotation) * transform.rotation;

            _isStuck = true;
            rb.isKinematic = true;

            // 3. Register with Box Logic (for Emotions)
            var attachPoint = collision.collider.GetComponentInParent<MagnetAttachPoint>();
            if (attachPoint != null)
            {
                attachPoint.RegisterMagnet(this);
            }
        }

        /// <summary>
        /// Called by PlayerHandController when grabbing this object
        /// </summary>
        public void Unstick()
        {
            if (!_isStuck) return;

            // Notify Box we are leaving
            if (_stuckTo != null)
            {
                var attachPoint = _stuckTo.GetComponentInParent<MagnetAttachPoint>();
                if (attachPoint != null) attachPoint.UnregisterMagnet(this);
            }

            _isStuck = false;
            _stuckTo = null;

            // Re-enable physics so we can be thrown again
            rb.isKinematic = false;
        }
    }
}