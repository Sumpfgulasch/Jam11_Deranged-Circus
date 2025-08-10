using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
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
    }

    void FixedUpdate()
    {
        Move();
        Look();
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

    private void Look()
    {
        Quaternion horizontalRotation = Quaternion.Euler(0, lookInput.x, 0);
        rb.MoveRotation(rb.rotation * horizontalRotation);
        float verticalRotation = -lookInput.y;
        float currentPitch = playerCamera.transform.localRotation.eulerAngles.x;
        float newPitch = currentPitch + verticalRotation;

        if (newPitch > 180)
        {
            newPitch -= 360;
        }

        float clampedPitch = Mathf.Clamp(newPitch, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(clampedPitch, 0, 0);
    }
}