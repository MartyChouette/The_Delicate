using Unity.Netcode;
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// Runtime emotion state for an entity (player or box).
    /// Intensities 0-1 for Fear, Abandonment, Denial, Anger.
    /// DISTRIBUTED VERSION: Owner writes, everyone reads.
    /// </summary>
    public class EmotionState : NetworkBehaviour
    {
        [Header("Which emotions are relevant for this entity?")]
        public EmotionDefinition[] emotionDefinitions;

        // CRITICAL CHANGE: WritePermission must be Owner for Distributed Authority
        public NetworkVariable<float> fearIntensity = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<float> abandonmentIntensity = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<float> denialIntensity = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<float> angerIntensity = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Lookups for config
        private EmotionDefinition _fearDef;
        private EmotionDefinition _abandonDef;
        private EmotionDefinition _denialDef;
        private EmotionDefinition _angerDef;

        public EmotionDefinition FearDef => _fearDef;
        public EmotionDefinition AbandonmentDef => _abandonDef;
        public EmotionDefinition DenialDef => _denialDef;
        public EmotionDefinition AngerDef => _angerDef;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Cache defs by type (optional, but convenient)
            foreach (var def in emotionDefinitions)
            {
                if (def == null) continue;
                switch (def.emotionType)
                {
                    case EmotionType.Fear: _fearDef = def; break;
                    case EmotionType.Abandonment: _abandonDef = def; break;
                    case EmotionType.Denial: _denialDef = def; break;
                    case EmotionType.Anger: _angerDef = def; break;
                }
            }
        }

        // Helpers to lerp toward a target intensity
        // CHANGED: We check IsOwner instead of IsServer

        public void LerpFear(float target, float dt)
        {
            if (!IsOwner || _fearDef == null) return;
            fearIntensity.Value = Mathf.MoveTowards(
                fearIntensity.Value,
                Mathf.Clamp01(target),
                _fearDef.intensityLerpSpeed * dt);
        }

        public void LerpAbandonment(float target, float dt)
        {
            if (!IsOwner || _abandonDef == null) return;
            abandonmentIntensity.Value = Mathf.MoveTowards(
                abandonmentIntensity.Value,
                Mathf.Clamp01(target),
                _abandonDef.intensityLerpSpeed * dt);
        }

        public void LerpDenial(float target, float dt)
        {
            if (!IsOwner || _denialDef == null) return;
            denialIntensity.Value = Mathf.MoveTowards(
                denialIntensity.Value,
                Mathf.Clamp01(target),
                _denialDef.intensityLerpSpeed * dt);
        }

        public void LerpAnger(float target, float dt)
        {
            if (!IsOwner || _angerDef == null) return;
            angerIntensity.Value = Mathf.MoveTowards(
                angerIntensity.Value,
                Mathf.Clamp01(target),
                _angerDef.intensityLerpSpeed * dt);
        }

        // Generic getter (useful for visuals)
        public float GetIntensity(EmotionType type)
        {
            return type switch
            {
                EmotionType.Fear => fearIntensity.Value,
                EmotionType.Abandonment => abandonmentIntensity.Value,
                EmotionType.Denial => denialIntensity.Value,
                EmotionType.Anger => angerIntensity.Value,
                _ => 0f
            };
        }
    }
}