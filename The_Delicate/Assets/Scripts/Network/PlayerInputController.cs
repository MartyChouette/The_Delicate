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
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();

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
            if (_playerInput == null) return;
            _playerInput.enabled = true;
            _playerInput.onActionTriggered -= OnActionTriggered;
            _playerInput.onActionTriggered += OnActionTriggered;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Sync starting rotation so we don't snap
            _currentYaw = transform.eulerAngles.y;
        }

        private void DisableInput()
        {
            if (_playerInput == null) return;
            _playerInput.onActionTriggered -= OnActionTriggered;
            _playerInput.enabled = false;
        }

        private void OnActionTriggered(InputAction.CallbackContext ctx)
        {
            if (!IsOwner) return;

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
                    if (ctx.performed) handController.ToggleLockBothHands();
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
                if (now - lastTapTime < DoubleTapWindow) handController.ToggleLockHand(side);
                lastTapTime = now;
                handController.SetHandPressed(side, true);
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