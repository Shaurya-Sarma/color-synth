using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SocketExplosion : MonoBehaviour
{
    [Header("Explosion Settings")]
    public AudioClip explosionClip;
    public float explosionVolume = 1f;
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    public ForceMode forceMode = ForceMode.Impulse;

    [Header("Ripple Settings")]
    public float rippleMagnitude = 5f;

    private bool forkInserted = false;

    private void OnEnable()
    {
        XRSocketInteractor socket = GetComponent<XRSocketInteractor>();
        socket.selectEntered.AddListener(OnSocketed);
    }

    private void OnDisable()
    {
        XRSocketInteractor socket = GetComponent<XRSocketInteractor>();
        socket.selectEntered.RemoveListener(OnSocketed);
    }

    private void OnSocketed(SelectEnterEventArgs args)
    {
        if (forkInserted) return; // only trigger once

        GameObject obj = args.interactableObject.transform.gameObject;

        if (obj.CompareTag("Fork"))
        {
            forkInserted = true;

            if (explosionClip != null)
            {
                GameObject audioObj = new GameObject("ExplosionSound");
                audioObj.transform.position = transform.position;

                AudioSource audioSource = audioObj.AddComponent<AudioSource>();
                audioSource.clip = explosionClip;
                audioSource.volume = explosionVolume;
                audioSource.spatialBlend = 1f;
                audioSource.Play();

                Destroy(audioObj, explosionClip.length + 0.1f);
            }

            EmitBigRipple(transform.position, rippleMagnitude);

            XRGrabInteractable grabInteractable = obj.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null) grabInteractable.enabled = false;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            ApplyExplosionForce(transform.position, explosionRadius, explosionForce);
        }
    }

    private void EmitBigRipple(Vector3 position, float magnitude)
    {
        if (InteractionInterpreter.Instance != null)
        {
            InteractionInterpreter.Instance.ProcessDiscreteCollision(
                position: position,
                impactEnergy: magnitude * 10f,
                primaryMaterial: MaterialType.Metal,
                secondaryMaterialForAudio: MaterialType.Metal,
                distanceToListener: Vector3.Distance(position, Camera.main != null ? Camera.main.transform.position : position)
            );
        }
    }

    private void ApplyExplosionForce(Vector3 position, float radius, float force)
    {
        // Find all colliders in the radius
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (Collider col in colliders)
        {
            if (col.tag == "Fork") continue; // skip the fork itself

            Rigidbody nearbyRb = col.attachedRigidbody;
            if (nearbyRb != null && !nearbyRb.isKinematic)
            {
                nearbyRb.AddExplosionForce(force, position, radius, 1f, forceMode);
            }
        }
    }
}
