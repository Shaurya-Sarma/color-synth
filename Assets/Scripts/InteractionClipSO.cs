using UnityEngine;

[CreateAssetMenu(menuName = "Audio/InteractionClip")]
public class InteractionClipSO : ScriptableObject
{
    public AudioClip clip;
    public MaterialType materialType;
    [Range(0f, 1f)]
    public float basePitch; // precomputed in editor
}
