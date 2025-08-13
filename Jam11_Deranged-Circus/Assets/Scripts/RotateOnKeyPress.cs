using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RotateOnKeyPress : MonoBehaviour
{
    [Tooltip("The maximum rotation speed in degrees per second.")]
    public float maxRotationSpeed = 90.0f;

    [Tooltip("How quickly the object reaches max speed.")]
    public float acceleration = 180.0f;

    [Tooltip("How quickly the object stops rotating when the key is released.")]
    public float deceleration = 360.0f;

    [Tooltip("The key that triggers the rotation when held down.")]
    public KeyCode rotationKey = KeyCode.R;

    private Rigidbody rb;
    private bool isRotationActive = false;
    private float currentRotationSpeed = 0.0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Read input in Update for responsiveness.
        isRotationActive = Input.GetKey(rotationKey);
    }

    void FixedUpdate()
    {
        // Apply acceleration or deceleration in FixedUpdate for smooth physics.
        if (isRotationActive)
        {
            // Accelerate towards max speed.
            currentRotationSpeed = Mathf.MoveTowards(currentRotationSpeed, maxRotationSpeed, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            // Decelerate towards zero.
            currentRotationSpeed = Mathf.MoveTowards(currentRotationSpeed, 0, deceleration * Time.fixedDeltaTime);
        }

        // Apply the calculated rotation if there is any speed.
        if (Mathf.Abs(currentRotationSpeed) > 0.01f)
        {
            Quaternion deltaRotation = Quaternion.Euler(Vector3.up * currentRotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }
}