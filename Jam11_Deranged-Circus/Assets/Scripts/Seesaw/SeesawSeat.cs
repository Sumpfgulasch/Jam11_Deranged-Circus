using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SeesawSeat : MonoBehaviour
{
    [SerializeField] private SeesawController controller;
    [SerializeField] private SeesawSide side = SeesawSide.Left;
    [SerializeField] private Transform seatMount;
    [SerializeField] private Transform groundProbe;
    [SerializeField] private SeesawGlowFeedback seatGlow;
    [SerializeField] private SeesawGlowFeedback pushBarGlow;

    private readonly HashSet<SeesawPlayerInteractor> nearbyPlayers = new();

    public SeesawController Controller => controller;
    public SeesawSide Side => side;
    public Transform SeatMount => seatMount != null ? seatMount : transform;
    public Transform GroundProbe => groundProbe != null ? groundProbe : SeatMount;
    public bool IsOccupied { get; private set; }
    public bool IsAvailable => !IsOccupied;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void Awake()
    {
        UpdateSeatGlow();
        SetPushAvailability(0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        SeesawPlayerInteractor player = other.GetComponentInParent<SeesawPlayerInteractor>();
        if (player == null)
        {
            return;
        }

        nearbyPlayers.Add(player);
        player.RegisterNearbySeat(this);
        UpdateSeatGlow();
    }

    private void OnTriggerExit(Collider other)
    {
        SeesawPlayerInteractor player = other.GetComponentInParent<SeesawPlayerInteractor>();
        if (player == null)
        {
            return;
        }

        nearbyPlayers.Remove(player);
        player.UnregisterNearbySeat(this);
        UpdateSeatGlow();
    }

    public bool CanSeat(SeesawPlayerInteractor player)
    {
        return !IsOccupied && player != null && nearbyPlayers.Contains(player);
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
        if (occupied)
        {
            SetPushAvailability(0f);
        }

        UpdateSeatGlow();
    }

    public void SetPushAvailability(float normalizedAvailability)
    {
        if (pushBarGlow != null)
        {
            pushBarGlow.SetIntensity(IsOccupied ? Mathf.Clamp01(normalizedAvailability) : 0f);
        }
    }

    public float GetLeverArm(Transform beamTransform)
    {
        return beamTransform.InverseTransformPoint(SeatMount.position).x;
    }

    private void UpdateSeatGlow()
    {
        if (seatGlow != null)
        {
            seatGlow.SetIntensity(!IsOccupied && nearbyPlayers.Count > 0 ? 1f : 0f);
        }
    }
}
