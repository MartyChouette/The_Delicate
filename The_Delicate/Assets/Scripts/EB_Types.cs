// EB_Types.cs
using UnityEngine;

namespace EmotionBank
{
    /// <summary>
    /// Different emotions that can be active in the session.
    /// This is data-driven via EmotionDefinition assets.
    /// </summary>
    public enum EmotionType
    {
        Fear,
        Abandonment,
        Denial,
        Anger
        // Add more later if needed.
    }

    /// <summary>
    /// The different magnet "words" that can exist as physical tiles.
    /// </summary>
    public enum MagnetWordId
    {
        Warmth,
        Help,
        Sorry,
        Stay,
        Safe,
        Together,
        Quiet,
        Please,
        HoldMe,
        DontLeave
        // Add more later.
    }

    /// <summary>
    /// Which hand is being used.
    /// </summary>
    public enum HandSide
    {
        Left,
        Right
    }
}