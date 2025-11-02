using UnityEngine;

public enum MaterialType
{
    Glass,
    Metal,
    Wood,
    Cloth,
    Plastic
}

// attach to any object with Rigidbody to enable physics-based interaction ripples
// detects collisions and continuous movement to send interaction data to InteractionInterpreter
// setKinematic = true to prevent object from being affected by physics

[RequireComponent(typeof(Rigidbody))]
public class PhysicsInteractor : MonoBehaviour
{
    [Header("Thresholds")]
    public float minEnergyThreshold = 1f;
    public float minContinuousVelocity = 0.1f;

    [Header("Material Settings")]
    public MaterialType materialType = MaterialType.Wood;

    [Header("Physics Settings")]

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    //! Find a solution to "fix" duplicate collision events between two objects? maybe allow this?
    //! when a non-kinematic object moves then the ripple usually never actually colors the original object 
    //! because the ripple shader uses world position and the object has moved away from the impact point

    private void OnCollisionEnter(Collision collision)
    {
        if (InteractionInterpreter.Instance == null) return;
        if (this.gameObject.CompareTag("IgnoreRipple")) return; // optional tag to skip ripple generation

        // Get other rigidbody
        Rigidbody otherRb = collision.rigidbody;

        // Calculate effective mass
        float myMass = rb.mass;
        float otherMass = otherRb != null ? otherRb.mass : myMass;
        float effectiveMass = myMass * otherMass / (myMass + otherMass);

        // Impact calculations
        float impactSpeed = collision.relativeVelocity.magnitude;
        float impactEnergy = 0.5f * effectiveMass * impactSpeed * impactSpeed;

        if (impactEnergy < minEnergyThreshold) return;

        // Contact info
        ContactPoint contact = collision.contacts[0];
        Vector3 contactPoint = contact.point;
        Vector3 contactNormal = contact.normal;

        // Contact radius
        Collider col = collision.collider;
        float contactRadius = col switch
        {
            SphereCollider sphere => sphere.radius,
            BoxCollider box => box.size.magnitude * 0.25f,
            CapsuleCollider capsule => capsule.radius,
            _ => 0.1f
        };

        // Physics details
        float bounciness = col.material != null ? col.material.bounciness : 0f;
        Vector3 tangentialVel = Vector3.ProjectOnPlane(rb.linearVelocity, contactNormal);
        float slipSpeed = tangentialVel.magnitude;
        float spinSpeed = rb.angularVelocity.magnitude;
        float distanceToListener = Camera.main != null ?
            Vector3.Distance(contactPoint, Camera.main.transform.position) : 10f;

        Debug.Log($"Collision with impactEnergy={impactEnergy} by object={this.gameObject.name}");

        // Send to interpreter
        InteractionInterpreter.Instance.ProcessDiscreteCollision(
            position: contactPoint,
            impactEnergy: impactEnergy,
            slipSpeed: slipSpeed,
            spinSpeed: spinSpeed,
            contactRadius: contactRadius,
            bounciness: bounciness,
            material: materialType,
            distanceToListener: distanceToListener
        );
    }

    void Update()
    {

        // float speed = rb.linearVelocity.magnitude;
        // if (speed < minContinuousVelocity) return;

        // Continuous interaction handling here if needed
    }
}