using UnityEngine;
using Obi;
using System.Linq;
using Audio;
using System.Reflection;
using FMOD.Studio;

[RequireComponent(typeof(ObiRope))]
public class ObiRopeSounds : MonoBehaviour
{
    private ObiRope rope;
    private EventInstance ropeSound;

    void Awake()
    {
        rope = GetComponent<ObiRope>();
    }

    void OnEnable()
    {
        rope.OnBlueprintLoaded += Rope_OnBlueprintLoaded;
        if (rope.isLoaded)
            Rope_OnBlueprintLoaded(rope, rope.sourceBlueprint);
    }

    void OnDisable()
    {
        rope.OnBlueprintLoaded -= Rope_OnBlueprintLoaded;
        if (rope.solver != null)
            rope.solver.OnCollision -= Solver_OnCollision;
    }

    private void Rope_OnBlueprintLoaded(ObiActor actor, ObiActorBlueprint blueprint)
    {
        if (rope.solver != null)
        {
            rope.solver.OnCollision += Solver_OnCollision;
        }
    }

    void Start()
    {
        // The sound will start at the rope's initial position, but will be updated in Update()
        ropeSound = AudioManager.Instance.Play3DAudio(AudioEvent.Chain, transform);
    }

    void Update()
    {
        if (rope == null || !rope.isLoaded)
            return;

        Vector3 averagePosition = GetAverageParticlePosition();
        float averageVelocityOfFastestParticles = GetAverageVelocityOfFastestParticles(0.3f);

        AudioManager.Instance.SetLocalParameter(ropeSound, "ChainVelocity", averageVelocityOfFastestParticles);
        ropeSound.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(averagePosition));
    }

    public Vector3 GetAverageParticlePosition()
    {
        if (rope.solver == null || rope.activeParticleCount == 0)
            return Vector3.zero;

        Vector3 totalPosition = Vector3.zero;
        for (int i = 0; i < rope.activeParticleCount; ++i)
        {
            int solverIndex = rope.solverIndices[i];
            // Convert particle position from solver's local space to world space
            totalPosition += rope.solver.transform.TransformPoint(rope.solver.positions[solverIndex]);
        }

        return totalPosition / rope.activeParticleCount;
    }

    public float GetAverageVelocityOfFastestParticles(float percentage)
    {
        if (rope.solver == null || rope.activeParticleCount == 0)
            return 0f;

        int particleCount = rope.activeParticleCount;
        float[] speeds = new float[particleCount];

        for (int i = 0; i < particleCount; ++i)
        {
            int solverIndex = rope.solverIndices[i];
            speeds[i] = rope.solver.velocities[solverIndex].magnitude;
        }

        // Sort speeds in descending order
        var sortedSpeeds = speeds.OrderByDescending(s => s);

        // Determine the number of particles to include in the average
        int countToAverage = Mathf.CeilToInt(particleCount * percentage);

        // Take the top 'countToAverage' speeds and calculate their average
        float averageVelocity = sortedSpeeds.Take(countToAverage).Average();

        return averageVelocity;
    }

    private void Solver_OnCollision(ObiSolver solver, ObiNativeContactList contacts)
    {
        float totalImpulse = 0;
        float totalGroundVelocity = 0;
        int groundContacts = 0;
        var world = ObiColliderWorld.GetInstance();

        foreach (var contact in contacts)
        {
            // check if one of the bodies involved in the collision is a particle.
            if (contact.distance < 0.01f)
            {
                var contactActor = solver.particleToActor[contact.bodyA];
                if (contactActor.actor != rope)
                    continue;

                // check if the other body is a collider:
                var collider = world.colliderHandles[contact.bodyB].owner;
                if (collider != null && collider.gameObject.CompareTag("Ground"))
                {
                    totalImpulse += contact.normalImpulse;

                    // Get the velocity of the colliding particle
                    Vector3 particleVelocity = solver.velocities[contact.bodyA];
                    totalGroundVelocity += particleVelocity.magnitude;
                    groundContacts++;
                }
            }
        }

        if (totalImpulse > 0.7f)
        {
            ropeSound.getParameterByName("ChainCollisionStrength", out float currentFmodValue);
            AudioManager.Instance.SetLocalParameter(ropeSound, "ChainCollisionStrength", currentFmodValue + totalImpulse);
            print("collision; adding value " + totalImpulse + ", total value: " + totalImpulse + currentFmodValue);
        }

        if (groundContacts > 0)
        {
            print("total ground velocity: " + totalGroundVelocity);
            float averageGroundVelocity = totalGroundVelocity / groundContacts;
            AudioManager.Instance.SetLocalParameter(ropeSound, "GroundVelocity", averageGroundVelocity);
        }
        else
            AudioManager.Instance.SetLocalParameter(ropeSound, "GroundVelocity", 0);
    }
}