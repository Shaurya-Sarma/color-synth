using UnityEngine;

[SelectionBase]
public class Breakable : MonoBehaviour
{
    [SerializeField] GameObject intactVersion;
    [SerializeField] GameObject brokenVersion;

    [SerializeField] float breakThreshold = 5f; // velocity required to break
    [SerializeField] float destroyDelay = 20f;   // seconds before destroying pieces

    BoxCollider boxCollider;
    Rigidbody rb;

    private void Awake()
    {
        intactVersion.SetActive(true);
        brokenVersion.SetActive(false);
        boxCollider = GetComponent<BoxCollider>();

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // object stays in place until hit
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only break if impact is strong enough
        if (collision.relativeVelocity.magnitude >= breakThreshold)
        {
            Break();
        }
    }

    private void Break()
    {
        intactVersion.SetActive(false);
        brokenVersion.SetActive(true);
        boxCollider.enabled = false;

        // Enable physics on broken pieces
        Rigidbody[] pieces = brokenVersion.GetComponentsInChildren<Rigidbody>();
        foreach (var piece in pieces)
        {
            piece.isKinematic = false;
            piece.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // Destroy broken pieces after delay
        Destroy(brokenVersion, destroyDelay);
        Destroy(gameObject, destroyDelay);
    }
}
