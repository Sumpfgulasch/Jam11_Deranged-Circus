using System.Collections.Generic;
using UnityEngine;

public class SeesawController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SeesawConfig config;
    [SerializeField] private Transform pivotTransform;
    [SerializeField] private Transform beamTransform;
    [SerializeField] private Rigidbody beamRigidbody;
    [SerializeField] private SeesawSeat leftSeat;
    [SerializeField] private SeesawSeat rightSeat;
    [SerializeField] private Transform leftGroundProbe;
    [SerializeField] private Transform rightGroundProbe;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private SeesawAudioFeedback audioFeedback;

    private readonly HashSet<SeesawWeightSource> boardWeights = new();
    private readonly HashSet<Player1stPersonCameraController> trackedBoardPlayers = new();
    private readonly List<SeesawWeightSource> invalidWeights = new();

    private Quaternion beamBaseLocalRotation;
    private float currentAngle;
    private float angularVelocity;

    private SeatRuntime leftRuntime;
    private SeatRuntime rightRuntime;

    private sealed class SeatRuntime
    {
        public SeesawSeat Seat;
        public SeesawPlayerInteractor PlayerOccupant;
        public SeesawSeatPassenger Passenger;
        public SeesawWeightSource WeightSource;
        public bool HoldActive;
        public bool PendingRelease;
        public float HoldCharge;
        public bool MinimumDistanceTriggered;
    }

    private void Reset()
    {
        pivotTransform = transform;
        beamTransform = transform;
        beamRigidbody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        config ??= ScriptableObject.CreateInstance<SeesawConfig>();
        pivotTransform ??= transform;
        beamTransform ??= transform;
        beamRigidbody ??= beamTransform.GetComponent<Rigidbody>();

        if (beamRigidbody != null)
        {
            beamRigidbody.isKinematic = true;
        }

        beamBaseLocalRotation = beamTransform.localRotation;

        leftRuntime = new SeatRuntime { Seat = leftSeat };
        rightRuntime = new SeatRuntime { Seat = rightSeat };
    }

    private void OnDisable()
    {
        foreach (Player1stPersonCameraController player in trackedBoardPlayers)
        {
            if (player != null)
            {
                player.SetExternalPlatformVelocity(Vector3.zero);
            }
        }

        trackedBoardPlayers.Clear();
        leftSeat?.SetPushAvailability(0f);
        rightSeat?.SetPushAvailability(0f);
    }

    private void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        UpdateHoldState(deltaTime);

        float torque = ComputeTotalTorque();
        float angularAcceleration = torque / Mathf.Max(0.01f, config.momentOfInertia);
        angularVelocity += angularAcceleration * deltaTime;
        angularVelocity = Mathf.MoveTowards(angularVelocity, 0f, config.angularDamping * deltaTime);

        currentAngle += angularVelocity * deltaTime;

        HandleGroundContact(leftRuntime, config.maxAngle);
        HandleGroundContact(rightRuntime, -config.maxAngle);
        ResolvePendingRelease(leftRuntime);
        ResolvePendingRelease(rightRuntime);

        ApplyBeamRotation();
        UpdateBoardPlayerVelocities();
        UpdateSeatFeedback(leftRuntime, leftGroundProbe != null ? leftGroundProbe : leftSeat?.GroundProbe);
        UpdateSeatFeedback(rightRuntime, rightGroundProbe != null ? rightGroundProbe : rightSeat?.GroundProbe);
    }

    public void RegisterBoardWeight(SeesawWeightSource weightSource)
    {
        if (weightSource != null)
        {
            boardWeights.Add(weightSource);
        }
    }

    public void UnregisterBoardWeight(SeesawWeightSource weightSource)
    {
        if (weightSource == null)
        {
            return;
        }

        boardWeights.Remove(weightSource);
        if (weightSource.TryGetComponent(out Player1stPersonCameraController playerController))
        {
            playerController.SetExternalPlatformVelocity(Vector3.zero);
            trackedBoardPlayers.Remove(playerController);
        }
    }

    public bool TrySeat(SeesawPlayerInteractor playerInteractor, SeesawSeat seat)
    {
        if (playerInteractor == null || seat == null || !seat.CanSeat(playerInteractor))
        {
            return false;
        }

        SeatRuntime runtime = GetRuntime(seat.Side);
        if (runtime == null || runtime.Passenger != null || runtime.PlayerOccupant != null)
        {
            return false;
        }

        runtime.PlayerOccupant = playerInteractor;
        runtime.Passenger = null;
        runtime.WeightSource = playerInteractor.WeightSource;
        runtime.HoldActive = false;
        runtime.PendingRelease = false;
        runtime.HoldCharge = 0f;
        runtime.MinimumDistanceTriggered = false;

        runtime.WeightSource?.SetSeated(true);
        UnregisterBoardWeight(runtime.WeightSource);
        runtime.Seat?.SetOccupied(true);
        playerInteractor.EnterSeat(this, seat);
        return true;
    }

    public SeesawSeat GetSeat(SeesawSide side)
    {
        return side == SeesawSide.Left ? leftSeat : rightSeat;
    }

    public SeesawSeat GetAvailableSeat(SeesawSide? preferredSide = null)
    {
        if (preferredSide.HasValue)
        {
            SeesawSeat preferredSeat = GetSeat(preferredSide.Value);
            if (preferredSeat != null && preferredSeat.IsAvailable)
            {
                return preferredSeat;
            }
        }

        if (leftSeat != null && leftSeat.IsAvailable)
        {
            return leftSeat;
        }

        if (rightSeat != null && rightSeat.IsAvailable)
        {
            return rightSeat;
        }

        return null;
    }

    public bool TrySeatPassenger(SeesawSeatPassenger passenger, SeesawSeat requestedSeat = null)
    {
        if (passenger == null)
        {
            return false;
        }

        SeesawSeat seat = requestedSeat != null && requestedSeat.IsAvailable
            ? requestedSeat
            : GetAvailableSeat();
        if (seat == null)
        {
            return false;
        }

        SeatRuntime runtime = GetRuntime(seat.Side);
        if (runtime == null || runtime.Passenger != null || runtime.PlayerOccupant != null)
        {
            return false;
        }

        runtime.PlayerOccupant = null;
        runtime.Passenger = passenger;
        runtime.WeightSource = passenger.WeightSource;
        runtime.HoldActive = false;
        runtime.PendingRelease = false;
        runtime.HoldCharge = 0f;
        runtime.MinimumDistanceTriggered = false;

        runtime.WeightSource?.SetSeated(true);
        UnregisterBoardWeight(runtime.WeightSource);
        runtime.Seat?.SetOccupied(true);
        passenger.EnterSeat(seat);
        return true;
    }

    public void BeginHold(SeesawPlayerInteractor playerInteractor)
    {
        SeatRuntime runtime = FindRuntime(playerInteractor);
        if (runtime != null)
        {
            runtime.HoldActive = true;
            runtime.PendingRelease = false;
        }
    }

    public void EndHold(SeesawPlayerInteractor playerInteractor)
    {
        SeatRuntime runtime = FindRuntime(playerInteractor);
        if (runtime == null)
        {
            return;
        }

        runtime.HoldActive = false;
        runtime.PendingRelease = true;
    }

    public void Dismount(SeesawPlayerInteractor playerInteractor)
    {
        SeatRuntime runtime = FindRuntime(playerInteractor);
        if (runtime == null)
        {
            return;
        }

        Vector3 seatVelocity = GetPointVelocity(runtime.Seat.SeatMount.position);
        Vector3 launchVelocity = seatVelocity + Vector3.up * config.dismountVerticalVelocity;

        runtime.WeightSource?.SetSeated(false);
        UnregisterBoardWeight(runtime.WeightSource);
        runtime.Seat?.SetOccupied(false);
        runtime.Seat?.SetPushAvailability(0f);

        playerInteractor.ExitSeat(launchVelocity);

        runtime.PlayerOccupant = null;
        runtime.Passenger = null;
        runtime.WeightSource = null;
        runtime.HoldActive = false;
        runtime.PendingRelease = false;
        runtime.HoldCharge = 0f;
        runtime.MinimumDistanceTriggered = false;
    }

    public bool ReleasePassenger(SeesawSeatPassenger passenger, Vector3 worldPosition, Quaternion worldRotation)
    {
        SeatRuntime runtime = FindRuntime(passenger);
        if (runtime == null)
        {
            return false;
        }

        runtime.WeightSource?.SetSeated(false);
        UnregisterBoardWeight(runtime.WeightSource);
        runtime.Seat?.SetOccupied(false);
        runtime.Seat?.SetPushAvailability(0f);

        passenger.ExitSeat(worldPosition, worldRotation);

        runtime.PlayerOccupant = null;
        runtime.Passenger = null;
        runtime.WeightSource = null;
        runtime.HoldActive = false;
        runtime.PendingRelease = false;
        runtime.HoldCharge = 0f;
        runtime.MinimumDistanceTriggered = false;
        return true;
    }

    public Vector3 GetPointVelocity(Vector3 worldPoint)
    {
        Vector3 worldAxis = pivotTransform.TransformDirection(Vector3.forward).normalized;
        Vector3 angularVelocityVector = worldAxis * (angularVelocity * Mathf.Deg2Rad);
        Vector3 radius = worldPoint - pivotTransform.position;
        return Vector3.Cross(angularVelocityVector, radius);
    }

    public float GetVisualLiftVelocityThreshold()
    {
        return config.seatedVisualLiftVelocity;
    }

    private float ComputeTotalTorque()
    {
        float torque = 0f;

        torque += ComputeSeatTorque(leftRuntime);
        torque += ComputeSeatTorque(rightRuntime);

        invalidWeights.Clear();
        foreach (SeesawWeightSource weightSource in boardWeights)
        {
            if (weightSource == null)
            {
                invalidWeights.Add(weightSource);
                continue;
            }

            if (weightSource.IsSeated || weightSource.Weight <= 0f)
            {
                continue;
            }

            float localX = beamTransform.InverseTransformPoint(weightSource.transform.position).x;
            torque += ComputeTorqueForWeight(weightSource.Weight, localX);
        }

        for (int i = 0; i < invalidWeights.Count; i++)
        {
            boardWeights.Remove(invalidWeights[i]);
        }

        return torque;
    }

    private float ComputeSeatTorque(SeatRuntime runtime)
    {
        if (runtime == null || runtime.Seat == null || runtime.WeightSource == null)
        {
            return 0f;
        }

        float leverArm = runtime.Seat.GetLeverArm(beamTransform);
        return ComputeTorqueForWeight(runtime.WeightSource.Weight, leverArm);
    }

    private float ComputeTorqueForWeight(float weight, float leverArm)
    {
        return -weight * leverArm * Mathf.Abs(Physics.gravity.y) * config.weightGravityScale;
    }

    private void UpdateHoldState(float deltaTime)
    {
        UpdateHoldState(leftRuntime, deltaTime);
        UpdateHoldState(rightRuntime, deltaTime);
    }

    private void UpdateHoldState(SeatRuntime runtime, float deltaTime)
    {
        if (runtime == null || runtime.PlayerOccupant == null || runtime.Seat == null)
        {
            return;
        }

        float pushAvailability = GetPushAvailability(runtime.Seat.GroundProbe);
        runtime.Seat.SetPushAvailability(pushAvailability);

        if (!runtime.HoldActive || pushAvailability <= 0f)
        {
            return;
        }

        runtime.HoldCharge = Mathf.Clamp(runtime.HoldCharge + config.holdChargeRate * pushAvailability * deltaTime, 0f, config.maxHoldCharge);

        float downwardAngularSpeed = Mathf.Max(0f, angularVelocity * runtime.Seat.Side.DownwardAngularSign());
        if (downwardAngularSpeed <= 0f)
        {
            return;
        }

        float brakeAmount = Mathf.Min(downwardAngularSpeed, config.earlyHoldBrake * pushAvailability * deltaTime);
        angularVelocity -= runtime.Seat.Side.DownwardAngularSign() * brakeAmount;
    }

    private void TryApplyReleaseBoost(SeatRuntime runtime)
    {
        if (runtime == null || runtime.Seat == null || runtime.HoldCharge <= 0f)
        {
            return;
        }

        float pushAvailability = GetPushAvailability(runtime.Seat.GroundProbe);
        if (pushAvailability <= 0f)
        {
            return;
        }

        float upwardAngularSpeed = Mathf.Max(0f, angularVelocity * runtime.Seat.Side.UpwardAngularSign());
        float boost = upwardAngularSpeed * (runtime.HoldCharge * config.releaseMomentumMultiplier);
        boost += runtime.HoldCharge * config.baseReleaseBoost;
        boost = Mathf.Min(boost, config.maxReleaseBoost);

        angularVelocity += runtime.Seat.Side.UpwardAngularSign() * boost;
    }

    private void HandleGroundContact(SeatRuntime runtime, float angleLimit)
    {
        if (runtime == null || runtime.Seat == null)
        {
            return;
        }

        bool exceededLimit = angleLimit < 0f ? currentAngle < angleLimit : currentAngle > angleLimit;
        if (!exceededLimit)
        {
            return;
        }

        currentAngle = angleLimit;

        float intoGroundSpeed = Mathf.Max(0f, angularVelocity * runtime.Seat.Side.DownwardAngularSign());
        if (intoGroundSpeed <= 0f)
        {
            return;
        }

        float damping = intoGroundSpeed >= config.hardImpactSpeed ? config.hardImpactDamping : config.impactDamping;
        float reboundSpeed = intoGroundSpeed * (1f - damping) * config.reboundFactor;
        reboundSpeed = Mathf.Min(reboundSpeed, config.maxReboundSpeed);
        angularVelocity = runtime.Seat.Side.UpwardAngularSign() * reboundSpeed;

        float normalizedImpact = Mathf.InverseLerp(config.cameraImpactAngularSpeed, config.hardImpactSpeed, intoGroundSpeed);
        if (runtime.PlayerOccupant != null && normalizedImpact > 0f)
        {
            runtime.PlayerOccupant.ApplyImpact(normalizedImpact, config.cameraImpactPitch, config.cameraImpactDip, config.cameraImpactReturnSpeed);
        }

        audioFeedback?.PlayImpact();
    }

    private void ResolvePendingRelease(SeatRuntime runtime)
    {
        if (runtime == null || !runtime.PendingRelease)
        {
            return;
        }

        TryApplyReleaseBoost(runtime);
        runtime.PendingRelease = false;
        runtime.HoldCharge = 0f;
    }

    private void ApplyBeamRotation()
    {
        Quaternion localRotation = beamBaseLocalRotation * Quaternion.AngleAxis(currentAngle, Vector3.forward);

        if (beamRigidbody != null)
        {
            Transform beamParent = beamTransform.parent;
            Quaternion worldRotation = beamParent != null ? beamParent.rotation * localRotation : localRotation;
            beamRigidbody.MoveRotation(worldRotation);
        }
        else
        {
            beamTransform.localRotation = localRotation;
        }
    }

    private void UpdateBoardPlayerVelocities()
    {
        foreach (Player1stPersonCameraController player in trackedBoardPlayers)
        {
            if (player != null)
            {
                player.SetExternalPlatformVelocity(Vector3.zero);
            }
        }

        trackedBoardPlayers.Clear();

        foreach (SeesawWeightSource weightSource in boardWeights)
        {
            if (weightSource == null || weightSource.IsSeated)
            {
                continue;
            }

            if (!weightSource.TryGetComponent(out Player1stPersonCameraController playerController))
            {
                continue;
            }

            playerController.SetExternalPlatformVelocity(GetPointVelocity(weightSource.transform.position));
            trackedBoardPlayers.Add(playerController);
        }
    }

    private void UpdateSeatFeedback(SeatRuntime runtime, Transform probe)
    {
        if (runtime == null || runtime.Seat == null)
        {
            return;
        }

        if (runtime.PlayerOccupant == null)
        {
            runtime.Seat.SetPushAvailability(0f);
            runtime.MinimumDistanceTriggered = false;
            return;
        }

        float pushAvailability = GetPushAvailability(probe);
        runtime.Seat.SetPushAvailability(pushAvailability);

        float groundDistance = GetGroundDistance(probe);
        bool reachedMinimum = groundDistance <= config.minimumGroundDistance;

        if (reachedMinimum && !runtime.MinimumDistanceTriggered)
        {
            runtime.MinimumDistanceTriggered = true;
            audioFeedback?.PlayMinimumDistance();
        }
        else if (!reachedMinimum && groundDistance > config.minimumGroundDistance + 0.02f)
        {
            runtime.MinimumDistanceTriggered = false;
        }
    }

    private float GetPushAvailability(Transform probe)
    {
        float groundDistance = GetGroundDistance(probe);
        if (float.IsPositiveInfinity(groundDistance) || groundDistance > config.pushWindowDistance)
        {
            return 0f;
        }

        if (groundDistance <= config.optimalPushDistance)
        {
            return 1f;
        }

        return 1f - Mathf.InverseLerp(config.optimalPushDistance, config.pushWindowDistance, groundDistance);
    }

    private float GetGroundDistance(Transform probe)
    {
        if (probe == null)
        {
            return float.PositiveInfinity;
        }

        Vector3 rayOrigin = probe.position + Vector3.up * 0.05f;
        float rayLength = Mathf.Max(config.pushWindowDistance + 0.1f, config.minimumGroundDistance + 0.1f);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundLayers, QueryTriggerInteraction.Ignore))
        {
            return Mathf.Max(0f, hit.distance - 0.05f);
        }

        return float.PositiveInfinity;
    }

    private SeatRuntime GetRuntime(SeesawSide side)
    {
        return side == SeesawSide.Left ? leftRuntime : rightRuntime;
    }

    private SeatRuntime FindRuntime(SeesawPlayerInteractor playerInteractor)
    {
        if (leftRuntime != null && leftRuntime.PlayerOccupant == playerInteractor)
        {
            return leftRuntime;
        }

        if (rightRuntime != null && rightRuntime.PlayerOccupant == playerInteractor)
        {
            return rightRuntime;
        }

        return null;
    }

    private SeatRuntime FindRuntime(SeesawSeatPassenger passenger)
    {
        if (leftRuntime != null && leftRuntime.Passenger == passenger)
        {
            return leftRuntime;
        }

        if (rightRuntime != null && rightRuntime.Passenger == passenger)
        {
            return rightRuntime;
        }

        return null;
    }
}
