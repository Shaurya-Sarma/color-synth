using UnityEngine;

public enum MaterialType
{
    Glass,
    Metal,
    Wood,
    Cloth,
    Plastic
}

[RequireComponent(typeof(Rigidbody))]
public class PhysicsInteractor : MonoBehaviour
{
    [Header("Thresholds")]
    public float minEnergyThreshold = 1f;
    public float minContinuousVelocity = 0.1f;

    [Header("Material Settings")]
    public MaterialType materialType = MaterialType.Wood;

    [Header("Ripple Settings")]
    public float rippleCooldown = 0.1f; // seconds between allowed ripples

    private Rigidbody rb;
    private float lastRippleTime = -999f; // track last ripple emission

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (InteractionInterpreter.Instance == null) return;
        if (this.gameObject.CompareTag("IgnoreRipple")) return;

        Debug.Log("Collision detected on " + this.gameObject.name);


        Rigidbody otherRb = collision.rigidbody;
        if (otherRb == null) return;

        PhysicsInteractor otherInteractor = otherRb.GetComponent<PhysicsInteractor>();
        if (otherInteractor == null) return;

        // ripple cooldown per object
        if ((Time.time - lastRippleTime < rippleCooldown) ||
            (Time.time - otherInteractor.lastRippleTime < otherInteractor.rippleCooldown))
        {
            Debug.Log($"Ignoring collision due to cooldown. " +
                      $"Self Δt={(Time.time - lastRippleTime):F2}s, " +
                      $"Other Δt={(Time.time - otherInteractor.lastRippleTime):F2}s, " +
                      $"Objects: {this.gameObject.name}, {otherInteractor.gameObject.name}");
            return;
        }

        // both are ready, update both cooldowns
        lastRippleTime = Time.time;
        otherInteractor.lastRippleTime = Time.time;

        // Calculate effective mass
        float myMass = rb.mass;
        float otherMass = otherRb.mass;
        float effectiveMass = myMass * otherMass / (myMass + otherMass);

        // Total impact energy
        float impactSpeed = collision.relativeVelocity.magnitude;
        float totalImpactEnergy = 0.5f * effectiveMass * impactSpeed * impactSpeed;

        if (totalImpactEnergy < minEnergyThreshold) return;

        // Energy contribution per object
        float myContribution = otherMass / (myMass + otherMass) * totalImpactEnergy;
        float otherContribution = myMass / (myMass + otherMass) * totalImpactEnergy;

        // Decide which object sends the ripple
        float epsilon = 0.001f * totalImpactEnergy; // 0.1% tolerance
        bool nearlyEqual = Mathf.Abs(myContribution - otherContribution) < epsilon;

        bool isPrimary;
        if (nearlyEqual)
        {
            // Deterministic fallback
            isPrimary = this.GetInstanceID() < otherRb.GetInstanceID();
        }
        else
        {
            // Energy-based decision
            isPrimary = myContribution > otherContribution;
        }

        if (!isPrimary) return;


        MaterialType primaryMaterial = materialType;
        MaterialType secondaryMaterial = otherInteractor.materialType;

        // Contact info
        ContactPoint contact = collision.contacts[0];
        Vector3 contactPoint = contact.point;
        Vector3 contactNormal = contact.normal;

        // Contact radius (rough heuristic)
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

        // Send to interpreter (ripple + both sounds)
        InteractionInterpreter.Instance.ProcessDiscreteCollision(
            position: contactPoint,
            impactEnergy: totalImpactEnergy,
            slipSpeed: slipSpeed,
            spinSpeed: spinSpeed,
            contactRadius: contactRadius,
            bounciness: bounciness,
            primaryMaterial: primaryMaterial,
            secondaryMaterialForAudio: secondaryMaterial,
            distanceToListener: distanceToListener
        );
    }
}
