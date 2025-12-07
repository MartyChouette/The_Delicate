using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// DISTRIBUTED AUTHORITY VERSION
    /// On the owner, raycasts from the camera to see what they're looking at.
    /// Directly sets the focus point on the local HandController (no RPCs).
    /// </summary>
    [RequireComponent(typeof(PlayerAvatar))]
    public class PlayerFocusTarget : NetworkBehaviour
    {
        public PlayerAvatar avatar;
        public PlayerHandController handController;

        [Header("Focus Settings")]
        public float maxFocusDistance = 5f;
        public LayerMask focusMask; // usually same as grabMask
        public float updateInterval = 0.05f;

        private float _timer;

        private void Awake()
        {
            if (avatar == null)
                avatar = GetComponent<PlayerAvatar>();
            if (handController == null)
                handController = GetComponent<PlayerHandController>();
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (avatar.playerCamera == null || handController == null) return;

            _timer += Time.deltaTime;
            if (_timer < updateInterval)
                return;
            _timer = 0f;

            Ray ray = new Ray(avatar.playerCamera.transform.position, avatar.playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxFocusDistance, focusMask, QueryTriggerInteraction.Ignore))
            {
                // DISTRIBUTED CHANGE: Call local method directly
                handController.SetFocusPoint(hit.point, true);
            }
            else
            {
                // DISTRIBUTED CHANGE: Call local method directly
                handController.SetFocusPoint(Vector3.zero, false);
            }
        }
    }
}