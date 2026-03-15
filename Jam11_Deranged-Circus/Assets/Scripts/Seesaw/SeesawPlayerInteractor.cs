using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SeesawWeightSource))]
public class SeesawPlayerInteractor : MonoBehaviour
{
    [SerializeField] private Player1stPersonCameraController playerController;
    [SerializeField] private RopeInteractor ropeInteractor;
    [SerializeField] private string dismountActionName = "Dismount";
    [SerializeField] private string backActionName = "Back";
    [SerializeField] private string toggleGoatActionName = "ToggleGoatSeat";
    [SerializeField] private SeesawController goatController;
    [SerializeField] private SeesawSeatPassenger goatPassenger;
    [SerializeField] private Transform goatExitPoint;

    private readonly HashSet<SeesawSeat> nearbySeats = new();

    private InputAction interactAction;
    private InputAction dismountAction;
    private InputAction backAction;
    private InputAction toggleGoatAction;
    private SeesawController currentController;
    private SeesawWeightSource weightSource;
    private SeesawSide? currentSeatSide;

    public bool IsSeated => currentController != null;
    public SeesawWeightSource WeightSource => weightSource;
    public SeesawSide? CurrentSeatSide => currentSeatSide;

    private void Awake()
    {
        playerController ??= GetComponent<Player1stPersonCameraController>();
        ropeInteractor ??= GetComponent<RopeInteractor>();
        weightSource = GetComponent<SeesawWeightSource>();

        interactAction = ropeInteractor != null && ropeInteractor.interactActionReference != null
            ? ropeInteractor.interactActionReference.action
            : null;

        InputActionMap actionMap = playerController != null && playerController.moveActionReference != null
            ? playerController.moveActionReference.action.actionMap
            : null;
        dismountAction = actionMap?.FindAction(dismountActionName, false);
        backAction = actionMap?.FindAction(backActionName, false);
        toggleGoatAction = actionMap?.FindAction(toggleGoatActionName, false);
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.Enable();
            interactAction.started += OnInteractStarted;
            interactAction.canceled += OnInteractCanceled;
        }

        if (dismountAction != null)
        {
            dismountAction.Enable();
            dismountAction.started += OnDismountStarted;
        }

        if (backAction != null)
        {
            backAction.Enable();
            backAction.started += OnBackStarted;
        }

        if (toggleGoatAction != null)
        {
            toggleGoatAction.Enable();
            toggleGoatAction.started += OnToggleGoatStarted;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.started -= OnInteractStarted;
            interactAction.canceled -= OnInteractCanceled;
        }

        if (dismountAction != null)
        {
            dismountAction.started -= OnDismountStarted;
        }

        if (backAction != null)
        {
            backAction.started -= OnBackStarted;
        }

        if (toggleGoatAction != null)
        {
            toggleGoatAction.started -= OnToggleGoatStarted;
        }
    }

    public void RegisterNearbySeat(SeesawSeat seat)
    {
        if (seat != null)
        {
            nearbySeats.Add(seat);
        }
    }

    public void UnregisterNearbySeat(SeesawSeat seat)
    {
        if (seat != null)
        {
            nearbySeats.Remove(seat);
        }
    }

    public void EnterSeat(SeesawController controller, SeesawSeat seat)
    {
        currentController = controller;
        currentSeatSide = seat != null ? seat.Side : null;
        playerController.SetExternalPlatformVelocity(Vector3.zero);
        playerController.EnterSeatedMode(seat.SeatMount);
    }

    public void ExitSeat(Vector3 launchVelocity)
    {
        currentController = null;
        currentSeatSide = null;
        playerController.ExitSeatedMode(launchVelocity);
    }

    public void ApplyImpact(float normalizedIntensity, float maxPitch, float maxDip, float returnSpeed)
    {
        playerController.ApplyCameraImpact(maxPitch * normalizedIntensity, maxDip * normalizedIntensity, returnSpeed);
    }

    private void OnInteractStarted(InputAction.CallbackContext context)
    {
        if (currentController != null)
        {
            currentController.BeginHold(this);
            return;
        }

        SeesawSeat seat = GetClosestAvailableSeat();
        if (seat != null)
        {
            seat.Controller.TrySeat(this, seat);
        }
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        if (currentController != null)
        {
            currentController.EndHold(this);
        }
    }

    private void OnDismountStarted(InputAction.CallbackContext context)
    {
        if (currentController != null)
        {
            currentController.Dismount(this);
        }
    }

    private void OnBackStarted(InputAction.CallbackContext context)
    {
        if (currentController != null)
        {
            currentController.Dismount(this);
        }
    }

    private void OnToggleGoatStarted(InputAction.CallbackContext context)
    {
        if (goatController == null || goatPassenger == null || goatExitPoint == null)
        {
            return;
        }

        if (goatPassenger.IsSeated)
        {
            goatController.ReleasePassenger(goatPassenger, goatExitPoint.position, goatExitPoint.rotation);
            return;
        }

        SeesawSide? preferredSide = null;
        if (currentController == goatController && currentSeatSide.HasValue)
        {
            preferredSide = currentSeatSide.Value == SeesawSide.Left ? SeesawSide.Right : SeesawSide.Left;
        }

        SeesawSeat targetSeat = goatController.GetAvailableSeat(preferredSide);
        if (targetSeat != null)
        {
            goatController.TrySeatPassenger(goatPassenger, targetSeat);
        }
    }

    private SeesawSeat GetClosestAvailableSeat()
    {
        SeesawSeat bestSeat = null;
        float bestDistance = float.PositiveInfinity;

        foreach (SeesawSeat seat in nearbySeats)
        {
            if (seat == null || !seat.CanSeat(this))
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(transform.position - seat.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSeat = seat;
            }
        }

        return bestSeat;
    }
}
