using UnityEngine;

public class SimpleFlyCamera : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float sprintMultiplier = 2f;
    public float lookSensitivity = 2f;

    private float yaw;
    private float pitch;
    private float startHeight;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;

        startPosition = transform.position;
        startRotation = transform.rotation;
        startHeight = transform.position.y;

        // Hintergrund erzwingen: weiß
        var cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;
        }
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        // Wie im alten Projekt: nur schauen, wenn rechte Maustaste gehalten wird
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        Vector3 inputDir = new Vector3(
            Input.GetAxis("Horizontal"),  // A/D
            0f,
            Input.GetAxis("Vertical"));   // W/S

        Vector3 move = transform.TransformDirection(inputDir) * speed * Time.deltaTime;

        // Vertikalbewegung über Q/E
        if (Input.GetKey(KeyCode.E)) move += Vector3.up * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down * speed * Time.deltaTime;

        transform.position += move;
    }

    public void ResetToStart()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        yaw = startRotation.eulerAngles.y;
        pitch = startRotation.eulerAngles.x;
    }
}
