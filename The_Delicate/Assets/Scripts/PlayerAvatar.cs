using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// DISTRIBUTED AUTHORITY.
    /// Uses Physics-based rotation (MoveRotation) so 'Forward' always updates correctly.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerAvatar : NetworkBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 4f;

        [Header("Camera Config")]
        public bool isFirstPerson = true;
        public Camera playerCamera;
        public Transform cameraRoot;

        [Header("Third Person Settings")]
        public float cameraDistance = 5f;
        public float cameraHeight = 2f;

        public Rigidbody rb;

        // Inputs passed from Controller
        private Vector2 _moveInput;
        private float _targetYaw;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Standard FPS Rigidbody Setup
            rb.freezeRotation = true; // We control rotation via script, not collisions
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth visuals
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {

                if (SimpleVivoxManager.Instance != null)
                {
                    SimpleVivoxManager.Instance.SetLocalPlayer(this.transform);
                }

                if (playerCamera != null)
                {
                    playerCamera.enabled = true;
                    playerCamera.gameObject.SetActive(true);
                    var listener = playerCamera.GetComponent<AudioListener>();
                    if (listener) listener.enabled = true;
                }
                // Sync initial yaw
                _targetYaw = transform.eulerAngles.y;


            }
            else
            {
                if (playerCamera != null)
                {
                    playerCamera.enabled = false;
                    playerCamera.gameObject.SetActive(false);
                }
                rb.isKinematic = true; // Remote players don't run physics
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner || playerCamera == null || cameraRoot == null) return;

            // Pin camera to head
            if (isFirstPerson)
            {
                if (playerCamera.transform.parent != cameraRoot)
                {
                    playerCamera.transform.SetParent(cameraRoot);
                    playerCamera.transform.localPosition = Vector3.zero;
                    playerCamera.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                // Third Person Fallback
                Vector3 targetPos = cameraRoot.position - cameraRoot.forward * cameraDistance + Vector3.up * cameraHeight;
                playerCamera.transform.position = targetPos;
                playerCamera.transform.rotation = cameraRoot.rotation;
            }
        }

        // --- INPUT METHODS (Called by PlayerInputController) ---

        public void SetInputs(Vector2 move, float yaw)
        {
            _moveInput = move;
            _targetYaw = yaw;
        }

        public void SetPitch(float pitch)
        {
            if (cameraRoot != null)
            {
                cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        // --- PHYSICS LOOP ---

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            // 1. Apply Rotation to Rigidbody
            Quaternion targetRotation = Quaternion.Euler(0f, _targetYaw, 0f);
            rb.MoveRotation(targetRotation);

            // 2. Calculate Move Direction based on that NEW rotation
            // This ensures 'Forward' is always aligned with where the mouse just turned
            Vector3 forward = targetRotation * Vector3.forward;
            Vector3 right = targetRotation * Vector3.right;

            Vector3 desiredMove = (forward * _moveInput.y + right * _moveInput.x).normalized;

            // 3. Apply Velocity
            // Preserve vertical velocity (gravity)
            float yVel = rb.linearVelocity.y;
            rb.linearVelocity = new Vector3(desiredMove.x * moveSpeed, yVel, desiredMove.z * moveSpeed);
        }
    }
}