// EmotionDefinition.cs
using UnityEngine;

namespace EmotionBank
{
    [CreateAssetMenu(menuName = "EmotionBank/Emotion Definition")]
    public class EmotionDefinition : ScriptableObject
    {
        [Header("Identity")]
        public EmotionType emotionType;
        public Color uiColor = Color.white;

        [Header("Affects Players?")]
        public bool affectsPlayers = true;

        [Tooltip("Max camera jitter in degrees at intensity=1.")]
        public float maxCameraJitterDeg = 2f;

        [Tooltip("Max hand jitter offset in units at intensity=1.")]
        public float maxHandJitter = 0.05f;

        [Tooltip("Max body tilt in degrees at intensity=1.")]
        public float maxBodyTiltDeg = 6f;

        [Tooltip("Chance per second of a stumble impulse at intensity=1.")]
        public float stumbleChancePerSecond = 0.3f;

        [Header("Affects Box?")]
        public bool affectsBox = false;

        [Tooltip("Base drift strength tugging the box toward exits or away (units/sec^2).")]
        public float baseDriftStrength = 3f;

        [Tooltip("Random impulse strength for jittery box (anger/denial).")]
        public float randomImpulseStrength = 2f;

        [Tooltip("How often the box phases/hiccups (for denial), in seconds at intensity=1.")]
        public float phaseIntervalSeconds = 3f;

        [Header("Intensity Tuning")]
        [Tooltip("How quickly intensity moves toward target [0-1].")]
        public float intensityLerpSpeed = 1f;
    }
}