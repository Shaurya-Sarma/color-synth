using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Singleton to handle playing audio clips at specific positions with volume and pitch control
// keeps a pool of available AudioSources to optimize performance
// when a clip is requested to play, reuses an available AudioSource or creates a new one if needed
// also loops through shuffles those ambient tracks with crossfading
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private List<AudioSource> sources = new();

    // Dedicated music/ambient sources for crossfading
    private AudioSource ambientSourceA;
    private AudioSource ambientSourceB;

    private bool isSourceAPlaying = true;

    [Header("Ambient Settings")]
    public List<AudioClip> ambientTracks = new();
    public float crossfadeDuration = 2f;
    public float ambientVolume = 0.5f;
    private Queue<AudioClip> ambientQueue = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create two audio sources for crossfading
        ambientSourceA = CreateAmbientSource("AmbientSourceA");
        ambientSourceB = CreateAmbientSource("AmbientSourceB");

        // Initialize queue with random order
        ResetAmbientQueue();

        // Start the ambient loop
        if (ambientQueue.Count > 0)
            StartCoroutine(PlayAmbientLoop());
    }

    private AudioSource CreateAmbientSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = transform;
        AudioSource source = go.AddComponent<AudioSource>();
        source.loop = false;       // we'll handle looping manually
        source.spatialBlend = 0;   // 2D
        source.volume = ambientVolume;
        return source;
    }

    private void ResetAmbientQueue()
    {
        List<AudioClip> shuffled = new List<AudioClip>(ambientTracks);
        // Shuffle list randomly
        for (int i = 0; i < shuffled.Count; i++)
        {
            int r = Random.Range(i, shuffled.Count);
            AudioClip temp = shuffled[i];
            shuffled[i] = shuffled[r];
            shuffled[r] = temp;
        }

        ambientQueue = new Queue<AudioClip>(shuffled);
    }

    private IEnumerator PlayAmbientLoop()
    {
        while (true)
        {
            if (ambientQueue.Count == 0)
                ResetAmbientQueue();

            AudioClip nextTrack = ambientQueue.Dequeue();

            AudioSource currentSource = isSourceAPlaying ? ambientSourceA : ambientSourceB;
            AudioSource nextSource = isSourceAPlaying ? ambientSourceB : ambientSourceA;

            nextSource.clip = nextTrack;
            nextSource.volume = 0f;
            nextSource.Play();

            // Crossfade to next track
            float t = 0f;
            while (t < crossfadeDuration)
            {
                t += Time.deltaTime;
                float normalized = t / crossfadeDuration;
                currentSource.volume = Mathf.Lerp(ambientVolume, 0f, normalized);
                nextSource.volume = Mathf.Lerp(0f, ambientVolume, normalized);
                yield return null;
            }

            currentSource.volume = 0f;
            nextSource.volume = ambientVolume;
            currentSource.Stop();

            isSourceAPlaying = !isSourceAPlaying;

            // Wait until next track is almost done before starting next crossfade
            yield return new WaitForSeconds(nextTrack.length - crossfadeDuration);
        }
    }

    // play interaction sound at position with volume and pitch in 3D space
    #region Sound Effects
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
        src.dopplerLevel = dopplerLevel;
        src.rolloffMode = AudioRolloffMode.Logarithmic;

        src.PlayOneShot(clip);
    }

    private AudioSource GetAvailableSource()
    {
        foreach (var s in sources)
        {
            if (!s.isPlaying)
                return s;
        }

        AudioSource newSrc = new GameObject("PooledAudioSource").AddComponent<AudioSource>();
        newSrc.transform.parent = transform;
        sources.Add(newSrc);
        return newSrc;
    }
    #endregion
}
