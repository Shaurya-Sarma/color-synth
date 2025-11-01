using UnityEngine;

// give every object a material type to slightly alter sound/ripple characteristics 
// (e.g., glass sounds sharper, cloth more muffled, etc.)
public enum MaterialType
{
    Glass,
    Metal,
    Wood,
    Cloth,
    Plastic
}

// handles physics-based interactions to generate sound and ripple effects
// passes interaction data to InteractionInterpreter for further processing

[RequireComponent(typeof(Rigidbody))]
public class PhysicsInteractor : MonoBehaviour
{
    [Header("Thresholds")]
    public float minEnergyThreshold = 1f;  // minimum impact energy to register a discrete interaction
    public float minContinuousVelocity = 0.1f;  // minimum velocity to register continuous interaction

    [Header("Material Settings")]
    public MaterialType materialType = MaterialType.Wood; // object material type
    private Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // -------------------
    // DISCRETE INTERACTIONS
    // -------------------

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody otherRb = collision.rigidbody;

        // effective mass for energy calculation
        float effectiveMass = rb.mass * (otherRb != null ? otherRb.mass : rb.mass) /
                              (rb.mass + (otherRb != null ? otherRb.mass : rb.mass));

        // impact energy as proxy for loudness / ripple size
        float impactSpeed = collision.relativeVelocity.magnitude;
        float impactEnergy = 0.5f * effectiveMass * impactSpeed * impactSpeed;

        if (impactEnergy < minEnergyThreshold) return; // ignore insignificant collisions

        // Contact point & normal
        ContactPoint contact = collision.contacts[0];
        Vector3 contactPoint = contact.point;
        Vector3 contactNormal = contact.normal;

        // Approximate contact area
        Collider col = collision.collider;
        float contactRadius = 0.1f; // default
        if (col is SphereCollider sphere) contactRadius = sphere.radius;
        else if (col is BoxCollider box) contactRadius = box.size.magnitude * 0.25f;

        // Restitution / bounciness
        float bounciness = col.material != null ? col.material.bounciness : 0f;

        // Tangential/slip velocity
        Vector3 tangentialVel = Vector3.ProjectOnPlane(rb.linearVelocity, contactNormal);
        float slipSpeed = tangentialVel.magnitude;

        // Angular velocity
        float spinSpeed = rb.angularVelocity.magnitude;

        // Distance to listener (camera)
        float distanceToListener = Vector3.Distance(contactPoint, Camera.main.transform.position);

        // send event to InteractionInterpreter
        //! Give physics interaction with priority -1 to ensure unique 
        //! interactions are processed before generic physics-based ones.
        // InteractionInterpreter.Instance.ProcessDiscreteInteraction(
        //     position: contactPoint,
        //     impactEnergy: impactEnergy,
        //     slipSpeed: slipSpeed,
        //     spinSpeed: spinSpeed,
        //     contactRadius: contactRadius,
        //     bounciness: bounciness,
        //     material: materialType,
        //     distanceToListener: distanceToListener,
        //     otherObject: collision.gameObject
        // );
    }

    // -------------------
    // CONTINUOUS INTERACTIONS
    // -------------------
    void Update()
    {
        float speed = rb.linearVelocity.magnitude;

        if (speed < minContinuousVelocity) return;

        // Continuous event: e.g., rolling, sliding
        // InteractionInterpreter.Instance.ProcessContinuousInteraction(
        //     position: transform.position,
        //     velocity: speed,
        //     angularVelocity: rb.angularVelocity.magnitude,
        //     material: materialType,
        //     objectRef: gameObject
        // );
    }
}


