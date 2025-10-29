using UnityEngine;

[System.Serializable]
public class FrequencyColorMap
{
    [SerializeField]
    private Gradient frequencyToColor;

    public FrequencyColorMap()
    {
        // Define color and alpha keys
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        // 0.0 → Deep Blue (#1438FF)
        colorKeys[0].color = HexToColor("#1438FF");
        colorKeys[0].time = 0.0f;

        // 0.2 → Cyan (#1AD0FF)
        colorKeys[1].color = HexToColor("#1AD0FF");
        colorKeys[1].time = 0.2f;

        // 0.5 → Violet (#9A33FF)
        colorKeys[2].color = HexToColor("#9A33FF");
        colorKeys[2].time = 0.5f;

        // 0.7 → Magenta (#FF33C4)
        colorKeys[3].color = HexToColor("#FF33C4");
        colorKeys[3].time = 0.7f;

        // 1.0 → Pinkish Red (#FF3366)
        colorKeys[4].color = HexToColor("#FF3366");
        colorKeys[4].time = 1.0f;

        // Alpha keys (fully opaque)
        alphaKeys[0].alpha = 1f;
        alphaKeys[0].time = 0.0f;
        alphaKeys[1].alpha = 1f;
        alphaKeys[1].time = 1.0f;

        // Construct the gradient
        frequencyToColor = new Gradient();
        frequencyToColor.SetKeys(colorKeys, alphaKeys);
    }

    // Returns a color corresponding to a frequency hint (0 = low, 1 = high).
    public Color evaluate(float frequencyHint)
    {
        return frequencyToColor.Evaluate(Mathf.Clamp01(frequencyHint));
    }


    // Converts a hex string (e.g. "#FF33C4") to a Unity Color.
    private Color HexToColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
            return color;
        return Color.white;
    }
}
