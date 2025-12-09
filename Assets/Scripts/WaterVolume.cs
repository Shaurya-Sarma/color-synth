// WaterVolume.cs - For mesh collider water bodies
using UnityEngine;

public class WaterVolume : MonoBehaviour
{
    [Header("Water Properties")]
    public float waterDensity = 1000f; // kg/m³
    public float linearDrag = 5f;
    public float angularDrag = 3f;

    [Header("Visual Effects")]
    public ParticleSystem splashEffect;
    public AudioClip splashSound;

    private AudioSource audioSource;
    private Collider waterCollider;
    public float rippleEnergyScale = 3.0f;

    void Start()
    {
        // Get any collider type (mesh, box, capsule, etc)
        waterCollider = GetComponent<Collider>();

        if (waterCollider == null)
        {
            Debug.LogError("WaterVolume needs a Collider component!");
            return;
        }

        // Ensure it's a trigger
        if (!waterCollider.isTrigger)
        {
            Debug.LogWarning("Setting water collider as trigger...");
            waterCollider.isTrigger = true;
        }

        // Check if mesh collider is convex
        MeshCollider meshCol = waterCollider as MeshCollider;
        if (meshCol != null && !meshCol.convex)
        {
            Debug.LogError("Mesh Collider must be CONVEX to work as trigger!");
        }

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public float GetWaterSurfaceHeight()
    {
        if (waterCollider == null) return transform.position.y;

        // Use bounds - works for any collider type
        Bounds bounds = waterCollider.bounds;
        return bounds.center.y + (bounds.size.y * 0.5f);
    }

    public void CreateSplash(Vector3 position, float intensity)
    {
        if (splashEffect != null)
        {
            var splash = Instantiate(splashEffect, position, Quaternion.identity);

            splash.transform.Rotate(-90f, 0f, 0f, Space.Self);

            var main = splash.main;
            main.startSpeed = intensity * 1.5f;

            Destroy(splash.gameObject, 2f);
        }

        if (splashSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(splashSound, Mathf.Clamp01(intensity * 2f));
        }

        if (InteractionInterpreter.Instance != null)
        {
            float rippleEnergy = intensity * rippleEnergyScale;

            InteractionInterpreter.Instance.ProcessDiscreteCollision(
                position: position,
                impactEnergy: rippleEnergy,
                primaryMaterial: MaterialType.Cloth,
                secondaryMaterialForAudio: MaterialType.Cloth,
                distanceToListener: Camera.main ? Vector3.Distance(position, Camera.main.transform.position) : 5f
            );
        }
    }


    // Visualize water surface in Scene view only (optional - can remove this entire function)
    void OnDrawGizmosSelected()  // Changed: only shows when object is selected
    {
        if (waterCollider == null)
        {
            waterCollider = GetComponent<Collider>();
            if (waterCollider == null) return;
        }

        Gizmos.color = new Color(0, 0.6f, 1f, 0.3f);

        Bounds bounds = waterCollider.bounds;
        float surfaceY = bounds.center.y + (bounds.size.y * 0.5f);

        Vector3 surfaceCenter = new Vector3(bounds.center.x, surfaceY, bounds.center.z);
        Vector3 surfaceSize = new Vector3(bounds.size.x, 0.02f, bounds.size.z);

        Gizmos.DrawCube(surfaceCenter, surfaceSize);
    }
}