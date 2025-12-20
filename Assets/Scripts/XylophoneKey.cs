using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class XylophoneKey : MonoBehaviour
{
    [Header("Key Setup")]
    [Tooltip("Index of this key (0 = C, 6 = B)")]
    [Range(0, 6)]
    public int keyIndex = 0;

    [Tooltip("Unique audio clip for this note")]
    public AudioClip clip;

    [Tooltip("Base volume for this key")]
    public float baseVolume = 8f;

    [Header("Impact Settings")]
    public float minImpactVelocity = 0.2f;
    public float maxImpactVelocity = 5f;
    public float cooldown = 0.08f;

    [Header("Ripple Properties (Read-Only)")]
    [Range(0f, 1f)] public float colorPosition;
    public float noiseScale;
    public float noiseStrength;

    private float lastTriggerTime = -999f;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        MapRippleProperties();
    }

    void MapRippleProperties()
    {
        // Map ripple properties linearly across keys
        float t = keyIndex / 6f; // 7 keys total
        colorPosition = t;
        noiseScale = Mathf.Lerp(0.7f, 2.8f, t);
        noiseStrength = Mathf.Lerp(0.2f, 0.4f, t);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastTriggerTime < cooldown) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactVelocity) return;

        lastTriggerTime = Time.time;

        float normalizedEnergy = Mathf.InverseLerp(
            minImpactVelocity, maxImpactVelocity, impactSpeed
        );

        Vector3 hitPoint = collision.contacts[0].point;
        Emit(hitPoint, normalizedEnergy);
    }

    void Emit(Vector3 position, float normalizedEnergy)
    {
        if (clip == null) return;

        // --------------------
        // Audio
        // --------------------
        float volume = Mathf.Lerp(0.1f, 1f, normalizedEnergy) * baseVolume;
        AudioManager.Instance.PlayClip(clip, position, volume, 1f); // fixed pitch

        // --------------------
        // Color
        // --------------------
        FrequencyColorMap freqMap = new FrequencyColorMap();
        Color rippleColor = freqMap.evaluate(colorPosition);

        // --------------------
        // Ripple
        // --------------------
        float speed = Mathf.Lerp(0.2f, 1.25f, normalizedEnergy);
        float maxDist = Mathf.Lerp(0.1f, 2f, normalizedEnergy);

        RippleEvent ripple = new RippleEvent(
            position,
            rippleColor,
            speed,
            maxDist,
            fadeWidth: 0f,
            noiseScale: noiseScale,
            noiseStrength: noiseStrength,
            continuous: false
        );

        SoundToColorManager.Instance.EmitRipple(ripple);
    }
}
