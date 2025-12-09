// Buoyancy.cs - Simplified version with only density control
// Attach this to any object that should float/sink in water
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Buoyancy : MonoBehaviour
{
    [Header("Buoyancy Control")]
    [Tooltip("1.0 = floats | 1.25 = partially submerged | 1.5+ = sinks")]
    [Range(1f, 1.75f)]
    public float objectDensity = 1.0f;

    // Constants 
    private const float BUOYANCY_FORCE = 30f;
    private const float WATER_DRAG = 4f;
    private const float BASE_DAMPING = 3.5f;
    private const float SURFACE_DAMPING_MULTIPLIER = 4f;
    private const float SURFACE_DAMPING_RANGE = 0.4f;
    private const float MAX_UPWARD_VELOCITY = 3f;
    private const float MAX_DOWNWARD_VELOCITY = 10f;

    private Rigidbody rb;
    private WaterVolume currentWater;
    private float defaultDrag;
    private float defaultAngularDrag;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        defaultDrag = rb.linearDamping;
        defaultAngularDrag = rb.angularDamping;

        if (rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    void FixedUpdate()
    {
        // Clamp velocities
        Vector3 vel = rb.linearVelocity;
        vel.y = Mathf.Clamp(vel.y, -MAX_DOWNWARD_VELOCITY, MAX_UPWARD_VELOCITY);
        rb.linearVelocity = vel;

        if (currentWater == null) return;

        float waterSurfaceY = currentWater.GetWaterSurfaceHeight();
        float objectY = transform.position.y;
        float depth = waterSurfaceY - objectY;

        if (depth > 0) // Underwater
        {
            // Buoyancy force
            float submersionFactor = Mathf.Clamp01(depth / 1.5f);
            float gravityForce = rb.mass * Physics.gravity.magnitude;
            float totalForce = (gravityForce + (submersionFactor * BUOYANCY_FORCE)) / objectDensity;
            rb.AddForce(Vector3.up * totalForce, ForceMode.Force);

            // Progressive damping (stronger near surface)
            float surfaceProximity = 1f - Mathf.Clamp01(depth / SURFACE_DAMPING_RANGE);
            float progressiveDamping = BASE_DAMPING + (SURFACE_DAMPING_MULTIPLIER * surfaceProximity);
            Vector3 dampingForce = -rb.linearVelocity * progressiveDamping * rb.mass;
            rb.AddForce(dampingForce, ForceMode.Force);

            // Water drag
            rb.linearDamping = WATER_DRAG;
            rb.angularDamping = WATER_DRAG * 0.8f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        WaterVolume water = other.GetComponent<WaterVolume>();
        if (water != null && other.isTrigger)
        {
            currentWater = water;

            // Splash
            float entrySpeed = Mathf.Abs(rb.linearVelocity.y);
            if (entrySpeed > 0.5f)
            {
                Vector3 splashPos = new Vector3(
                    transform.position.x,
                    currentWater.GetWaterSurfaceHeight(),
                    transform.position.z
                );
                currentWater.CreateSplash(splashPos, entrySpeed);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        WaterVolume water = other.GetComponent<WaterVolume>();
        if (water == currentWater)
        {
            currentWater = null;
            rb.linearDamping = defaultDrag;
            rb.angularDamping = defaultAngularDrag;
        }
    }
}

/* ========================================
 * DENSITY GUIDE (ONLY CONTROL YOU NEED):
 * ========================================
 * 
 * 1.00 = Floats high (cork, foam, beach ball)
 * 1.10 = Floats at surface (rubber duck)
 * 1.20 = Partially submerged (wood)
 * 1.30 = Mostly submerged (dense plastic)
 * 1.40 = Barely floats (very dense plastic)
 * 1.50 = Starts to sink slowly
 * 1.60 = Sinks steadily
 * 1.75 = Sinks quickly (metal)
 * 
 * 
 */