using UnityEngine;

/// <summary>
/// Richtet dieses Objekt immer zur Zielkamera aus,
/// sodass es wie ein 2D-Sprite im Raum wirkt.
/// </summary>
[ExecuteAlways]
public class Billboard : MonoBehaviour
{
    public Camera targetCamera;
    [Tooltip("Wenn deaktiviert, wird die Ausrichtung zur Kamera nicht mehr aktualisiert.")]
    public bool enableBillboard = false;

    void LateUpdate()
    {
        if (!enableBillboard)
            return;

        if (targetCamera == null)
        {
            var main = Camera.main;
            if (main == null) return;
            targetCamera = main;
        }

        Vector3 camPos = targetCamera.transform.position;
        Vector3 dir = transform.position - camPos;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        // Blickrichtung zur Kamera, Up-Achse bleibt global Up,
        // damit nichts "kippt".
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
