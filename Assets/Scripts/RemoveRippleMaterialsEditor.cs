// using UnityEngine;
// using UnityEditor;

// public class RemoveRippleMaterialsEditor : EditorWindow
// {
//     private Material defaultMaterial;

//     [MenuItem("Tools/Remove Ripple Materials")]
//     public static void ShowWindow()
//     {
//         GetWindow<RemoveRippleMaterialsEditor>("Remove Ripple Materials");
//     }

//     private void OnGUI()
//     {
//         GUILayout.Label("Ripple Material Cleanup", EditorStyles.boldLabel);
//         GUILayout.Space(5);

//         defaultMaterial = (Material)EditorGUILayout.ObjectField("Default Material", defaultMaterial, typeof(Material), false);

//         GUILayout.Space(10);

//         if (GUILayout.Button("Reset All Scene Objects"))
//         {
//             if (defaultMaterial == null)
//             {
//                 EditorUtility.DisplayDialog("Missing Material", "Please assign the default material.", "OK");
//                 return;
//             }

//             CleanupScene();
//         }
//     }

//     private void CleanupScene()
//     {
//         int rendererCount = 0;

//         foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
//         {
//             if (renderer.gameObject.CompareTag("IgnoreRipple"))
//                 continue;

//             renderer.sharedMaterials = new Material[] { defaultMaterial };
//             rendererCount++;
//         }

//         Debug.Log($"✅ Reset {rendererCount} renderers to the default material.");
//         EditorUtility.DisplayDialog("Cleanup Complete", $"Reset {rendererCount} renderers to default material.", "OK");
//     }
// }
