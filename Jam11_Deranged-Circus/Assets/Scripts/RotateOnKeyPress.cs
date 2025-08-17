using Audio;
using FMOD.Studio;
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

    [Header("Y-Move Settings")]

    [Tooltip("The maximum height the object will move up to.")]
    public float maxHeight = 2.0f;

    [Tooltip("How quickly the object moves up and down.")]
    public float moveSpeed = 2.0f;

    private Rigidbody rb;
    private bool isRotationActive = false;
    private bool isMoveActive = false;
    private float currentRotationSpeed = 0.0f;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private EventInstance audio;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalPosition = transform.position;
    }

    void Update()
    {
        // Read input in Update for responsiveness.
        isRotationActive = Input.GetKey(rotationKey);

        // Audio
        if (Input.GetKeyDown(rotationKey))
        {
            audio = AudioManager.Instance.Play3DAudio(AudioEvent.MachineInUse, transform);
        }
        else if (Input.GetKeyUp(rotationKey) && audio.isValid())
        {
            AudioManager.Instance.StopAudio(audio);
        }
    }

    void FixedUpdate()
    {
        // Handle Rotation
        if (isRotationActive)
        {
            // Accelerate towards max speed.
            currentRotationSpeed = Mathf.MoveTowards(currentRotationSpeed, maxRotationSpeed, acceleration * Time.fixedDeltaTime);
            targetPosition = originalPosition + new Vector3(0, maxHeight, 0);
        }
        else
        {
            // Decelerate towards zero.
            currentRotationSpeed = Mathf.MoveTowards(currentRotationSpeed, 0, deceleration * Time.fixedDeltaTime);
            targetPosition = originalPosition;
        }

        // Apply the calculated rotation if there is any speed.
        if (Mathf.Abs(currentRotationSpeed) > 0.01f)
        {
            Quaternion deltaRotation = Quaternion.Euler(Vector3.up * currentRotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
        
        AudioManager.Instance.SetGlobalParameter("MachineRotationSpeed", currentRotationSpeed);

        if ((targetPosition - rb.position).magnitude > 0.01f) {
            rb.MovePosition(Vector3.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime));

            AudioManager.Instance.SetLocalParameter(audio, "MachineVerticalMovement", 1f);
        } else {
            AudioManager.Instance.SetLocalParameter(audio, "MachineVerticalMovement", 0);
        }
    }
}