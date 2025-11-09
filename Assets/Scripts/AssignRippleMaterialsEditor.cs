using UnityEngine;
using UnityEditor;

public class AssignRippleMaterialsEditor : EditorWindow
{
    private Material defaultMaterial;
    private Material rippleMaterial;

    [MenuItem("Tools/Assign Ripple Materials")]
    public static void ShowWindow()
    {
        GetWindow<AssignRippleMaterialsEditor>("Assign Ripple Materials");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ripple Material Assigner", EditorStyles.boldLabel);
        GUILayout.Space(5);

        defaultMaterial = (Material)EditorGUILayout.ObjectField("Default Material", defaultMaterial, typeof(Material), false);
        rippleMaterial = (Material)EditorGUILayout.ObjectField("Ripple Material", rippleMaterial, typeof(Material), false);

        GUILayout.Space(10);

        if (GUILayout.Button("Apply to All Scene Objects"))
        {
            if (defaultMaterial == null || rippleMaterial == null)
            {
                EditorUtility.DisplayDialog("Missing Materials", "Please assign both materials before applying.", "OK");
                return;
            }

            ApplyMaterialsToScene();
        }
    }

    private void ApplyMaterialsToScene()
    {
        int count = 0;

        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            // Skip editor-only or VR hands / ignored objects
            if (renderer.gameObject.CompareTag("IgnoreRipple"))
                continue;

            // Assign both materials (default first, ripple second)
            renderer.sharedMaterials = new Material[] { defaultMaterial, rippleMaterial };
            count++;
        }

        Debug.Log($"✅ Assigned ripple + default materials to {count} renderers in scene.");
        EditorUtility.DisplayDialog("Ripple Material Assignment", $"Assigned materials to {count} objects.", "OK");
    }
}
