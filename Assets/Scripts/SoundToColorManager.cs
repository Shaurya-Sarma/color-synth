using UnityEngine;
using System.Collections.Generic;

/// Manages sound-triggered ripples and updates GPU textures for the ripple shader.
/// Uses a 1D texture per ripple property, allowing multiple ripples simultaneously.
public class SoundToColorManager : MonoBehaviour
{
    public static SoundToColorManager Instance;

    [Header("Ripple Settings")]
    public Material rippleMaterial; // ripple shader material
    public float rippleLifetime = 5f; // how long ripples live before removal
    private int maxRipples; // maximum number of simultaneous ripples [set in Awake()]

    // Internal storage for ripple events (CPU-side for reference)
    private List<RippleEvent> activeRipples = new();

    // Track which texture index each ripple is stored at
    private Dictionary<RippleEvent, int> rippleToIndex = new();

    // GPU textures to store ripple data
    private Texture2D rippleDataTex;    // RGBA: x, y, z, speed
    private Texture2D rippleTimeTex;    // RGBA: startTime, maxDist, fadeWidth, unused
    private Texture2D rippleColorTex;   // RGBA: r, g, b, unused

    private int nextRippleIndex = 0;    // circular buffer index

    void Awake()
    {
        Instance = this;
        maxRipples = 100;
        InitializeTextures();
        AssignTexturesToMaterialShader();
    }

    void InitializeTextures()
    {
        rippleDataTex = new Texture2D(maxRipples, 1, TextureFormat.RGBAFloat, false);
        rippleTimeTex = new Texture2D(maxRipples, 1, TextureFormat.RGBAFloat, false);
        rippleColorTex = new Texture2D(maxRipples, 1, TextureFormat.RGBAFloat, false);

        // Clear all texels
        for (int i = 0; i < maxRipples; i++)
        {
            ClearTextureSlot(i);
        }

        // finished changing pixels on CPU side -> send to GPU
        rippleDataTex.Apply(false, false);
        rippleTimeTex.Apply(false, false);
        rippleColorTex.Apply(false, false);
    }

    /// Clears a specific texture slot
    void ClearTextureSlot(int index)
    {
        rippleDataTex.SetPixel(index, 0, Color.clear);
        rippleTimeTex.SetPixel(index, 0, Color.clear);
        rippleColorTex.SetPixel(index, 0, Color.clear);
    }

    /// Assigns textures to the shader material
    void AssignTexturesToMaterialShader()
    {
        rippleMaterial.SetTexture("_RippleDataTex", rippleDataTex);
        rippleMaterial.SetTexture("_RippleTimeTex", rippleTimeTex);
        rippleMaterial.SetTexture("_RippleColorTex", rippleColorTex);
        rippleMaterial.SetFloat("_MaxRipples", (float)maxRipples);
    }

    /// Emits a new ripple at a position with given volume and frequency 
    public void EmitRipple(RippleEvent ripple)
    {
        // Debug.Log("Active Ripples: " + activeRipples.Count);

        // Keep within limit
        if (activeRipples.Count >= maxRipples)
        {
            Debug.LogWarning("Max ripples exceeded, removing oldest ripple." + maxRipples);
            RippleEvent oldestRipple = activeRipples[0];
            RemoveRipple(oldestRipple);
        }

        activeRipples.Add(ripple);
        rippleToIndex[ripple] = nextRippleIndex;

        // Write ripple data to GPU textures
        WriteRippleToTextures(ripple, nextRippleIndex);

        // Advance index (circular)
        nextRippleIndex = (nextRippleIndex + 1) % maxRipples;
    }

    /// Packs the ripple's data into 3 textures and uploads to the GPU.
    void WriteRippleToTextures(RippleEvent ripple, int index)
    {
        // RippleDataTex: position (xyz) + speed
        rippleDataTex.SetPixel(index, 0,
            new Color(ripple.position.x, ripple.position.y, ripple.position.z, ripple.speed));

        // RippleTimeTex: startTime, maxDist, fadeWidth, timbre
        rippleTimeTex.SetPixel(index, 0,
            new Color(ripple.startTime, ripple.maxDistance, ripple.fadeWidth, ripple.timbre));

        // RippleColorTex: color RGB
        rippleColorTex.SetPixel(index, 0,
            new Color(ripple.color.r, ripple.color.g, ripple.color.b, 1));

        // finished changing pixels on CPU side -> send to GPU
        rippleDataTex.Apply(false, false);
        rippleTimeTex.Apply(false, false);
        rippleColorTex.Apply(false, false);
    }

    /// Removes a ripple and clears its GPU texture slot
    void RemoveRipple(RippleEvent ripple)
    {
        if (rippleToIndex.TryGetValue(ripple, out int index))
        {
            // Clear the GPU texture slot
            ClearTextureSlot(index);

            // Apply changes to GPU
            rippleDataTex.Apply(false, false);
            rippleTimeTex.Apply(false, false);
            rippleColorTex.Apply(false, false);

            // Remove from tracking
            rippleToIndex.Remove(ripple);
        }

        activeRipples.Remove(ripple);
    }

    void Update()
    {
        // Remove ripples older than rippleLifetime seconds
        // Use a copy to avoid modifying list during iteration
        var ripplesToRemove = activeRipples.FindAll(r => Time.time - r.startTime > rippleLifetime + 10f);
        foreach (var ripple in ripplesToRemove)
        {
            RemoveRipple(ripple);
        }

    }

    public List<RippleEvent> GetActiveRipples() => activeRipples;
}