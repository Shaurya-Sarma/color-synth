using UnityEngine;
using UnityEditor;
using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;

public class InteractionClipGenerator : EditorWindow
{
    [MenuItem("Audio/Generate InteractionClip Assets")]
    static void ShowWindow()
    {
        GetWindow<InteractionClipGenerator>("InteractionClip Generator");
    }

    public string audioFolder = "Assets/Audio";
    public string outputFolder = "Assets/InteractionClips";

    [Header("Pitch Detection Settings")]
    public PitchDetectionMethod method = PitchDetectionMethod.Hybrid;
    public float minFreq = 80f;
    public float maxFreq = 2000f;
    public bool showDetailedLogs = true;

    public enum PitchDetectionMethod
    {
        FFTPeak,            // Simple FFT peak finding
        HarmonicProduct,    // Harmonic Product Spectrum (better for pitched sounds)
        Autocorrelation,    // Time-domain autocorrelation (robust)
        Cepstrum,          // Cepstral analysis (best for complex harmonics)
        Hybrid             // Combines multiple methods for best accuracy
    }

    void OnGUI()
    {
        GUILayout.Label("Generate InteractionClip ScriptableObjects", EditorStyles.boldLabel);

        audioFolder = EditorGUILayout.TextField("Audio Folder", audioFolder);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space();
        GUILayout.Label("Pitch Detection", EditorStyles.boldLabel);
        method = (PitchDetectionMethod)EditorGUILayout.EnumPopup("Detection Method", method);
        minFreq = EditorGUILayout.FloatField("Min Frequency (Hz)", minFreq);
        maxFreq = EditorGUILayout.FloatField("Max Frequency (Hz)", maxFreq);
        showDetailedLogs = EditorGUILayout.Toggle("Show Detailed Logs", showDetailedLogs);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Clips", GUILayout.Height(40)))
        {
            GenerateClips();
        }
    }

    void GenerateClips()
    {
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            AssetDatabase.CreateFolder(outputFolder.Substring(0, outputFolder.LastIndexOf('/')),
                                      outputFolder.Substring(outputFolder.LastIndexOf('/') + 1));
        }

        int processed = 0;
        foreach (var mat in Enum.GetValues(typeof(MaterialType)))
        {
            string matFolder = $"{audioFolder}/{mat}";
            if (!AssetDatabase.IsValidFolder(matFolder))
            {
                Debug.LogWarning($"Folder not found: {matFolder}");
                continue;
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { matFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                EditorUtility.DisplayProgressBar("Generating InteractionClips",
                    $"Processing {clip.name}", processed / (float)guids.Length);

                PitchAnalysisResult analysis = AnalyzeClip(clip);

                InteractionClipSO iclipSO = ScriptableObject.CreateInstance<InteractionClipSO>();
                iclipSO.clip = clip;
                iclipSO.materialType = (MaterialType)mat;
                iclipSO.basePitch = analysis.normalizedPitch;

                string outputPath = $"{outputFolder}/{mat}_{clip.name}.asset";
                AssetDatabase.CreateAsset(iclipSO, outputPath);

                if (showDetailedLogs)
                {
                    Debug.Log($"<color=cyan>Created {clip.name}</color>\n" +
                             $"  Frequency: {analysis.frequency:F1} Hz\n" +
                             $"  Normalized: {analysis.normalizedPitch:F3}\n" +
                             $"  Confidence: {analysis.confidence:F2}\n" +
                             $"  Method: {analysis.methodUsed}");
                }

                processed++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>Successfully generated {processed} InteractionClip assets!</color>");
    }

    struct PitchAnalysisResult
    {
        public float frequency;
        public float normalizedPitch;
        public float confidence;
        public string methodUsed;
    }

    PitchAnalysisResult AnalyzeClip(AudioClip clip)
    {
        if (clip == null)
            return new PitchAnalysisResult { frequency = 440f, normalizedPitch = 0.5f, confidence = 0f };

        // Get audio data
        float[] mono = GetMonoSamples(clip);

        // Apply windowing to reduce spectral leakage
        ApplyHannWindow(mono);

        PitchAnalysisResult result = new PitchAnalysisResult();

        switch (method)
        {
            case PitchDetectionMethod.FFTPeak:
                result = DetectPitchFFT(mono, clip.frequency);
                break;
            case PitchDetectionMethod.HarmonicProduct:
                result = DetectPitchHPS(mono, clip.frequency);
                break;
            case PitchDetectionMethod.Autocorrelation:
                result = DetectPitchAutocorrelation(mono, clip.frequency);
                break;
            case PitchDetectionMethod.Cepstrum:
                result = DetectPitchCepstrum(mono, clip.frequency);
                break;
            case PitchDetectionMethod.Hybrid:
                result = DetectPitchHybrid(mono, clip.frequency);
                break;
        }

        // Normalize pitch
        result.normalizedPitch = Mathf.Clamp01((result.frequency - minFreq) / (maxFreq - minFreq));

        return result;
    }

    float[] GetMonoSamples(AudioClip clip)
    {
        int sampleCount = clip.samples;
        int channels = clip.channels;
        float[] samples = new float[sampleCount * channels];
        clip.GetData(samples, 0);

        // Convert to mono
        float[] mono = new float[sampleCount];
        if (channels == 1)
        {
            mono = samples;
        }
        else
        {
            for (int i = 0; i < sampleCount; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                    sum += samples[i * channels + c];
                mono[i] = sum / channels;
            }
        }

        return mono;
    }

    void ApplyHannWindow(float[] samples)
    {
        int N = samples.Length;
        for (int i = 0; i < N; i++)
        {
            float window = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (N - 1)));
            samples[i] *= window;
        }
    }

