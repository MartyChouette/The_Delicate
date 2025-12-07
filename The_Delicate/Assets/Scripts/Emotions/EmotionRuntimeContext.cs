using System.Collections.Generic;
using EmotionBank;

public class EmotionRuntimeContext
{
    public List<EmotionDefinition> activeEmotions = new();
    public float fearLevel;        // 0–1, derived from those defs
    public float abandonmentLevel; // etc., optional aggregates
    public float angerLevel;
    public float denialLevel;
}