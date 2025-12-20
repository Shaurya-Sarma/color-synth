using UnityEngine;

public class RippleTester : MonoBehaviour
{
    [Header("Ripple Settings")]
    public Vector3 spawnPosition = Vector3.zero;
    public float speed = 0.5f;
    public float maxDist = 1f;
    public float noiseScale = 1.5f;
    public float noiseStrength = 0.3f;
    public float fadeWidth = 0f;

    [Header("Ripple Color")]
    public Color rippleColor = Color.cyan;

    private void Start()
    {
        // Optional: emit on start
        // EmitRipple();
    }

    [ContextMenu("Emit Ripple")]
    private void EmitRipple()
    {
        RippleEvent ripple = new RippleEvent(
            spawnPosition,
            rippleColor,
            speed,
            maxDist,
            fadeWidth,
            noiseScale,
            noiseStrength,
            continuous: false
        );

        SoundToColorManager.Instance.EmitRipple(ripple);
    }
}
