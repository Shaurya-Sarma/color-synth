using UnityEngine;

// attach to any object making sound
// transmits message to SoundToColorManager to create ripple effects
// passes important sound parameters

public class SoundEmitter : MonoBehaviour
{
    public AudioClip clip;

    [Range(0f, 1f)] public float baseVolume = 0.8f;
    public float frequencyHint = 0.5f; // 0 = low, 1 = high (frequency analysis TBA...)
                                       // lower frequencyHint values (bass) correspond to colors 
                                       // towards the blue end of the spectrum

    private AudioSource audioSrc;


    void Awake()
    {
        audioSrc = GetComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 1f; // enable 3D sound
    }

    public void PlaySound()
    {
        if (clip == null) return;
        audioSrc.PlayOneShot(clip, baseVolume);

        // notify manager
        SoundToColorManager.Instance.EmitSoundEvent(
            transform.position,
            baseVolume,
            frequencyHint
        );
    }

    // Optionally trigger sound on collision
    private void OnCollisionEnter(Collision collision)
    {
        PlaySound();
    }
}
