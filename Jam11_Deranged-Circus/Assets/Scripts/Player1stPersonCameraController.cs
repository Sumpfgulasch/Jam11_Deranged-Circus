using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player1stPersonCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public InputActionReference moveActionReference;

    [Header("Camera")]
    public Camera playerCamera;
    public float lookXLimit = 45.0f;
    public InputActionReference lookActionReference;
    [SerializeField] private float seatedExitLift = 0.35f;
    [SerializeField] private Vector3 seatedPositionOffset = new(0f, 0.55f, 0f);

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Rigidbody rb;
    private Collider[] colliders;
    private float cameraPitch;
    private bool isSeated;
    private float seatedYaw;
    private Transform seatedMount;
    private Vector3 externalPlatformVelocity;
    private Vector3 cameraBaseLocalPosition;
    private Vector3 cameraImpactPositionOffset;
    private float cameraImpactPitchOffset;
    private float cameraImpactReturnSpeed = 10f;

    public bool IsSeated => isSeated;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponents<Collider>();
        cameraBaseLocalPosition = playerCamera != null ? playerCamera.transform.localPosition : Vector3.zero;

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

    private void Update()
    {
        ReadInput();
        Look();
        UpdateSeatedTransform();
        UpdateCameraOffsets();
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void SetExternalPlatformVelocity(Vector3 worldVelocity)
    {
        externalPlatformVelocity = isSeated ? Vector3.zero : worldVelocity;
    }

    public void EnterSeatedMode(Transform seatMount)
    {
        if (seatMount == null)
        {
            return;
        }

        float seatBaseYaw = GetSeatBaseYaw(seatMount);
        seatedMount = seatMount;
        isSeated = true;
        seatedYaw = Mathf.DeltaAngle(seatBaseYaw, transform.eulerAngles.y);
        externalPlatformVelocity = Vector3.zero;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        transform.SetParent(seatMount, false);
        SyncSeatedTransform();
    }

    public void ExitSeatedMode(Vector3 inheritedVelocity)
    {
        Vector3 worldPosition = transform.position + Vector3.up * seatedExitLift;
        Vector3 flattenedForward = Vector3.ProjectOnPlane(playerCamera != null ? playerCamera.transform.forward : transform.forward, Vector3.up);
        if (flattenedForward.sqrMagnitude < 0.0001f && playerCamera != null)
        {
            flattenedForward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up);
        }

        if (flattenedForward.sqrMagnitude < 0.0001f)
        {
            flattenedForward = transform.forward;
            flattenedForward.y = 0f;
        }

        Quaternion worldRotation = flattenedForward.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(flattenedForward.normalized, Vector3.up)
            : Quaternion.identity;

        transform.SetParent(null, true);
        isSeated = false;
        seatedMount = null;
        externalPlatformVelocity = Vector3.zero;
        seatedYaw = 0f;
        transform.SetPositionAndRotation(worldPosition, worldRotation);
        rb.position = worldPosition;
        rb.rotation = worldRotation;

        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = inheritedVelocity;
        rb.WakeUp();
    }

    public void ApplyCameraImpact(float pitchOffset, float downwardOffset, float returnSpeed)
    {
        cameraImpactPitchOffset -= Mathf.Abs(pitchOffset);
        cameraImpactPositionOffset += Vector3.down * Mathf.Abs(downwardOffset);
        cameraImpactReturnSpeed = Mathf.Max(1f, returnSpeed);
    }

    private void ReadInput()
    {
        moveInput = moveActionReference.action.ReadValue<Vector2>();
        lookInput = lookActionReference.action.ReadValue<Vector2>();
    }

    private void Move()
    {
        if (isSeated)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 targetVelocity = moveDirection * moveSpeed + externalPlatformVelocity;
        targetVelocity.y += rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    private void Look()
    {
        if (isSeated)
        {
            seatedYaw += lookInput.x;
        }
        else
        {
            Quaternion horizontalRotation = Quaternion.Euler(0f, lookInput.x, 0f);
            rb.MoveRotation(rb.rotation * horizontalRotation);
        }

        cameraPitch -= lookInput.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -lookXLimit, lookXLimit);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch + cameraImpactPitchOffset, 0f, 0f);
        }
    }

    private void UpdateSeatedTransform()
    {
        if (!isSeated || seatedMount == null)
        {
            return;
        }

        SyncSeatedTransform();
    }

    private void SyncSeatedTransform()
    {
        if (seatedMount == null)
        {
            return;
        }

        transform.localPosition = seatedPositionOffset;
        transform.localRotation = Quaternion.Euler(0f, seatedYaw, 0f);
        rb.position = transform.position;
        rb.rotation = transform.rotation;
    }

    private float GetSeatBaseYaw(Transform seatTransform)
    {
        if (seatTransform == null)
        {
            return transform.eulerAngles.y;
        }

        Vector3 flattenedForward = Vector3.ProjectOnPlane(seatTransform.forward, Vector3.up);
        if (flattenedForward.sqrMagnitude < 0.0001f)
        {
            return transform.eulerAngles.y;
        }

        return Quaternion.LookRotation(flattenedForward.normalized, Vector3.up).eulerAngles.y;
    }

    private void UpdateCameraOffsets()
    {
        cameraImpactPitchOffset = Mathf.MoveTowards(cameraImpactPitchOffset, 0f, cameraImpactReturnSpeed * Time.deltaTime);
        cameraImpactPositionOffset = Vector3.MoveTowards(cameraImpactPositionOffset, Vector3.zero, cameraImpactReturnSpeed * Time.deltaTime * 0.05f);

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = cameraBaseLocalPosition + cameraImpactPositionOffset;
        }
    }
}
