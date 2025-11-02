using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InteractionInterpreter : MonoBehaviour
{
    public static InteractionInterpreter Instance;

    [Header("Ripple Parameters")]
    public float minRippleRadius = 0.1f;
    public float maxRippleRadius = 2f;
    public float minRippleSpeed = 0.2f;
    public float maxRippleSpeed = 1.25f;
    public float minFadeWidth = 0.0f;
    public float maxFadeWidth = 0.05f;
    public float maxEnergyThreshold = 24f; // used to normalize impact energy
    public FrequencyColorMap frequencyToColor = new FrequencyColorMap();

    [Header("Volume Scaling")]
    public float minVolume = 0.1f;
    public float maxVolume = 1f;
    public float pitchVariation = 0.25f;

    [Header("Interaction Clips")]
    public string interactionClipsFolder = "Assets/InteractionClips";
    public List<InteractionClipSO> interactionClips;

    private Dictionary<MaterialType, List<InteractionClipSO>> materialClips = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadMaterialClips();
    }

    private void LoadMaterialClips()
    {
        materialClips.Clear();
        foreach (var iclip in interactionClips)
        {
            if (!materialClips.ContainsKey(iclip.materialType))
                materialClips[iclip.materialType] = new List<InteractionClipSO>();
            materialClips[iclip.materialType].Add(iclip);
        }
    }

    // Select a random clip for the given material type
    private InteractionClipSO SelectClip(MaterialType material)
    {
        if (!materialClips.ContainsKey(material) || materialClips[material].Count == 0)
            return null;
        var clips = materialClips[material];
        return clips[Random.Range(0, clips.Count)];
    }

#if UNITY_EDITOR
    // Button in inspector to auto-load all InteractionClipSO assets
    [ContextMenu("Auto-Load All InteractionClips")]
    public void AutoLoadInteractionClips()
    {
        interactionClips.Clear();

        string[] guids = AssetDatabase.FindAssets("t:InteractionClipSO", new[] { interactionClipsFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            InteractionClipSO clip = AssetDatabase.LoadAssetAtPath<InteractionClipSO>(path);
            if (clip != null)
            {
                interactionClips.Add(clip);
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"<color=green>Auto-loaded {interactionClips.Count} InteractionClipSO assets from {interactionClipsFolder}</color>");
    }
#endif

    // -----------------------
    // DISCRETE COLLISIONS
    // -----------------------
    public void ProcessDiscreteCollision(
      Vector3 position,
      float impactEnergy,
      float slipSpeed,
      float spinSpeed,
      float contactRadius,
      float bounciness,
      MaterialType material,
      float distanceToListener
  )
    {
        InteractionClipSO iclip = SelectClip(material);
        if (iclip == null) return;

        // Normalize impact energy
        float normalizedEnergy = Mathf.InverseLerp(0f, maxEnergyThreshold, impactEnergy);

        // Volume scales with impact energy & distance
        float volume = Mathf.Lerp(minVolume, maxVolume, normalizedEnergy);
        // scale volume based on distance to listener (1 = nearby, 0 = far)
        volume *= Mathf.Clamp01(1f - (distanceToListener / 10f));
        if (volume < 0.05f) return; // too quiet → ignore

        // Slight pitch variation based on physics        
        float pitchOffset = Mathf.Lerp(-pitchVariation, pitchVariation, normalizedEnergy);
        float colorPitch = iclip.basePitch + pitchOffset; // for ripple color evaluation
        float audioPitch = 1f + pitchOffset;             // for actual audio playback

        // Estimate timbre for ripple noise scale (rough heuristic by material type) 
        /// have more timber (more jagged-glassy) equal to more noisy ripples 
        //(e.g., wood = smoother ripples, glass = more noisy ripples)
        float timbre = EstimateTimbre(material, slipSpeed, spinSpeed);
        float timbreNoiseScale = Mathf.Lerp(0.1f, 2.5f, timbre);

        // Construct ripple event
        Color color = frequencyToColor.evaluate(colorPitch);
        float speed = Mathf.Lerp(minRippleSpeed, maxRippleSpeed, normalizedEnergy);
        float maxDist = Mathf.Lerp(minRippleRadius, maxRippleRadius, normalizedEnergy);
        float fadeWidth = 0f;

        RippleEvent ripple = new RippleEvent(position, color, speed, maxDist, fadeWidth, timbreNoiseScale, continuous: false);

        // Emit to SoundToColorManager        
        SoundToColorManager.Instance.EmitRipple(ripple);
        // Call AudioManager to play sound
        AudioManager.Instance.PlayClip(iclip.clip, position, volume, audioPitch);

        Debug.Log("Processed discrete collision: " +
            $"Material={material}, Energy={impactEnergy:F2}, Volume={volume:F2}, Pitch={audioPitch:F2}, " +
            $"Timbre={timbreNoiseScale:F2}, RippleSpeed={speed:F2}, MaxDist={maxDist:F2}");
    }


    // -----------------------
    // CONTINUOUS COLLISIONS
    // -----------------------
    public void ProcessContinuousCollision(
        Vector3 position,
        float velocity,
        float angularVelocity,
        MaterialType material
    )
    {
        InteractionClipSO iclip = SelectClip(material);
        if (iclip == null) return;

        // float volume = Mathf.Clamp(velocity / 5f, minVolume, maxVolume);
        // if (volume < 0.05f) return;

        // float pitch = iclip.basePitch + Mathf.Clamp(velocity / 10f, -0.1f, 0.1f);
        // float timbre = EstimateTimbre(material, velocity, angularVelocity);

        // // Construct ripple event
        // Color color = frequencyToColor.evaluate(pitch);
        // float speed = Mathf.Lerp(minRippleSpeed, maxRippleSpeed, impactEnergy / 10f);
        // float maxDist = Mathf.Lerp(0.5f, 2f, volume);
        // float fadeWidth = Mathf.Lerp(minFadeWidth, maxFadeWidth, slipSpeed + spinSpeed);

        // RippleEvent ripple = new RippleEvent(position, color, speed, maxDist, fadeWidth, timbre, continuous: false);

        // SoundToColorManager.Instance.EmitRipple(ripple);
        // AudioManager.Instance.PlayClip(iclip.clip, position, volume, pitch);
    }

    // -----------------------
    // TIMBRE HEURISTIC
    // -----------------------
    private float EstimateTimbre(MaterialType material, float velocity, float spinSpeed)
    {
        // rough example: metallic/glass = jagged (high noise), wood = smoother
        float baseTimbre = material switch
        {
            MaterialType.Glass => 0.9f,
            MaterialType.Metal => 0.8f,
            MaterialType.Wood => 0.4f,
            MaterialType.Cloth => 0.2f,
            MaterialType.Plastic => 0.3f,
            _ => 0.5f
        };
        ;
        return baseTimbre;
    }
}