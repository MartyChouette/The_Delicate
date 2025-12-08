using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
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
        public float grabRange = 10f;
        public LayerMask grabMask;

        [Header("Strength Settings")]
        public float holdingStrengthMultiplier = 3.0f;

        [Header("Magnet Toss Settings")]
        public float tossForce = 6f;

        [Header("Focus Attraction Settings")]
        public float focusAttractionStrength = 4f;
        public float focusMaxDistance = 4f;

        // State Tracking
        private bool _leftPressed;
        private bool _rightPressed;
        private bool _leftLocked;
        private bool _rightLocked;

        private FixedJoint _leftJoint;
        private FixedJoint _rightJoint;

        private Magnet _leftHeldMagnet;
        private Magnet _rightHeldMagnet;

        private ulong _currentVoicePartnerId = ulong.MaxValue;
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
            bool isOwner = IsOwner;
            if (leftHandRb != null) leftHandRb.isKinematic = !isOwner;
            if (rightHandRb != null) rightHandRb.isKinematic = !isOwner;
        }

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
                TryGrabExact(side);
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

        public void RequestTossMagnet()
        {
            if (!IsOwner) return;
            PerformToss();
        }

        public void SetFocusPoint(Vector3 point, bool hasFocus)
        {
            _hasFocus = hasFocus;
            _focusPoint = point;
        }

        private void TryGrabExact(HandSide side)
        {
            Transform handTf = side == HandSide.Left ? leftHand : rightHand;
            Rigidbody handRb = side == HandSide.Left ? leftHandRb : rightHandRb;
            if (handTf == null || handRb == null) return;

            var cam = avatar != null ? avatar.playerCamera : null;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabMask, QueryTriggerInteraction.Ignore))
            {
                var hitRb = hit.rigidbody;
                if (hitRb == null) return;

                // --- CRASH PREVENTION (FixedJoint can't connect to itself) ---
                if (hitRb == leftHandRb || hitRb == rightHandRb) return;
                if (avatar != null && hitRb == avatar.rb) return;
                if (hitRb.transform.root == transform.root) return;
                // -----------------------------------------------------------

                // Teleport Visuals
                handTf.position = hit.point;
                handTf.rotation = Quaternion.LookRotation(-hit.normal);
                handRb.linearVelocity = Vector3.zero;
                handRb.angularVelocity = Vector3.zero;

                var netObj = hitRb.GetComponent<NetworkObject>();
                PlayerAvatar hitAvatar = hitRb.GetComponent<PlayerAvatar>();

                // ==========================================================
                // DISABLED FOR PROXIMITY CHAT - Grab should not trigger voice
                // ==========================================================
                /*
                if (hitAvatar != null && netObj != null)
                {
                    Debug.Log($"[GRAB DEBUG] Hit a Player! Requesting Voice Link...");
                    RequestVoiceLinkServerRpc(netObj.OwnerClientId);
                }
                else 
                */
                if (netObj != null)
                {
                    // If it's an item/box, take ownership so we can move it
                    if (!netObj.IsOwner) netObj.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
                }

                // Magnet Logic
                Magnet mag = hitRb.GetComponent<Magnet>();
                if (mag != null) mag.Unstick();

                // Joint Creation
                FixedJoint joint = handTf.GetComponent<FixedJoint>();
                if (joint != null) Destroy(joint);

                joint = handTf.gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = hitRb;
                joint.breakForce = 5000f;
                joint.breakTorque = 5000f;

                if (side == HandSide.Left) { _leftJoint = joint; _leftHeldMagnet = mag; }
                else { _rightJoint = joint; _rightHeldMagnet = mag; }
            }
        }

        private void ReleaseHand(HandSide side)
        {
            Transform handTf = side == HandSide.Left ? leftHand : rightHand;
            if (handTf == null) return;

            var joint = handTf.GetComponent<FixedJoint>();

            // ==========================================================
            // DISABLED FOR PROXIMITY CHAT - Release shouldn't disconnect
            // ==========================================================
            /*
            if (joint != null && joint.connectedBody != null)
            {
                if (joint.connectedBody.GetComponent<PlayerAvatar>() != null)
                {
                    RequestVoiceDisconnectServerRpc();
                }
            }
            */

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

            ReleaseHand(side);

            mag.Unstick();
            mag.rb.linearVelocity = Vector3.zero;
            mag.rb.angularVelocity = Vector3.zero;

            Vector3 forward = avatar != null ? avatar.transform.forward : transform.forward;
            Vector3 dir = (forward * 0.8f + Vector3.up * 0.5f).normalized;

            mag.transform.position = handTf.position;
            mag.rb.AddForce(dir * tossForce, ForceMode.VelocityChange);
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;
            UpdateHandTarget(leftHand, leftHandRb, true);
            UpdateHandTarget(rightHand, rightHandRb, false);
        }

        private void UpdateHandTarget(Transform hand, Rigidbody handRb, bool isLeft)
        {
            if (hand == null || handRb == null) return;

            bool isHolding = hand.GetComponent<FixedJoint>() != null;

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
            float currentSpring = isHolding ? handSpring * holdingStrengthMultiplier : handSpring;
            Vector3 desiredVel = toTarget * currentSpring;
            Vector3 force = desiredVel - handRb.linearVelocity * handDamping;

            handRb.AddForce(force, ForceMode.Acceleration);
        }

        // --- EMPTY RPCS TO PREVENT ERRORS ---

        [ServerRpc]
        private void RequestVoiceLinkServerRpc(ulong targetClientId) { }

        [ServerRpc]
        private void RequestVoiceDisconnectServerRpc() { }

        [ClientRpc]
        private void EnableVoiceClientRpc(ulong partnerId, ClientRpcParams clientRpcParams = default)
        {
            // DISABLED - Do not touch SetMute
        }

        [ClientRpc]
        private void DisableVoiceClientRpc(ClientRpcParams clientRpcParams = default)
        {
            // DISABLED - Do not touch SetMute
        }
    }
}