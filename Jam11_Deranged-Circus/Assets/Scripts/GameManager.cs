using Obi;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Rope Settings")]
    public RopeController ropeController;
    public float stiffBendingValue = 0f;
    public float looseBendingValue = 0.025f;

    [Header("Goat Management")]
    public GameObject goatPrefab;
    public Transform goatSpawnPoint;
    public float minYPosition = -10f;

    private ObiRope obiRope;
    private GameObject currentGoat;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (ropeController != null)
        {
            obiRope = ropeController.GetComponent<ObiRope>();
        }
    }

    void Start()
    {
        // Find the initial goat in the scene.
        GoatAudio initialGoat = FindObjectOfType<GoatAudio>();
        if (initialGoat != null)
        {
            currentGoat = initialGoat.gameObject;
        }
    }

    void Update()
    {
        ManageGoat();
    }

    void OnEnable()
    {
        if (ropeController != null)
        {
            ropeController.OnRopeEndPlugged += CheckRopeState;
            ropeController.OnRopeEndUnplugged += CheckRopeState;
        }
    }

    void OnDisable()
    {
        if (ropeController != null)
        {
            ropeController.OnRopeEndPlugged -= CheckRopeState;
            ropeController.OnRopeEndUnplugged -= CheckRopeState;
        }
    }

    private void CheckRopeState()
    {
        if (ropeController == null || obiRope == null) return;

        bool bothEndsPlugged = ropeController.startEnd.state == RopeController.RopeEndState.Plugged &&
                               ropeController.endEnd.state == RopeController.RopeEndState.Plugged;

        obiRope.maxBending = bothEndsPlugged ? stiffBendingValue : looseBendingValue;
    }

    private void ManageGoat()
    {
        if (currentGoat == null)
        {
            // If there's no goat, spawn a new one.
            if (goatPrefab != null)
            {
                Vector3 spawnPosition = goatSpawnPoint != null ? goatSpawnPoint.position : Vector3.zero;
                Quaternion spawnRotation = goatSpawnPoint != null ? goatSpawnPoint.rotation : Quaternion.identity;
                currentGoat = Instantiate(goatPrefab, spawnPosition, spawnRotation);
            }
        }
        else
        {
            // If the goat falls off the world, destroy it.
            if (currentGoat.transform.position.y < minYPosition)
            {
                Destroy(currentGoat);
                currentGoat = null; // Set to null so a new one spawns next frame.
            }
        }
    }
}