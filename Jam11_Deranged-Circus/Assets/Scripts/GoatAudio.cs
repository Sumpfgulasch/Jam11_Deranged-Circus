using Audio;
using FMOD.Studio;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GoatAudio : MonoBehaviour
{
    private Rigidbody rb;
    private EventInstance audioInstance;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Start playing the goat sound and keep the instance to control it later.
        audioInstance = AudioManager.Instance.Play3DAudio(AudioEvent.Goat, transform);
    }

    void Update()
    {
        // Get the current speed of the rigidbody.
        float speed = rb.linearVelocity.magnitude;

        // Send the speed value to the FMOD event's "Speed" parameter.
        AudioManager.Instance.SetLocalParameter(audioInstance, "Speed", speed);
    }

    void OnDestroy()
    {
        // Stop the audio when the object is destroyed to prevent sound leaks.
        if (audioInstance.isValid())
        {
            audioInstance.stop(STOP_MODE.ALLOWFADEOUT);
            audioInstance.release();
        }
    }
}