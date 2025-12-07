using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// DISTRIBUTED AUTHORITY VERSION
    /// - Removed TryGrabServerRpc, ReleaseHandRpc, etc.
    /// - Owner grabs objects directly.
    /// - Includes logic to Request Ownership of the object being grabbed.
    /// </summary>
    public class PlayerHandController : NetworkBehaviour
    {
        [Header("References")]
        public PlayerAvatar avatar;
        public Transform leftHand;
        public Transform rightHand;
        public Rigidbody leftHandRb;
        public Rigidbody rightHandRb;

        [Header("Hand Settings")]
        public float handDistance = 1.2f;
        public float handHeight = 1.2f;
        public float handSpring = 200f;
        public float handDamping = 25f;
        public float grabRange = 2.5f;
        public LayerMask grabMask;

        [Header("Magnet Toss Settings")]
        public float tossForce = 6f;

        [Header("Focus Attraction Settings")]
        public float focusAttractionStrength = 4f;
        public float focusMaxDistance = 4f;

        private bool _leftPressed;
        private bool _rightPressed;
        private bool _leftLocked;
        private bool _rightLocked;

        private FixedJoint _leftJoint;
        private FixedJoint _rightJoint;

        private Magnet _leftHeldMagnet;
        private Magnet _rightHeldMagnet;

        private bool _hasFocus;
        private Vector3 _focusPoint;

        private void Awake()
        {
            if (avatar == null) avatar = GetComponent<PlayerAvatar>();
            if (leftHand != null && leftHandRb == null) leftHandRb = leftHand.GetComponent<Rigidbody>();
            if (rightHand != null && rightHandRb == null) rightHandRb = rightHand.GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // In Distributed, Owner needs physics enabled to control the hands
            if (IsOwner)
            {
                if (leftHandRb != null) leftHandRb.isKinematic = false;
                if (rightHandRb != null) rightHandRb.isKinematic = false;
            }
            else
            {
                // Remotes are kinematic (synced via NetworkTransform)
                if (leftHandRb != null) leftHandRb.isKinematic = true;
                if (rightHandRb != null) rightHandRb.isKinematic = true;
            }
        }

        // ───────────────────── Input hooks (Called locally) ─────────────────────

        public void SetHandPressed(HandSide side, bool pressed)
        {
            if (!IsOwner) return;

            if (side == HandSide.Left) _leftPressed = pressed;
            else _rightPressed = pressed;

            if (!pressed)
            {
                if (side == HandSide.Left && !_leftLocked) ReleaseHand(side);
                if (side == HandSide.Right && !_rightLocked) ReleaseHand(side);
            }
            else
            {
                TryGrabClosest(side);
            }
        }

        public void ToggleLockHand(HandSide side)
        {
            if (!IsOwner) return;

            if (side == HandSide.Left)
            {
                _leftLocked = !_leftLocked;
                if (!_leftLocked && !_leftPressed) ReleaseHand(HandSide.Left);
            }
            else
            {
                _rightLocked = !_rightLocked;
                if (!_rightLocked && !_rightPressed) ReleaseHand(HandSide.Right);
            }
        }

        public void ToggleLockBothHands()
        {
            if (!IsOwner) return;
            _leftLocked = !_leftLocked;
            _rightLocked = _leftLocked;

            if (!_leftLocked && !_leftPressed) ReleaseHand(HandSide.Left);
            if (!_rightLocked && !_rightPressed) ReleaseHand(HandSide.Right);
        }

        public void RequestTossMagnet()
        {
            if (!IsOwner) return;
            PerformToss();
        }

        // ───────────────────── LOCAL ACTIONS (No RPCs) ─────────────────────

        public void SetFocusPoint(Vector3 point, bool hasFocus)
        {
            _hasFocus = hasFocus;
            _focusPoint = point;
        }

        private void TryGrabClosest(HandSide side)
        {
            Transform handTf = side == HandSide.Left ? leftHand : rightHand;
            if (handTf == null) return;

            var cam = avatar != null ? avatar.playerCamera : null;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabMask, QueryTriggerInteraction.Ignore))
            {
                var hitRb = hit.rigidbody;
                if (hitRb == null) return;

                // --- DISTRIBUTED OWNERSHIP LOGIC ---
                // Before we can grab it, we must own it.
                var netObj = hitRb.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsOwner)
                {
                    // This assumes "Allow Grab Ownership" is checked in your config
                    // or Topology allows stealing ownership.
                    netObj.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
                }

                Magnet mag = hitRb.GetComponent<Magnet>();
                if (mag != null) mag.Unstick();

                // Create the joint locally
                FixedJoint joint = handTf.GetComponent<FixedJoint>();
                if (joint != null) Destroy(joint);

                joint = handTf.gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = hitRb;
                joint.breakForce = 2000f;
                joint.breakTorque = 2000f;

                if (side == HandSide.Left) { _leftJoint = joint; _leftHeldMagnet = mag; }
                else { _rightJoint = joint; _rightHeldMagnet = mag; }
            }
        }

        private void ReleaseHand(HandSide side)
        {
            Transform handTf = side == HandSide.Left ? leftHand : rightHand;
            if (handTf == null) return;

            var joint = handTf.GetComponent<FixedJoint>();
            if (joint != null) Destroy(joint);

            if (side == HandSide.Left) { _leftJoint = null; _leftHeldMagnet = null; }
            else { _rightJoint = null; _rightHeldMagnet = null; }
        }

        private void PerformToss()
        {
            Magnet mag = _leftHeldMagnet != null ? _leftHeldMagnet : _rightHeldMagnet;
            if (mag == null) return;

            Transform handTf = (mag == _leftHeldMagnet) ? leftHand : rightHand;
            HandSide side = (mag == _leftHeldMagnet) ? HandSide.Left : HandSide.Right;

            // Clear Joint
            var joint = handTf.GetComponent<FixedJoint>();
            if (joint != null) Destroy(joint);
            if (side == HandSide.Left) { _leftJoint = null; _leftHeldMagnet = null; }
            else { _rightJoint = null; _rightHeldMagnet = null; }

            // Apply Force locally (since we now own the magnet from the grab)
            mag.Unstick();
            mag.rb.linearVelocity = Vector3.zero;
            mag.rb.angularVelocity = Vector3.zero;

            Vector3 forward = avatar != null ? avatar.transform.forward : transform.forward;
            Vector3 dir = (forward * 0.8f + Vector3.up * 0.5f).normalized;

            mag.transform.position = handTf.position;
            mag.rb.AddForce(dir * tossForce, ForceMode.VelocityChange);
        }

        // ───────────────────── Physics Loop ─────────────────────

        private void FixedUpdate()
        {
            // Only run simulation if we are the Owner
            if (!IsOwner) return;

            UpdateHandTarget(leftHand, leftHandRb, true);
            UpdateHandTarget(rightHand, rightHandRb, false);
        }

        private void UpdateHandTarget(Transform hand, Rigidbody handRb, bool isLeft)
        {
            if (hand == null || handRb == null) return;
            if (hand.GetComponent<FixedJoint>() != null) return;

            Vector3 bodyPos = avatar.transform.position + Vector3.up * handHeight;
            Vector3 side = avatar.transform.right * (isLeft ? -1f : 1f);
            Vector3 targetPos = bodyPos + side * 0.5f + avatar.transform.forward * handDistance;

            if (_hasFocus)
            {
                Vector3 focus = _focusPoint;
                focus.y = bodyPos.y;
                float dist = Vector3.Distance(bodyPos, focus);
                if (dist <= focusMaxDistance)
                {
                    float t = Mathf.Clamp01(1f - dist / focusMaxDistance);
                    float weight = t * focusAttractionStrength * Time.fixedDeltaTime;
                    targetPos = Vector3.Lerp(targetPos, focus, weight);
                }
            }

            Vector3 toTarget = targetPos - hand.position;
            Vector3 desiredVel = toTarget * handSpring;
            Vector3 force = desiredVel - handRb.linearVelocity * handDamping;

            handRb.AddForce(force, ForceMode.Acceleration);
        }
    }
}