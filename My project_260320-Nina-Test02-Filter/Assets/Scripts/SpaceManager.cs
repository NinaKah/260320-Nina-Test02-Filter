using UnityEngine;

public class SpaceManager : MonoBehaviour
{
    [Header("Target")]
    public CircleManager circleManager;

    [Header("Default Mode")]
    public bool startWithSpace3D = false;

    void Awake()
    {
        if (circleManager == null)
            circleManager = FindObjectOfType<CircleManager>();
    }

    void Start()
    {
        if (circleManager == null) return;

        if (startWithSpace3D)
            circleManager.SetSpaceMode(CircleManager.SpaceMode.Space3D);
        else
            circleManager.SetSpaceMode(CircleManager.SpaceMode.Grid2D);
    }

    public void SetSpace3DMode(bool isOn)
    {
        if (!isOn || circleManager == null) return;
        circleManager.SetSpaceMode(CircleManager.SpaceMode.Space3D);
    }

    public void SetGrid2DMode(bool isOn)
    {
        if (!isOn || circleManager == null) return;
        circleManager.SetSpaceMode(CircleManager.SpaceMode.Grid2D);
    }

    public void ApplyRandom()
    {
        if (circleManager == null) return;
        circleManager.SetSpaceMode(CircleManager.SpaceMode.Space3D);
    }

    public void ApplyGrid()
    {
        if (circleManager == null) return;
        circleManager.SetSpaceMode(CircleManager.SpaceMode.Grid2D);
    }
}
