using Obi;
using UnityEngine;
using UnityEngine.InputSystem;

public class RopeInteractor : MonoBehaviour
{
    [Header("Setup")]
    public Transform playerGrabTransform; // A child object of the player where the rope is held.
    public GameObject grabIndicatorPrefab; // The half-transparent sphere prefab.
    public InputActionReference interactActionReference;

    [Header("Interaction Settings")]
    public float grabRadius = 1.5f;
    public float plugRadius = 2f;
    public string pluggableTag = "Pluggable";
    public float grabTransitionDuration = 0.2f;

    private GameObject grabIndicatorInstance;
    private RopeController currentRope;
    private RopeController.RopeEnd heldRopeEnd;

    private RopeController.RopeEnd potentialGrabTarget;
    private Transform potentialPlugTarget;

    void Start()
    {
        grabIndicatorInstance = Instantiate(grabIndicatorPrefab);
        grabIndicatorInstance.SetActive(false);
    }

    void OnEnable()
    {
        interactActionReference.action.Enable();
        interactActionReference.action.started += OnInteract;
    }

    void OnDisable()
    {
        interactActionReference.action.Disable();
        interactActionReference.action.started -= OnInteract;
    }

    void Update()
    {
        if (heldRopeEnd == null)
        {
            CheckForGrabbableRopeEnds();
        }
        else
        {
            CheckForPluggableTargets();
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (heldRopeEnd != null)
        {
            // Player is holding a rope end.
            if (potentialPlugTarget != null)
            {
                // Plug the rope.
                currentRope.Plug(heldRopeEnd, potentialPlugTarget, grabTransitionDuration);
                heldRopeEnd = null;
                currentRope = null;
                grabIndicatorInstance.SetActive(false);
            }
            else
            {
                // Drop the rope.
                currentRope.Drop(heldRopeEnd, playerGrabTransform, grabTransitionDuration);
                heldRopeEnd = null;
                currentRope = null;
            }
        }
        else if (potentialGrabTarget != null)
        {
            // Player is near a grabbable rope end.
            heldRopeEnd = potentialGrabTarget;
            currentRope = FindObjectOfType<RopeController>(); // Assuming one rope for now.
            currentRope.Grab(heldRopeEnd, playerGrabTransform, grabTransitionDuration);
            grabIndicatorInstance.SetActive(false);
        }
    }

    private void CheckForGrabbableRopeEnds()
    {
        potentialGrabTarget = null;
        RopeController rope = FindObjectOfType<RopeController>(); // In a multi-rope scene, this would need to be more sophisticated.
        if (rope == null) return;

        if (rope.GetComponent<ObiRope>() == null || !rope.GetComponent<ObiRope>().isLoaded)
        {
            grabIndicatorInstance.SetActive(false);
            return;
        }

        float distToStart = Vector3.Distance(transform.position, rope.GetRopeEndPosition(rope.startEnd));
        float distToEnd = Vector3.Distance(transform.position, rope.GetRopeEndPosition(rope.endEnd));

        if (rope.startEnd.state == RopeController.RopeEndState.Free && distToStart < grabRadius)
        {
            potentialGrabTarget = rope.startEnd;
            ShowIndicator(rope.GetRopeEndPosition(rope.startEnd));
        }
        else if (rope.endEnd.state == RopeController.RopeEndState.Free && distToEnd < grabRadius)
        {
            potentialGrabTarget = rope.endEnd;
            ShowIndicator(rope.GetRopeEndPosition(rope.endEnd));
        }
        else
        {
            grabIndicatorInstance.SetActive(false);
        }
    }

    private void CheckForPluggableTargets()
    {
        potentialPlugTarget = null;
        Collider[] colliders = Physics.OverlapSphere(transform.position, plugRadius);
        float closestDist = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.CompareTag(pluggableTag))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    potentialPlugTarget = col.transform;
                }
            }
        }

        if (potentialPlugTarget != null)
        {
            ShowIndicator(potentialPlugTarget.position);
        }
        else
        {
            grabIndicatorInstance.SetActive(false);
        }
    }

    private void ShowIndicator(Vector3 position)
    {
        grabIndicatorInstance.SetActive(true);
        grabIndicatorInstance.transform.position = position;
    }
}