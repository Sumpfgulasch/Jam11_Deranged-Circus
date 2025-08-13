using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player1stPersonCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public InputActionReference moveActionReference;
    private Vector2 moveInput;
    private Rigidbody rb;

    [Header("Camera")]
    public Camera playerCamera;
    public float lookXLimit = 45.0f;
    public InputActionReference lookActionReference;
    private Vector2 lookInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        moveActionReference.action.Enable();
        lookActionReference.action.Enable();
    }

    private void OnDisable()
    {
        moveActionReference.action.Disable();
        lookActionReference.action.Disable();
    }

    void Update()
    {
        ReadInput();
        Look();
    }

    void FixedUpdate()
    {
        Move();
    }

    private void ReadInput()
    {
        moveInput = moveActionReference.action.ReadValue<Vector2>();
        lookInput = lookActionReference.action.ReadValue<Vector2>();
    }

    private void Move()
    {
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        rb.linearVelocity = moveDirection * moveSpeed;
    }

    private float cameraPitch = 0.0f;

    private void Look()
    {
        // Horizontal rotation for the whole player body (physics-based)
        Quaternion horizontalRotation = Quaternion.Euler(0, lookInput.x, 0);
        rb.MoveRotation(rb.rotation * horizontalRotation);

        // Vertical rotation for the camera only (transform-based)
        cameraPitch -= lookInput.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -lookXLimit, lookXLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }
}