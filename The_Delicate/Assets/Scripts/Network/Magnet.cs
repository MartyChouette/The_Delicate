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
        // 1. Sync the ID so everyone knows what word this is
        public NetworkVariable<MagnetWordId> wordId = new NetworkVariable<MagnetWordId>();

        [Header("Visuals")]
        public TMP_Text label; // Drag a TextMeshPro object (child of this magnet) here

        [Header("Physics")]
        public Rigidbody rb;
        [Tooltip("Layers that can receive this magnet (box, player bodies).")]
        public LayerMask stickMask;
        public float stripImpulseThreshold = 10f;

        private bool _isStuck;
        private Transform _stuckTo;
        private Vector3 _localPos;
        private Quaternion _localRot;

        private void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 2. Listen for changes to the word (so text updates automatically)
            wordId.OnValueChanged += OnWordIdChanged;

            // Force initial update
            UpdateVisuals();
        }

        public override void OnNetworkDespawn()
        {
            wordId.OnValueChanged -= OnWordIdChanged;
        }

        /// <summary>
        /// Call this after spawning the magnet to set its type.
        /// </summary>
        public void SetWord(MagnetWordId newWord)
        {
            // Only Owner (or Server) can write to the NetworkVariable in Distributed
            if (IsOwner || IsServer)
            {
                wordId.Value = newWord;
            }
        }

        private void OnWordIdChanged(MagnetWordId previous, MagnetWordId current)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (label != null)
            {
                label.text = wordId.Value.ToString();
            }
        }

        // ----------------- PHYSICS (Distributed Logic) -----------------

        private void FixedUpdate()
        {
            // DISTRIBUTED FIX: Only Owner simulates physics
            if (!IsOwner) return;

            if (_isStuck && _stuckTo != null)
            {
                transform.position = _stuckTo.TransformPoint(_localPos);
                transform.rotation = _stuckTo.rotation * _localRot;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // DISTRIBUTED FIX: Only Owner detects hits
            if (!IsOwner) return;

            if (!_isStuck)
            {
                if (((1 << collision.gameObject.layer) & stickMask.value) != 0)
                {
                    TryStickTo(collision);
                }
            }
            else
            {
                if (collision.impulse.magnitude > stripImpulseThreshold)
                {
                    Unstick();
                }
            }
        }

        private void TryStickTo(Collision collision)
        {
            ContactPoint contact = collision.GetContact(0);
            Transform target = collision.transform;

            _stuckTo = target;
            _localPos = target.InverseTransformPoint(contact.point);
            _localRot = Quaternion.identity;

            _isStuck = true;
            rb.isKinematic = true;

            // Notify the object we hit (Box/Player)
            // Make sure MagnetAttachPoint.cs is updated to have 'RegisterMagnet' (no 'Server' suffix)
            var attachPoint = collision.collider.GetComponentInParent<MagnetAttachPoint>();
            if (attachPoint != null)
            {
                attachPoint.RegisterMagnet(this);
            }
        }

        public void Unstick()
        {
            if (!_isStuck) return;

            _isStuck = false;
            rb.isKinematic = false;

            if (_stuckTo != null)
            {
                var attachPoint = _stuckTo.GetComponentInParent<MagnetAttachPoint>();
                if (attachPoint != null)
                {
                    attachPoint.UnregisterMagnet(this);
                }
            }

            _stuckTo = null;
        }
    }
}