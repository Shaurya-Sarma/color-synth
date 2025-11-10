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


        //! Cooldown is broken need to fix, should i be checking both objects? but i did that and had issue 
        // --- Cooldown check (only for primary object) ---
        if (Time.time - lastRippleTime < rippleCooldown)
        {
            Debug.Log($"Ignoring collision due to cooldown. Δt={Time.time - lastRippleTime:F2}s, Object={gameObject.name}");
            return;
        }
        lastRippleTime = Time.time; // update cooldown only for primary

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

        // Physics details
        float distanceToListener = Camera.main != null ?
            Vector3.Distance(contactPoint, Camera.main.transform.position) : 10f;

        // Send to interpreter (ripple + both sounds)
        InteractionInterpreter.Instance.ProcessDiscreteCollision(
            position: contactPoint,
            impactEnergy: totalImpactEnergy,
            primaryMaterial: primaryMaterial,
            secondaryMaterialForAudio: secondaryMaterial,
            distanceToListener: distanceToListener
        );
    }
}
