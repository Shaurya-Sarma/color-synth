using UnityEngine;

[System.Serializable]

//* parameters to define a ripple effect
// position: where the ripple originates in world space (i.e collider hit point)
// color: color of the ripple (depends on frequency/pitch TBA...)
// startTime: when the ripple effect starts in seconds (allows us to animate how the ripple spreads)
// speed: speed of the ripple expansion (growth of ripple)
// maxDistance: maximum distance the ripple travels before fully fading out
// fadeWidth: controls the softness/thickness of the ripple edge

public struct RippleEvent
{
    public Vector3 position;
    public Color color;
    public float startTime;
    public float speed;
    public float maxDistance;
    public float fadeWidth;

    public RippleEvent(Vector3 pos, Color col, float spd = 1.0f, float maxDist = 2.0f, float fade = 0.05f)
    {
        position = pos;
        color = col;
        startTime = Time.time;
        speed = spd;
        maxDistance = maxDist;
        fadeWidth = fade;
    }

}