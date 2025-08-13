using Obi;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ObiRope))]
public class RopeController : MonoBehaviour
{
    public enum RopeEndState
    {
        Free,
        Held,
        Plugged
    }

    [System.Serializable]
    public class RopeEnd
    {
        public ObiParticleAttachment attachment;
        [HideInInspector] public RopeEndState state = RopeEndState.Free;
        [HideInInspector] public int particleIndexInActor;
        [HideInInspector] public int particleIndexInSolver;
    }

    [Header("Setup")]
    public RopeEnd startEnd;
    public RopeEnd endEnd;

    private ObiRope rope;
    private Coroutine activeTransition = null;

    void Awake()
    {
        rope = GetComponent<ObiRope>();
    }

    void OnEnable()
    {
        rope.OnBlueprintLoaded += OnRopeLoaded;
        if (rope.isLoaded)
            OnRopeLoaded(rope, rope.sourceBlueprint);
    }

    void OnDisable()
    {
        rope.OnBlueprintLoaded -= OnRopeLoaded;
    }

    private void OnRopeLoaded(ObiActor actor, ObiActorBlueprint blueprint)
    {
        if (rope.elements.Count > 0)
        {
            // Configure Start End
            startEnd.particleIndexInActor = 0;
            startEnd.particleIndexInSolver = rope.solverIndices[0];
            ConfigureAttachment(startEnd);

            // Configure End End
            endEnd.particleIndexInActor = rope.activeParticleCount - 1;
            endEnd.particleIndexInSolver = rope.solverIndices[rope.activeParticleCount - 1];
            ConfigureAttachment(endEnd);
        }
    }

    public Vector3 GetRopeEndPosition(RopeEnd ropeEnd)
    {
        if (rope != null && rope.isLoaded && rope.solver != null && ropeEnd.particleIndexInSolver >= 0 && ropeEnd.particleIndexInSolver < rope.solver.positions.count)
            return rope.solver.transform.TransformPoint(rope.solver.positions[ropeEnd.particleIndexInSolver]);
        return Vector3.zero;
    }

    private void ConfigureAttachment(RopeEnd ropeEnd)
    {
        if (ropeEnd.attachment != null)
        {
            if (ropeEnd.attachment.particleGroup == null)
            {
                var group = ScriptableObject.CreateInstance<ObiParticleGroup>();
                group.name = (ropeEnd == startEnd ? "Start" : "End") + "ParticleGroup";
                ropeEnd.attachment.particleGroup = group;
            }
            ropeEnd.attachment.particleGroup.particleIndices.Clear();
            ropeEnd.attachment.particleGroup.particleIndices.Add(ropeEnd.particleIndexInActor);
            ropeEnd.attachment.enabled = false;
        }
    }

    public void Grab(RopeEnd ropeEnd, Transform grabTarget, float transitionDuration)
    {
        if (ropeEnd.state == RopeEndState.Free && activeTransition == null)
        {
            // Create a temporary, non-parented transform to act as the attachment point.
            Transform tempTarget = new GameObject("TempGrabTarget").transform;
            tempTarget.position = GetRopeEndPosition(ropeEnd);
            
            // Enable the attachment and point it to our temporary target first.
            ropeEnd.attachment.target = tempTarget;
            ropeEnd.attachment.enabled = true;

            // This coroutine will move the tempTarget to the player's hand, then assign the hand as the permanent target.
            activeTransition = StartCoroutine(TransitionGrab(ropeEnd, tempTarget, grabTarget, transitionDuration));
        }
    }

    public void Plug(RopeEnd ropeEnd, Transform plugTarget, float transitionDuration)
    {
        if (ropeEnd.state == RopeEndState.Held && activeTransition == null)
        {
            // The current target is the player's hand. We need to move from its position.
            Transform currentTarget = ropeEnd.attachment.target;
            
            // Create a new temporary target that will be moved.
            Transform tempTarget = new GameObject("TempPlugTarget").transform;
            tempTarget.position = currentTarget.position;
            tempTarget.rotation = currentTarget.rotation;

            // Point the attachment to the temporary target before starting the transition.
            ropeEnd.attachment.target = tempTarget;

            System.Action onComplete = () => {
                ropeEnd.attachment.target = plugTarget; // Assign the final plug as the target.
                ropeEnd.state = RopeEndState.Plugged;
                Destroy(tempTarget.gameObject);
            };

            activeTransition = StartCoroutine(TransitionToPosition(ropeEnd, tempTarget, plugTarget, transitionDuration, onComplete));
        }
    }

    public void Drop(RopeEnd ropeEnd, Transform playerGrabTarget, float transitionDuration)
    {
        if (ropeEnd.state == RopeEndState.Held && activeTransition == null)
        {
            // The current target is the player's hand.
            Transform currentTarget = ropeEnd.attachment.target;

            // Calculate the release position.
            Vector3 releasePosition = currentTarget.position + currentTarget.forward * 0.5f;
            
            // Create a temporary target to move towards the release position.
            Transform tempTarget = new GameObject("TempReleasePoint").transform;
            tempTarget.position = currentTarget.position;
            tempTarget.rotation = currentTarget.rotation;
            
            // Point the attachment to the temporary target.
            ropeEnd.attachment.target = tempTarget;

            System.Action onComplete = () => {
                ropeEnd.attachment.enabled = false;
                ropeEnd.attachment.target = null;
                ropeEnd.state = RopeEndState.Free;
                Destroy(tempTarget.gameObject);
            };

            // Create a dummy transform for the final target position.
            Transform finalTargetDummy = new GameObject("FinalReleasePoint").transform;
            finalTargetDummy.position = releasePosition;
            
            activeTransition = StartCoroutine(TransitionToPosition(ropeEnd, tempTarget, finalTargetDummy, transitionDuration, () => {
                onComplete();
                Destroy(finalTargetDummy.gameObject);
            }));
        }
    }

    private IEnumerator TransitionToPosition(RopeEnd ropeEnd, Transform movingTarget, Transform finalDestination, float duration, System.Action onComplete = null) {
        // wait 1 frame
        yield return null;
        
        Vector3 startPosition = movingTarget.position;
        Quaternion startRotation = movingTarget.rotation;

        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            movingTarget.position = Vector3.Lerp(startPosition, finalDestination.position, elapsedTime / duration);
            movingTarget.rotation = Quaternion.Slerp(startRotation, finalDestination.rotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        movingTarget.position = finalDestination.position;
        movingTarget.rotation = finalDestination.rotation;

        onComplete?.Invoke();

        activeTransition = null;
    }

    private IEnumerator TransitionGrab(RopeEnd ropeEnd, Transform tempTarget, Transform finalTarget, float duration) {
        // wait 1 frame
        yield return null;
        
        Vector3 startPosition = tempTarget.position;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            tempTarget.position = Vector3.Lerp(startPosition, finalTarget.position, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        tempTarget.position = finalTarget.position;
        yield return null;

        // The transition is complete. Now, reparent the attachment to the final target.
        ropeEnd.attachment.target = finalTarget;
        ropeEnd.state = RopeEndState.Held;
        Destroy(tempTarget.gameObject);

        activeTransition = null;
    }
}