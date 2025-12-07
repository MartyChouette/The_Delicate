using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// Makes the visible body lean/sway when the player looks left/right.
    /// Only applied on NON-owners, so local player camera doesn't get nauseated.
    /// </summary>
    public class PlayerBodyVisuals : NetworkBehaviour
    {
        [Header("References")]
        public Transform visualRoot;   // assign VisualRoot child
        public PlayerAvatar avatar;

        [Header("Sway Settings")]
        [Tooltip("Max lean angle in degrees when turning quickly.")]
        public float maxLeanAngle = 12f;

        [Tooltip("How fast the lean follows turn speed.")]
        public float leanResponsiveness = 6f;

        [Tooltip("How fast lean returns to neutral.")]
        public float leanReturnSpeed = 4f;

        private float _currentLean;    // -1..1
        private float _targetLean;
        private float _lastYaw;

        private void Awake()
        {
            if (avatar == null)
                avatar = GetComponent<PlayerAvatar>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (visualRoot == null && avatar != null)
            {
                // Try to auto-find a child named "VisualRoot"
                var child = transform.Find("VisualRoot");
                if (child != null)
                    visualRoot = child;
            }

            _lastYaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            // We only want to show this sway to OTHER players.
            if (!IsSpawned || IsOwner || visualRoot == null) return;

            float yaw = transform.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(_lastYaw, yaw);
            _lastYaw = yaw;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Turn speed in degrees/sec
            float turnSpeed = deltaYaw / dt;

            // Map turn speed to target lean (-1..1). Positive = leaning right.
            float normalized = Mathf.Clamp(turnSpeed / 180f, -1f, 1f); // tweak 180f for how snappy it feels
            _targetLean = normalized;

            // Smooth toward target
            _currentLean = Mathf.MoveTowards(_currentLean, _targetLean, leanResponsiveness * dt);

            // Also return toward 0 over time so they don't stay leaned forever.
            _currentLean = Mathf.MoveTowards(_currentLean, 0f, leanReturnSpeed * dt);

            float angle = _currentLean * maxLeanAngle;
            // Lean around Z axis (like a person tilting sideways when turning)
            Quaternion leanRot = Quaternion.Euler(0f, 0f, -angle);

            visualRoot.localRotation = leanRot;
        }
    }
}
