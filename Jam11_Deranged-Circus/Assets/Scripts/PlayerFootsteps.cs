using UnityEngine;
using FMOD.Studio;
using Audio;

[RequireComponent(typeof(Rigidbody))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Footsteps Settings")]
    public float movementThreshold = 0.1f; // Minimum speed to trigger footsteps

    private Rigidbody rb;
    private EventInstance footstepInstance;
    private bool isMoving = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (AudioManager.Instance == null) {
            print("No Audio Manager");
            return;
        }
        
        // Check if the player is moving on the horizontal plane
        float horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;

        if (horizontalVelocity > movementThreshold)
        {
            if (!isMoving)
            {
                // Player started moving
                footstepInstance = AudioManager.Instance.Play3DAudio(AudioEvent.PlayerFootsteps, transform);
                isMoving = true;
            }
        }
        else
        {
            if (isMoving)
            {
                // Player stopped moving
                AudioManager.Instance.StopAudio(footstepInstance);
                isMoving = false;
            }
        }
    }

    void OnDestroy()
    {
        AudioManager.Instance.StopAudio(footstepInstance);
    }
}