// using UnityEngine;
// using UnityEditor;
// using System.IO;

// public class DoubleWarpNoiseBaker : EditorWindow
// {
//     private int textureSize = 2048;
//     private int octaves = 5;
//     private float baseScale = 4f;
//     private Texture2D noiseTex;

//     [MenuItem("Tools/Bake Seamless Double Warp Noise")]
//     public static void ShowWindow()
//     {
//         GetWindow<DoubleWarpNoiseBaker>("Seamless Noise Baker");
//     }

//     void OnGUI()
//     {
//         GUILayout.Label("Seamless Noise Texture Baker", EditorStyles.boldLabel);
//         GUILayout.Space(10);

//         textureSize = EditorGUILayout.IntPopup("Texture Size", textureSize,
//             new string[] { "512", "1024", "2048", "4096" },
//             new int[] { 512, 1024, 2048, 4096 });

//         octaves = EditorGUILayout.IntSlider("Octaves", octaves, 3, 8);
//         baseScale = EditorGUILayout.Slider("Base Scale", baseScale, 2f, 16f);

//         GUILayout.Space(10);

//         if (GUILayout.Button("Bake Texture", GUILayout.Height(40)))
//         {
//             BakeTexture();
//         }

//         GUILayout.Space(10);
//         GUILayout.Label("Higher octaves = more detail\nHigher scale = more features", EditorStyles.helpBox);
//     }

//     // Generate gradient vector for proper Perlin noise
//     Vector2 GetGradient(Vector2 p, float period)
//     {
//         // Wrap coordinates to create seamless tiling
//         p.x = Mathf.Repeat(p.x, period);
//         p.y = Mathf.Repeat(p.y, period);

//         // Generate pseudo-random angle from position
//         float hash = Mathf.Sin(p.x * 127.1f + p.y * 311.7f) * 43758.5453f;
//         hash = hash - Mathf.Floor(hash);
//         float angle = hash * 2f * Mathf.PI;

//         return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
//     }

//     // Improved smoothstep (6th order for C2 continuity)
//     float Smoothstep(float t)
//     {
//         return t * t * t * (t * (t * 6f - 15f) + 10f);
//     }

//     // Seamless Perlin noise with proper gradient interpolation
//     float PerlinNoise(Vector2 p, float period)
//     {
//         // Integer and fractional parts
//         Vector2 i = new Vector2(Mathf.Floor(p.x), Mathf.Floor(p.y));
//         Vector2 f = p - i;

//         // Smooth interpolation curve
//         float u = Smoothstep(f.x);
//         float v = Smoothstep(f.y);

//         // Wrap grid coordinates for seamless tiling
//         float x0 = Mathf.Repeat(i.x, period);
//         float x1 = Mathf.Repeat(i.x + 1f, period);
//         float y0 = Mathf.Repeat(i.y, period);
//         float y1 = Mathf.Repeat(i.y + 1f, period);

//         // Get gradient vectors at grid corners
//         Vector2 g00 = GetGradient(new Vector2(x0, y0), period);
//         Vector2 g10 = GetGradient(new Vector2(x1, y0), period);
//         Vector2 g01 = GetGradient(new Vector2(x0, y1), period);
//         Vector2 g11 = GetGradient(new Vector2(x1, y1), period);

//         // Distance vectors from corners
//         Vector2 d00 = f - new Vector2(0f, 0f);
//         Vector2 d10 = f - new Vector2(1f, 0f);
//         Vector2 d01 = f - new Vector2(0f, 1f);
//         Vector2 d11 = f - new Vector2(1f, 1f);

//         // Dot products (gradient influence)
//         float n00 = Vector2.Dot(g00, d00);
//         float n10 = Vector2.Dot(g10, d10);
//         float n01 = Vector2.Dot(g01, d01);
//         float n11 = Vector2.Dot(g11, d11);

//         // Bilinear interpolation
//         float nx0 = Mathf.Lerp(n00, n10, u);
//         float nx1 = Mathf.Lerp(n01, n11, u);
//         float result = Mathf.Lerp(nx0, nx1, v);

//         // Remap from [-1, 1] to [0, 1]
//         return result * 0.5f + 0.5f;
//     }

