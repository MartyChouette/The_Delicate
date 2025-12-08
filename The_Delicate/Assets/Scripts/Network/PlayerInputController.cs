using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EmotionBank
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputController : NetworkBehaviour
    {
        [Header("References")]
        public PlayerAvatar avatar;
        public PlayerHandController handController;

        [Header("Ghost Fix")]
        [Tooltip("Drag the Camera inside this player prefab here.")]
        public Camera playerCamera;

        [Header("Look Settings")]
        public float lookSensitivity = 15f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        private PlayerInput _playerInput;
        private Vector2 _moveInput;
        private Vector2 _lookInput;

        private float _currentYaw;
        private float _currentPitch;

        // Double tap logic variables
        private double _lastLeftTapTime;
        private double _lastRightTapTime;
        private const double DoubleTapWindow = 0.25;

        private void Awake()
        {
            if (avatar == null) avatar = GetComponent<PlayerAvatar>();
            if (handController == null) handController = GetComponent<PlayerHandController>();

            _playerInput = GetComponent<PlayerInput>();
            // SAFETY: Disable input immediately on Awake so we don't move before the network is ready
            if (_playerInput != null) _playerInput.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();

            // Determines who controls this object
            if (IsOwner)
            {
                EnableInput();
            }
            else
            {
                DisableInput();
            }
        }

        private void EnableInput()
        {
            // 1. GHOST FIX: Enable Camera & Audio Listener for the OWNER only
            if (playerCamera != null) playerCamera.enabled = true;
            var listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = true;

            // 2. Enable Input System
            if (_playerInput == null) return;
            _playerInput.enabled = true;

            // Unsubscribe first to be safe, then subscribe
            _playerInput.onActionTriggered -= OnActionTriggered;
            _playerInput.onActionTriggered += OnActionTriggered;

            // Lock Cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Sync starting rotation so we don't snap
            _currentYaw = transform.eulerAngles.y;
        }

        private void DisableInput()
        {
            // 1. GHOST FIX: Disable Camera & Audio Listener for REMOTE players
            if (playerCamera != null) playerCamera.enabled = false;
            var listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;

            // 2. Disable Input System
            if (_playerInput == null) return;
            _playerInput.onActionTriggered -= OnActionTriggered;
            _playerInput.enabled = false;
        }

        private void OnActionTriggered(InputAction.CallbackContext ctx)
        {
            if (!IsOwner) return;

            // Ensure we have a hand controller before trying to use it
            if (handController == null) return;

            switch (ctx.action.name)
            {
                case "Move":
                    _moveInput = ctx.ReadValue<Vector2>();
                    break;
                case "Look":
                    _lookInput = ctx.ReadValue<Vector2>();
                    break;
                case "LeftHand":
                    HandleHandInput(ctx, HandSide.Left, ref _lastLeftTapTime);
                    break;
                case "RightHand":
                    HandleHandInput(ctx, HandSide.Right, ref _lastRightTapTime);
                    break;
                case "LockBothHands":
                    if (ctx.performed)
                    {
                        handController.ToggleLockHand(HandSide.Left);
                        handController.ToggleLockHand(HandSide.Right);
                    }
                    break;
                case "Toss":
                    if (ctx.performed) handController.RequestTossMagnet();
                    break;
            }
        }

        private void HandleHandInput(InputAction.CallbackContext ctx, HandSide side, ref double lastTapTime)
        {
            if (ctx.started)
            {
                double now = Time.timeAsDouble;
                // Double tap check
                if (now - lastTapTime < DoubleTapWindow)
                {
                    handController.ToggleLockHand(side);
                }
                else
                {
                    // Single press
                    handController.SetHandPressed(side, true);
                }
                lastTapTime = now;
            }
            else if (ctx.canceled)
            {
                handController.SetHandPressed(side, false);
            }
        }

        private void Update()
        {
            if (!IsOwner || avatar == null) return;

            // Calculate Rotation
            float dt = Time.deltaTime;
            if (_lookInput.sqrMagnitude > 0.0001f)
            {
                _currentYaw += _lookInput.x * lookSensitivity * dt;

                // Pitch (Camera only)
                _currentPitch -= _lookInput.y * lookSensitivity * dt;
                _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);
            }

            // Send to Avatar for Physics application
            avatar.SetInputs(_moveInput, _currentYaw);
            avatar.SetPitch(_currentPitch);
        }
    }
}