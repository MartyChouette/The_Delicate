using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    
    public class PlayerEmotionVisuals : NetworkBehaviour
    {
        public PlayerAvatar avatar;
        public EmotionState emotionState;
        public PlayerHandController handController;

        [Header("Camera Jitter")]
        public float jitterFrequency = 10f;

        [Header("Hand Jitter")]
        public float handJitterFrequency = 12f;

        private float _camNoiseTime;
        private float _handNoiseTime;

        private Vector3 _baseCamLocalPos;

        private void Awake()
        {
            if (avatar == null)
                avatar = GetComponent<PlayerAvatar>();
            if (emotionState == null)
                emotionState = GetComponent<EmotionState>();
            if (handController == null)
                handController = GetComponent<PlayerHandController>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner && avatar.playerCamera != null)
            {
                _baseCamLocalPos = avatar.playerCamera.transform.localPosition;
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner || emotionState == null || avatar.playerCamera == null) return;

            float fear = emotionState.fearIntensity.Value;
            if (emotionState.FearDef == null) fear = 0f;

            // Camera jitter
            _camNoiseTime += Time.deltaTime * jitterFrequency;
            float camJitterDeg = (emotionState.FearDef != null ? emotionState.FearDef.maxCameraJitterDeg : 0f) * fear;

            float yawOffset = (Mathf.PerlinNoise(_camNoiseTime, 0.1f) - 0.5f) * 2f * camJitterDeg;
            float pitchOffset = (Mathf.PerlinNoise(0.1f, _camNoiseTime) - 0.5f) * 2f * camJitterDeg;

            Vector3 camEuler = avatar.playerCamera.transform.localEulerAngles;
            float pitch = camEuler.x;
            if (pitch > 180f) pitch -= 360f;
            pitch += pitchOffset;
            camEuler.x = pitch;
            camEuler.y += yawOffset;
            avatar.playerCamera.transform.localEulerAngles = camEuler;

            // Small positional shake
            Vector3 camPos = _baseCamLocalPos;
            float shakeMagnitude = 0.02f * fear;
            camPos.x += (Mathf.PerlinNoise(_camNoiseTime, 1.23f) - 0.5f) * 2f * shakeMagnitude;
            camPos.y += (Mathf.PerlinNoise(2.34f, _camNoiseTime) - 0.5f) * 2f * shakeMagnitude;
            avatar.playerCamera.transform.localPosition = camPos;

            // Hand jitter
            if (handController != null && handController.leftHand != null && handController.rightHand != null)
            {
                _handNoiseTime += Time.deltaTime * handJitterFrequency;
                float maxHandJitter = (emotionState.FearDef != null ? emotionState.FearDef.maxHandJitter : 0f) * fear;

                ApplyHandJitter(handController.leftHand, maxHandJitter, 10.3f);
                ApplyHandJitter(handController.rightHand, maxHandJitter, 20.7f);
            }
        }

        private void ApplyHandJitter(Transform hand, float mag, float seed)
        {
            if (hand == null) return;
            Vector3 pos = hand.localPosition;
            pos.x += (Mathf.PerlinNoise(_handNoiseTime, seed) - 0.5f) * 2f * mag;
            pos.y += (Mathf.PerlinNoise(seed, _handNoiseTime) - 0.5f) * 2f * mag;
            hand.localPosition = pos;
        }
    }
}
