using UnityEngine;

[System.Serializable]
public class FrequencyColorMap
{
    [SerializeField]
    private Gradient frequencyToColor;

    // Static helper to return a default gradient
    public static Gradient DefaultGradient()
    {
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[8];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        colorKeys[0].color = HexToColor("#1438FF"); colorKeys[0].time = 0.0f;
        colorKeys[1].color = HexToColor("#33CCFF"); colorKeys[1].time = 0.15f;
        colorKeys[2].color = HexToColor("#33FF77"); colorKeys[2].time = 0.35f;
        colorKeys[3].color = HexToColor("#FFFF33"); colorKeys[3].time = 0.5f;
        colorKeys[4].color = HexToColor("#FF9933"); colorKeys[4].time = 0.65f;
        colorKeys[5].color = HexToColor("#FF3333"); colorKeys[5].time = 0.75f;
        colorKeys[6].color = HexToColor("#FF33CC"); colorKeys[6].time = 0.9f;
        colorKeys[7].color = HexToColor("#9933FF"); colorKeys[7].time = 1.0f;

        alphaKeys[0].alpha = 1f; alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = 1f; alphaKeys[1].time = 1f;

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    public FrequencyColorMap()
    {
        frequencyToColor = DefaultGradient();
    }

    public Color evaluate(float position)
    {
        return frequencyToColor.Evaluate(Mathf.Clamp01(position));
    }

    private static Color HexToColor(string hex)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString(hex, out c)) return c;
        return Color.white;
    }
}
