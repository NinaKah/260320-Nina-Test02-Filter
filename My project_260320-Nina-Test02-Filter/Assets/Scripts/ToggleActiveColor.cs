using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleActiveColor : MonoBehaviour
{
    [Header("Colors")]
    public Color activeColor = new Color(0f, 0.4f, 1f, 1f);
    public Color inactiveColor = Color.white;

    [Header("Target")]
    public Image targetImage;

    Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        if (targetImage == null)
            targetImage = toggle.targetGraphic as Image;

        Apply();
    }

    void OnEnable()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        toggle.onValueChanged.AddListener(OnToggleChanged);
        Apply();
    }

    void OnDisable()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    void OnToggleChanged(bool isOn)
    {
        Apply();
    }

    void Apply()
    {
        if (targetImage == null || toggle == null)
            return;

        targetImage.color = toggle.isOn ? activeColor : inactiveColor;
    }
}
