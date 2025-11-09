using UnityEngine;

[System.Serializable]
public class FrequencyColorMap
{
    [SerializeField]
    private Gradient frequencyToColor;

    public FrequencyColorMap()
    {
        // Define 8 color keys for a full visible spectrum
        GradientColorKey[] colorKeys = new GradientColorKey[8];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        // 0.0 → Deep Blue (#1438FF)
        colorKeys[0].color = HexToColor("#1438FF");
        colorKeys[0].time = 0.0f;

        // 0.15 → Cyan (#33CCFF)
        colorKeys[1].color = HexToColor("#33CCFF");
        colorKeys[1].time = 0.15f;

        // 0.35 → Green (#33FF77)
        colorKeys[2].color = HexToColor("#33FF77");
        colorKeys[2].time = 0.35f;

        // 0.5 → Yellow (#FFFF33)
        colorKeys[3].color = HexToColor("#FFFF33");
        colorKeys[3].time = 0.5f;

        // 0.65 → Orange (#FF9933)
        colorKeys[4].color = HexToColor("#FF9933");
        colorKeys[4].time = 0.65f;

        // 0.75 → Red (#FF3333)
        colorKeys[5].color = HexToColor("#FF3333");
        colorKeys[5].time = 0.75f;

        // 0.9 → Pink (#FF33CC)
        colorKeys[6].color = HexToColor("#FF33CC");
        colorKeys[6].time = 0.9f;

        // 1.0 → Violet (#9933FF)
        colorKeys[7].color = HexToColor("#9933FF");
        colorKeys[7].time = 1.0f;

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