    // Method 1: Simple FFT Peak Detection
    PitchAnalysisResult DetectPitchFFT(float[] samples, int sampleRate)
    {
        Complex[] fft = PerformFFT(samples);
        int maxIndex = FindPeakFrequencyIndex(fft, sampleRate, 1, fft.Length / 2);

        float freq = maxIndex * sampleRate / (float)fft.Length;
        float confidence = (float)fft[maxIndex].Magnitude / fft.Max(c => (float)c.Magnitude);

        return new PitchAnalysisResult
        {
            frequency = freq,
            confidence = confidence,
            methodUsed = "FFT Peak"
        };
    }

    // Method 2: Harmonic Product Spectrum (HPS) - Best for pitched sounds with harmonics
    PitchAnalysisResult DetectPitchHPS(float[] samples, int sampleRate)
    {
        Complex[] fft = PerformFFT(samples);
        int N = fft.Length;

        // Get magnitude spectrum
        double[] magnitude = new double[N / 2];
        for (int i = 0; i < N / 2; i++)
            magnitude[i] = fft[i].Magnitude;

        // Compute HPS by multiplying downsampled versions
        int numHarmonics = 5;
        double[] hps = new double[N / (2 * numHarmonics)];

        for (int i = 0; i < hps.Length; i++)
        {
            hps[i] = 1.0;
            for (int h = 1; h <= numHarmonics; h++)
            {
                int idx = i * h;
                if (idx < magnitude.Length)
                    hps[i] *= magnitude[idx];
            }
        }

        // Find peak in HPS
        int peakIdx = 1;
        double maxVal = hps[1];
        int minIdx = (int)(minFreq * N / sampleRate);
        int maxIdx = (int)(maxFreq * N / sampleRate);

        for (int i = minIdx; i < Mathf.Min(maxIdx, hps.Length); i++)
        {
            if (hps[i] > maxVal)
            {
                maxVal = hps[i];
                peakIdx = i;
            }
        }

        float freq = peakIdx * sampleRate / (float)N;
        float confidence = (float)(maxVal / hps.Max());

        return new PitchAnalysisResult
        {
            frequency = freq,
            confidence = confidence,
            methodUsed = "Harmonic Product Spectrum"
        };
    }

    // Method 3: Autocorrelation - Time domain, robust for noisy signals
    PitchAnalysisResult DetectPitchAutocorrelation(float[] samples, int sampleRate)
    {
        int N = samples.Length;
        float[] acf = new float[N / 2];

        // Compute autocorrelation
        for (int lag = 0; lag < acf.Length; lag++)
        {
            float sum = 0f;
            for (int i = 0; i < N - lag; i++)
            {
                sum += samples[i] * samples[i + lag];
            }
            acf[lag] = sum / (N - lag);
        }

        // Find first peak after minimum lag
        int minLag = (int)(sampleRate / maxFreq);
        int maxLag = (int)(sampleRate / minFreq);

        int peakLag = minLag;
        float maxPeak = acf[minLag];

        for (int i = minLag + 1; i < Mathf.Min(maxLag, acf.Length); i++)
        {
            // Look for peak (local maximum)
            if (acf[i] > acf[i - 1] && acf[i] > acf[i + 1] && acf[i] > maxPeak)
            {
                maxPeak = acf[i];
                peakLag = i;
            }
        }

        float freq = sampleRate / (float)peakLag;
        float confidence = maxPeak / acf[0]; // Normalize by autocorrelation at lag 0

        return new PitchAnalysisResult
        {
            frequency = freq,
            confidence = confidence,
            methodUsed = "Autocorrelation"
        };
    }

