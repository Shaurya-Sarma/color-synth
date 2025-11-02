using System.Collections.Generic;
using UnityEngine;

// Singleton to handle playing audio clips at specific positions with volume and pitch control
// keeps a pool of available AudioSources to optimize performance
// when a clip is requested to play, reuses an available AudioSource or creates a new one if needed
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private List<AudioSource> sources = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayClip(AudioClip clip, Vector3 position, float volume, float pitch = 1f, float dopplerLevel = 0f)
    {
        if (clip == null) return;

        AudioSource src = GetAvailableSource();
        src.transform.position = position;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 1f; // 3D sound
        src.minDistance = 0.5f;
        src.maxDistance = 15f;
        src.dopplerLevel = dopplerLevel; // based on velocity of source/listener (default is stationary objects)
        src.rolloffMode = AudioRolloffMode.Logarithmic;

        src.PlayOneShot(clip);
    }

    private AudioSource GetAvailableSource()
    {
        // Reuse available source if one isn't playing
        foreach (var s in sources)
        {
            if (!s.isPlaying)
                return s;
        }

        // Otherwise, make a new one
        AudioSource newSrc = new GameObject("PooledAudioSource").AddComponent<AudioSource>();
        newSrc.transform.parent = transform;
        sources.Add(newSrc);
        return newSrc;
    }
}
