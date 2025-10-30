using UnityEngine;
using System.Collections.Generic;

public class SoundToColorManager : MonoBehaviour
{
    public static SoundToColorManager Instance; // singleton instance
    public FrequencyColorMap frequencyToColor = new FrequencyColorMap(); // custom frequency-color mapping
    public int maxRipples = 16;
    public Material rippleMaterial; // ripple shader

    private List<RippleEvent> activeRipples = new();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // remove expired ripples 
        activeRipples.RemoveAll(r => Time.time - r.startTime > 5f);
        SendDataToShader();
    }

    public void EmitSoundEvent(Vector3 pos, float volume, float freqHint)
    {
        Color col = frequencyToColor.evaluate(freqHint);

        RippleEvent ripple = new RippleEvent(
            pos: pos,
            col: col,
            spd: Mathf.Lerp(.1f, .7f, volume), // speed based on volume
            maxDist: Mathf.Lerp(.5f, 1f, volume),
            fade: 0.0f  // edge softness
        );

        activeRipples.Add(ripple);

        if (activeRipples.Count > maxRipples)
            activeRipples.RemoveAt(0);
    }


    void SendDataToShader()
    {
        if (!rippleMaterial) return;

        if (activeRipples.Count == 0)
        {
            rippleMaterial.SetFloat("_RippleRadius", -1f);
            return;
        }

        RippleEvent ripple = activeRipples[^1]; // last ripple
        float elapsedTime = Time.time - ripple.startTime;
        float currentRadius = elapsedTime * ripple.speed * .825f;
        Debug.Log("Current Ripple Radius: " + currentRadius);

        // Clamp it to avoid infinite growth
        float clampedRadius = Mathf.Min(currentRadius, ripple.maxDistance);

        // Fade intensity as it expands
        float t = Mathf.Clamp01(currentRadius / ripple.maxDistance);

        float fadeoutFactor;
        if (t < 0.8f)
        {
            // stay fully visible until 80%
            fadeoutFactor = 1f;
        }
        else
        {
            // linearly fade from 1 → 0 as t goes 0.8 → 1.0
            fadeoutFactor = Mathf.Lerp(1f, 0f, (t - 0.8f) / 0.2f);
        }

        rippleMaterial.SetVector("_RippleOrigin", ripple.position);
        rippleMaterial.SetColor("_RippleColor", ripple.color);
        rippleMaterial.SetFloat("_RippleRadius", clampedRadius);
        rippleMaterial.SetFloat("_FadeoutFactor", fadeoutFactor);
        rippleMaterial.SetFloat("_FadeWidth", ripple.fadeWidth);

    }


    public List<RippleEvent> GetActiveRipples() => activeRipples;
}