    // Method 4: Cepstral Analysis - Best for complex harmonic structures
    PitchAnalysisResult DetectPitchCepstrum(float[] samples, int sampleRate)
    {
        // First FFT
        Complex[] fft = PerformFFT(samples);

        // Log magnitude spectrum
        Complex[] logMag = new Complex[fft.Length];
        for (int i = 0; i < fft.Length; i++)
        {
            double mag = Math.Max(fft[i].Magnitude, 1e-10); // Avoid log(0)
            logMag[i] = new Complex(Math.Log(mag), 0);
        }

        // Inverse FFT to get cepstrum
        Complex[] cepstrum = PerformIFFT(logMag);

        // Find peak in quefrency domain
        int minQuefrency = (int)(sampleRate / maxFreq);
        int maxQuefrency = (int)(sampleRate / minFreq);

        int peakIdx = minQuefrency;
        double maxVal = cepstrum[minQuefrency].Magnitude;

        for (int i = minQuefrency; i < Mathf.Min(maxQuefrency, cepstrum.Length / 2); i++)
        {
            double val = cepstrum[i].Magnitude;
            if (val > maxVal)
            {
                maxVal = val;
                peakIdx = i;
            }
        }

        float freq = sampleRate / (float)peakIdx;
        float confidence = (float)(maxVal / cepstrum.Take(cepstrum.Length / 2).Max(c => c.Magnitude));

        return new PitchAnalysisResult
        {
            frequency = freq,
            confidence = confidence,
            methodUsed = "Cepstrum"
        };
    }

    // Method 5: Hybrid - Combines multiple methods weighted by confidence
    PitchAnalysisResult DetectPitchHybrid(float[] samples, int sampleRate)
    {
        var results = new List<PitchAnalysisResult>
        {
            DetectPitchFFT(samples, sampleRate),
            DetectPitchHPS(samples, sampleRate),
            DetectPitchAutocorrelation(samples, sampleRate),
            DetectPitchCepstrum(samples, sampleRate)
        };

        // Weighted average based on confidence
        float totalWeight = 0f;
        float weightedFreq = 0f;

        foreach (var result in results)
        {
            // Filter out obviously wrong results
            if (result.frequency >= minFreq && result.frequency <= maxFreq)
            {
                float weight = result.confidence;
                weightedFreq += result.frequency * weight;
                totalWeight += weight;
            }
        }

        float finalFreq = totalWeight > 0 ? weightedFreq / totalWeight : results[0].frequency;
        float avgConfidence = totalWeight / results.Count;

        return new PitchAnalysisResult
        {
            frequency = finalFreq,
            confidence = avgConfidence,
            methodUsed = "Hybrid (weighted average)"
        };
    }

    int FindPeakFrequencyIndex(Complex[] fft, int sampleRate, int startIdx, int endIdx)
    {
        int minIdx = Mathf.Max(startIdx, (int)(minFreq * fft.Length / sampleRate));
        int maxIdx = Mathf.Min(endIdx, (int)(maxFreq * fft.Length / sampleRate));

        int maxIndex = minIdx;
        double maxMag = fft[minIdx].Magnitude;

        for (int i = minIdx; i < maxIdx; i++)
        {
            double mag = fft[i].Magnitude;
            if (mag > maxMag)
            {
                maxMag = mag;
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    Complex[] PerformFFT(float[] samples)
    {
        int N = 1;
        while (N < samples.Length) N <<= 1;

        Complex[] fft = new Complex[N];
        for (int i = 0; i < samples.Length; i++)
            fft[i] = new Complex(samples[i], 0);
        for (int i = samples.Length; i < N; i++)
            fft[i] = Complex.Zero;

        FFT(fft, false);
        return fft;
    }

    Complex[] PerformIFFT(Complex[] data)
    {
        Complex[] result = (Complex[])data.Clone();
        FFT(result, true);
        return result;
    }

    void FFT(Complex[] buffer, bool inverse)
    {
        int N = buffer.Length;
        if (N <= 1) return;

        // Bit-reversal permutation
        int j = 0;
        for (int i = 0; i < N; i++)
        {
            if (i < j)
            {
                var temp = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = temp;
            }
            int m = N >> 1;
            while (m >= 1 && j >= m)
            {
                j -= m;
                m >>= 1;
            }
            j += m;
        }

        // Cooley-Tukey FFT
        int direction = inverse ? 1 : -1;
        for (int len = 2; len <= N; len <<= 1)
        {
            double angle = direction * 2 * Math.PI / len;
            Complex wlen = new Complex(Math.Cos(angle), Math.Sin(angle));
            for (int i = 0; i < N; i += len)
            {
                Complex w = Complex.One;
                for (int k = 0; k < len / 2; k++)
                {
                    Complex u = buffer[i + k];
                    Complex v = buffer[i + k + len / 2] * w;
                    buffer[i + k] = u + v;
                    buffer[i + k + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }

        // Normalize for inverse transform
        if (inverse)
        {
            for (int i = 0; i < N; i++)
                buffer[i] /= N;
        }
    }
}