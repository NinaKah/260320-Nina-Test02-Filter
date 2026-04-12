using UnityEngine;

public class ColorModeManager : MonoBehaviour
{
    [Header("Color Settings")]
    public Color orange = new Color32(0xFF, 0xA0, 0x06, 0xFF); // #FFA006
    public Color violet = new Color32(0xC6, 0x75, 0xFF, 0xFF); // #C675FF
    public Color red = new Color32(0xAB, 0x0C, 0x20, 0xFF);    // #AB0C20

    [Header("Mode")]
    public bool grayscaleEnabled = false;

    [Header("Target")]
    public CircleManager circleManager;

    public void SetGrayscale(bool isOn)
    {
        grayscaleEnabled = isOn;
        ApplyToAll();
    }

    // Checkbox "Details": an = Farben, aus = Graustufen
    public void SetDetails(bool isOn)
    {
        grayscaleEnabled = !isOn;
        ApplyToAll();
    }

    public Color GetColor(ColorCategory category)
    {
        Color c = GetBaseColor(category);

        if (grayscaleEnabled)
        {
            float g = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            c = new Color(g, g, g, c.a);
        }

        return c;
    }

    public Color GetBaseColor(ColorCategory category)
    {
        switch (category)
        {
            case ColorCategory.Orange:
                return orange;
            case ColorCategory.Violet:
                return violet;
            case ColorCategory.Red:
            default:
                return red;
        }
    }

    public void ApplyToAll()
    {
        if (circleManager == null)
            return;

        circleManager.ApplyColorsFromManager(this);
    }
}
