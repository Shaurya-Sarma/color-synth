using UnityEngine;

/// Singleton that receives all interaction events (discrete vs continuous)
/// and translates them into audio + ripple events with appropriately
/// adjusted parameters to create procedural sound and visual effects.
public class InteractionInterpreter : MonoBehaviour
{
    public static InteractionInterpreter Instance;

    [Header("Audio Clips by Material")]
    public AudioClip[] glassHitClips;
    public AudioClip[] metalHitClips;
    public AudioClip[] woodHitClips;
    public AudioClip[] clothHitClips;
    public AudioClip[] plasticHitClips;

    [Header("Ripple Parameters")]
    public float minRippleRadius = 0.1f;
    public float maxRippleRadius = 2f;
    public float minRippleSpeed = 0.5f;
    public float maxRippleSpeed = 3f;
    public float minFadeWidth = 0.2f;
    public float maxFadeWidth = 1f;

    [Header("Volume Scaling")]
    public float minVolume = 0.1f;
    public float maxVolume = 1f;

    void Awake()
    {
        if (Instance != null) Destroy(this);
        else Instance = this;
    }

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
        AudioClip clip = SelectClip(material);

        // Volume scales with impact energy & distance
        float volume = Mathf.Clamp(impactEnergy / 10f, minVolume, maxVolume);
        volume *= Mathf.Clamp01(1f - (distanceToListener / 10f)); //TODO have it so that too far away is silent and therefore not generating ripples

        // Ripple parameters
        float rippleRadius = Mathf.Lerp(minRippleRadius, maxRippleRadius, impactEnergy / 10f);
        float rippleSpeed = Mathf.Lerp(minRippleSpeed, maxRippleSpeed, impactEnergy / 10f);
        float fadeWidth = Mathf.Lerp(minFadeWidth, maxFadeWidth, slipSpeed + spinSpeed);

        // Construct ripple event (color handled by manager)
        // TODO let this script handle color as well (need the frequency color map)
        RippleEvent ripple = new RippleEvent(position, Color.white, rippleSpeed, rippleRadius, fadeWidth);

        // Emit to SoundToColorManager
        // SoundToColorManager.Instance.EmitSoundEvent(ripple, clip, volume);
    }

    // -----------------------
    // CONTINUOUS COLLISIONS
    // -----------------------
    public void ProcessContinuousCollisions(
        Vector3 position,
        float velocity,
        float angularVelocity,
        MaterialType material
    )
    {
        // AudioClip clip = SelectClip(material);

        // float volume = Mathf.Clamp(velocity / 5f, minVolume, maxVolume);
        // float rippleRadius = Mathf.Lerp(minRippleRadius, maxRippleRadius, velocity / 5f);
        // float rippleSpeed = Mathf.Lerp(minRippleSpeed, maxRippleSpeed, velocity / 5f);
        // float fadeWidth = Mathf.Lerp(minFadeWidth, maxFadeWidth, angularVelocity / 10f);

        // RippleEvent ripple = new RippleEvent(position, Color.white, rippleSpeed, rippleRadius, fadeWidth);

        // Emit as continuous
        // SoundToColorManager.Instance.EmitRipple(ripple, clip, volume, continuous: true);
    }


    // select an audio clip based on material type
    // TODO could add layered randomization (e.g. pitch variation) for proceduralism
    private AudioClip SelectClip(MaterialType material)
    {
        AudioClip[] clips = material switch
        {
            MaterialType.Glass => glassHitClips,
            MaterialType.Metal => metalHitClips,
            MaterialType.Wood => woodHitClips,
            MaterialType.Cloth => clothHitClips,
            MaterialType.Plastic => plasticHitClips,
            _ => woodHitClips
        };

        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    // TODO implmement method for unique interactions with increased priority to basically override generic physics-based ones
    // e.g. user touches object, special scripted event, etc.

    // TODO maintain a priority queue of interaction events to process in order of importance
    // throw out low-priority events if too many are queued up
    // cell-based spatial partitioning to avoid overlapping ripples from nearby interactions -> divide scene into grid of cells and limit to one ripple per cell per frame?
    // using priority queue, send ripple events to SoundToColorManager in order of priority
    // all ripple information should be calculated within this file, SoundToColorManager just handles sending to GPU

}
