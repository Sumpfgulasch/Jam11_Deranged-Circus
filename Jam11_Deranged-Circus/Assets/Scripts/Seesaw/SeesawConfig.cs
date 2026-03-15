using UnityEngine;

[CreateAssetMenu(fileName = "SeesawConfig", menuName = "Gameplay/Seesaw Config")]
public class SeesawConfig : ScriptableObject
{
    [Header("Rotation")]
    [Min(1f)] public float maxAngle = 28f;
    [Min(0.1f)] public float momentOfInertia = 6f;
    [Min(0.1f)] public float weightGravityScale = 18f;
    [Min(0f)] public float angularDamping = 8f;

    [Header("Ground Contact")]
    [Range(0f, 0.99f)] public float impactDamping = 0.72f;
    [Range(0f, 0.99f)] public float hardImpactDamping = 0.85f;
    [Min(1f)] public float hardImpactSpeed = 70f;
    [Range(0f, 1f)] public float reboundFactor = 0.28f;
    [Min(0f)] public float maxReboundSpeed = 16f;

    [Header("Push Window")]
    [Min(0.01f)] public float pushWindowDistance = 0.7f;
    [Min(0.01f)] public float optimalPushDistance = 0.08f;
    [Min(0.01f)] public float minimumGroundDistance = 0.04f;

    [Header("Push Tuning")]
    [Min(0f)] public float holdChargeRate = 1.2f;
    [Min(0f)] public float maxHoldCharge = 1f;
    [Min(0f)] public float earlyHoldBrake = 90f;
    [Min(0f)] public float releaseMomentumMultiplier = 0.65f;
    [Min(0f)] public float baseReleaseBoost = 10f;
    [Min(0f)] public float maxReleaseBoost = 28f;

    [Header("Seat Exit")]
    [Min(0f)] public float dismountVerticalVelocity = 5.5f;
    [Min(0f)] public float seatedVisualLiftVelocity = 1.25f;

    [Header("Camera Impact")]
    [Min(0f)] public float cameraImpactAngularSpeed = 45f;
    [Min(0f)] public float cameraImpactPitch = 4.5f;
    [Min(0f)] public float cameraImpactDip = 0.05f;
    [Min(0f)] public float cameraImpactReturnSpeed = 12f;
}
