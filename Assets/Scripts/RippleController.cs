using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class RippleController : MonoBehaviour
{
    [Header("Assign your Ripple Shader Material here")]
    public Material rippleShaderMaterial; // drag in your RippleMaterial in Inspector

    private Material rippleMatInstance;

    // void Start()
    // {
    //     if (rippleShaderMaterial == null)
    //     {
    //         Debug.LogError("RippleController: No rippleShaderMaterial assigned!");
    //         return;
    //     }

    //     var rend = GetComponent<MeshRenderer>();
    //     rippleMatInstance = new Material(rippleShaderMaterial); // create unique runtime instance
    //     rend.material = rippleMatInstance;

    //     // Link to manager
    //     if (SoundToColorManager.Instance != null)
    //         SoundToColorManager.Instance.rippleMaterial = rippleMatInstance;
    // }
}
