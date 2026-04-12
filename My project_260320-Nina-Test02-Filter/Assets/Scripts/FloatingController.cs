using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingController : MonoBehaviour
{
    [Header("Floating Settings")]
    [Tooltip("Wenn aktiviert, floaten alle Elemente leicht auf und ab.")]
    public bool floatingEnabled = false;
    [Tooltip("Amplitude des Floatings in Welt-Einheiten.")]
    public float floatingAmplitude = 0.35f;
    [Tooltip("Frequenz des Floatings.")]
    public float floatingFrequency = 1.2f;
    [Tooltip("Seitliche Amplitude (X/Z) des Floatings.")]
    public Vector2 floatingAmplitudeXZ = new Vector2(0.15f, 0.15f);

    [Header("Debug")]
    public bool logToggleEvents = false;

    [Header("Targets")] 
    [Tooltip("Optional: Eltern-Transform, dessen Kinder gefloatet werden sollen. Wird vom CircleManager gesetzt, wenn leer.")]
    public Transform parentTransform;

    [Header("Toggle Sync (optional)")]
    [Tooltip("Wenn gesetzt, steuert dieser Toggle den Static-Modus (an = statisch).")]
    public Toggle staticToggle;
    [Tooltip("Wenn gesetzt, steuert dieser Toggle den Dynamic-Modus (an = floating).")]
    public Toggle dynamicToggle;
    [Tooltip("Wenn aktiv, wird floatingEnabled aus den Toggles gelesen (übersteuert Events).")]
    public bool syncFromToggles = true;

    private List<Transform> targets = new List<Transform>();
    private Vector3[] basePositions;
    private float[] phaseOffsets;
    private bool wasFloating = false;
    private bool lastSyncedFloating;

    void Awake()
    {
        if (parentTransform == null)
            parentTransform = transform;

        CollectTargetsFromParent();
        CaptureBasePositions();
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        if (syncFromToggles)
        {
            bool hasDynamic = dynamicToggle != null;
            bool hasStatic = staticToggle != null;

            if (hasDynamic)
            {
                floatingEnabled = dynamicToggle.isOn;
            }
            else if (hasStatic)
            {
                floatingEnabled = !staticToggle.isOn;
            }

            if (floatingEnabled != lastSyncedFloating)
            {
                lastSyncedFloating = floatingEnabled;
                if (logToggleEvents)
                    Debug.Log($"FloatingController: syncFromToggles => floatingEnabled = {floatingEnabled}", this);

                if (floatingEnabled)
                {
                    if ((targets == null || targets.Count == 0) && parentTransform != null)
                    {
                        CollectTargetsFromParent();
                    }
                    CaptureBasePositions();
                }
                else
                {
                    SnapToBasePositions();
                }
            }
        }

        if ((targets == null || targets.Count == 0) && parentTransform != null)
        {
            CollectTargetsFromParent();
            CaptureBasePositions();
        }

        if (floatingEnabled)
        {
            ApplyFloating();
        }
        else if (wasFloating)
        {
            SnapToBasePositions();
        }

        wasFloating = floatingEnabled;
    }

    public void SetStaticMode()
    {
        SetFloating(false);
    }

    public void SetDynamicMode()
    {
        SetFloating(true);
    }

    // Für UI-Toggles: wird mit dem bool-Wert (isOn) aufgerufen
    public void SetStaticMode(bool isOn)
    {
        if (isOn)
        {
            if (logToggleEvents) Debug.Log("FloatingController: SetStaticMode(true)", this);
            SetStaticMode();
        }
    }

    public void SetDynamicMode(bool isOn)
    {
        if (isOn)
        {
            if (logToggleEvents) Debug.Log("FloatingController: SetDynamicMode(true)", this);
            SetDynamicMode();
        }
    }

    // Direkte Toggle-Hooks, damit beide Toggles korrekt wirken
    // Dynamic: an = floaten, aus = statisch
    public void SetFloatingFromDynamicToggle(bool isOn)
    {
        if (logToggleEvents) Debug.Log($"FloatingController: DynamicToggle isOn={isOn}", this);
        SetFloating(isOn);
    }

    // Static: an = statisch (kein Floating), aus = floaten
    public void SetFloatingFromStaticToggle(bool isOn)
    {
        if (logToggleEvents) Debug.Log($"FloatingController: StaticToggle isOn={isOn}", this);
        SetFloating(!isOn);
    }

    public void SetFloating(bool enabled)
    {
        floatingEnabled = enabled;
        if (logToggleEvents) Debug.Log($"FloatingController: floatingEnabled = {floatingEnabled}", this);
        if (enabled)
        {
            if ((targets == null || targets.Count == 0) && parentTransform != null)
            {
                CollectTargetsFromParent();
            }
            CaptureBasePositions();
        }
        else
        {
            SnapToBasePositions();
        }
    }

    public void SetTargets(List<Transform> newTargets)
    {
        targets = newTargets ?? new List<Transform>();
        EnsureArrays();
        CaptureBasePositions();
    }

    public void CaptureBasePositions()
    {
        if (targets == null || targets.Count == 0)
            return;

        EnsureArrays();

        for (int i = 0; i < targets.Count; i++)
        {
            basePositions[i] = targets[i].localPosition;
        }
    }

    void CollectTargetsFromParent()
    {
        targets.Clear();
        if (parentTransform == null)
            return;

        for (int i = 0; i < parentTransform.childCount; i++)
        {
            targets.Add(parentTransform.GetChild(i));
        }

        EnsureArrays();
    }

    void EnsureArrays()
    {
        int count = (targets != null) ? targets.Count : 0;
        if (count <= 0)
            return;

        if (basePositions == null || basePositions.Length != count)
        {
            basePositions = new Vector3[count];
        }

        if (phaseOffsets == null || phaseOffsets.Length != count)
        {
            phaseOffsets = new float[count];
            for (int i = 0; i < count; i++)
            {
                phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }
    }

    void SnapToBasePositions()
    {
        if (basePositions == null || targets == null)
            return;

        int count = Mathf.Min(basePositions.Length, targets.Count);
        for (int i = 0; i < count; i++)
        {
            targets[i].localPosition = basePositions[i];
        }
    }

    void ApplyFloating()
    {
        if (basePositions == null || targets == null)
            return;

        int count = Mathf.Min(basePositions.Length, targets.Count);
        for (int i = 0; i < count; i++)
        {
            float phase = (phaseOffsets != null && phaseOffsets.Length > i) ? phaseOffsets[i] : 0f;
            float t = Time.time * floatingFrequency + phase;
            float offsetY = Mathf.Sin(t) * floatingAmplitude;
            float offsetX = Mathf.Sin(t * 0.9f + 1.1f) * floatingAmplitudeXZ.x;
            float offsetZ = Mathf.Cos(t * 0.8f + 0.4f) * floatingAmplitudeXZ.y;
            Vector3 pos = basePositions[i] + new Vector3(offsetX, offsetY, offsetZ);
            targets[i].localPosition = pos;
        }
    }
}
