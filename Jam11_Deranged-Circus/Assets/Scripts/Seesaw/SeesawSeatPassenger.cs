using UnityEngine;

[RequireComponent(typeof(SeesawWeightSource))]
public class SeesawSeatPassenger : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Rigidbody controlledRigidbody;
    [SerializeField] private Vector3 seatedLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 seatedLocalEulerAngles = Vector3.zero;

    private Collider[] colliders;
    private SeesawWeightSource weightSource;

    public SeesawWeightSource WeightSource => weightSource;
    public SeesawSeat CurrentSeat { get; private set; }
    public bool IsSeated => CurrentSeat != null;

    private void Awake()
    {
        visualRoot ??= transform;
        controlledRigidbody ??= GetComponent<Rigidbody>();
        weightSource = GetComponent<SeesawWeightSource>();
        colliders = GetComponentsInChildren<Collider>(true);
    }

    public void EnterSeat(SeesawSeat seat)
    {
        if (seat == null)
        {
            return;
        }

        CurrentSeat = seat;
        weightSource.SetSeated(true);

        if (controlledRigidbody != null)
        {
            controlledRigidbody.linearVelocity = Vector3.zero;
            controlledRigidbody.angularVelocity = Vector3.zero;
            controlledRigidbody.isKinematic = true;
            controlledRigidbody.useGravity = false;
        }

        SetCollidersEnabled(false);

        visualRoot.SetParent(seat.SeatMount, false);
        visualRoot.localPosition = seatedLocalPosition;
        visualRoot.localRotation = Quaternion.Euler(seatedLocalEulerAngles);
    }

    public void ExitSeat(Vector3 worldPosition, Quaternion worldRotation)
    {
        visualRoot.SetParent(null, true);
        visualRoot.SetPositionAndRotation(worldPosition, worldRotation);

        if (controlledRigidbody != null)
        {
            controlledRigidbody.isKinematic = false;
            controlledRigidbody.useGravity = true;
            controlledRigidbody.linearVelocity = Vector3.zero;
            controlledRigidbody.angularVelocity = Vector3.zero;
        }

        SetCollidersEnabled(true);
        weightSource.SetSeated(false);
        CurrentSeat = null;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabled;
            }
        }
    }
}