//     // Fractal Brownian Motion with seamless tiling
//     float FBM(Vector2 p, float period)
//     {
//         float value = 0f;
//         float amplitude = 0.5f;
//         float frequency = 1f;
//         float maxValue = 0f; // For normalization

//         for (int i = 0; i < octaves; i++)
//         {
//             // Each octave adds detail at higher frequency
//             value += PerlinNoise(p * frequency, period * frequency) * amplitude;
//             maxValue += amplitude;

//             frequency *= 2f;
//             amplitude *= 0.5f;
//         }

//         // Normalize to 0-1 range
//         return value / maxValue;
//     }

//     // Double domain warped noise for organic patterns
//     float DoubleWarpedNoise(Vector2 p, float period)
//     {
//         // First domain warp layer
//         Vector2 q = new Vector2(
//             FBM(p + new Vector2(0.0f, 0.0f), period),
//             FBM(p + new Vector2(5.2f, 1.3f), period)
//         );

//         // Second domain warp layer
//         Vector2 r = new Vector2(
//             FBM(p + 4.0f * q + new Vector2(1.7f, 9.2f), period),
//             FBM(p + 4.0f * q + new Vector2(8.3f, 2.8f), period)
//         );

//         // Final layer with warped coordinates
//         return FBM(p + 4.0f * r, period);
//     }

//     void BakeTexture()
//     {
//         // Create texture with proper settings
//         noiseTex = new Texture2D(textureSize, textureSize, TextureFormat.RFloat, true, true);
//         noiseTex.wrapMode = TextureWrapMode.Repeat;
//         noiseTex.filterMode = FilterMode.Bilinear;
//         noiseTex.anisoLevel = 8;

//         // Use power-of-2 scale for perfect tiling at all octave levels
//         float scale = Mathf.Pow(2, Mathf.Max(2, octaves - 2)) * baseScale;

//         Debug.Log($"Generating {textureSize}x{textureSize} texture with {octaves} octaves, scale: {scale}");

//         // Generate noise
//         for (int y = 0; y < textureSize; y++)
//         {
//             // Progress bar for large textures
//             if (y % 64 == 0)
//             {
//                 EditorUtility.DisplayProgressBar("Baking Noise",
//                     $"Generating row {y}/{textureSize}",
//                     (float)y / textureSize);
//             }

//             for (int x = 0; x < textureSize; x++)
//             {
//                 // Map pixel to noise space
//                 Vector2 uv = new Vector2((float)x / textureSize, (float)y / textureSize);
//                 Vector2 p = uv * scale;

//                 // Generate seamless warped noise
//                 float value = DoubleWarpedNoise(p, scale);

//                 // Ensure value is in valid range
//                 value = Mathf.Clamp01(value);

//                 noiseTex.SetPixel(x, y, new Color(value, value, value, 1f));
//             }
//         }

//         EditorUtility.ClearProgressBar();

//         // Apply changes
//         noiseTex.Apply(true);

//         // Save texture
//         string path = EditorUtility.SaveFilePanelInProject(
//             "Save Seamless Noise Texture",
//             "SeamlessDoubleWarpNoise",
//             "png",
//             "Choose location to save texture");

//         if (!string.IsNullOrEmpty(path))
//         {
//             // Save as PNG
//             byte[] bytes = noiseTex.EncodeToPNG();
//             File.WriteAllBytes(path, bytes);

//             AssetDatabase.Refresh();

//             // Configure import settings for seamless tiling
//             TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
//             if (importer != null)
//             {
//                 importer.wrapMode = TextureWrapMode.Repeat;
//                 importer.filterMode = FilterMode.Bilinear;
//                 importer.anisoLevel = 8;
//                 importer.textureCompression = TextureImporterCompression.Uncompressed;
//                 importer.isReadable = false;
//                 importer.mipmapEnabled = true;
//                 importer.sRGBTexture = true;

//                 // Set to single channel for optimization
//                 TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
//                 settings.format = TextureImporterFormat.R8;
//                 importer.SetPlatformTextureSettings(settings);

//                 importer.SaveAndReimport();
//             }

//             Debug.Log($"✅ Seamless noise texture saved to: {path}");

//             // Select the created texture
//             Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
//         }
//         else
//         {
//             Debug.LogWarning("Texture save cancelled");
//         }
//     }
// }