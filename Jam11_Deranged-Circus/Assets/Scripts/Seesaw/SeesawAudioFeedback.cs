using Audio;
using UnityEngine;

public class SeesawAudioFeedback : MonoBehaviour
{
    [SerializeField] private Transform audioOrigin;
    [SerializeField] private string minimumDistanceEventName = string.Empty;
    [SerializeField] private string impactEventName = string.Empty;

    public void PlayMinimumDistance()
    {
        PlayNamedEvent(minimumDistanceEventName);
    }

    public void PlayImpact()
    {
        PlayNamedEvent(impactEventName);
    }

    private void PlayNamedEvent(string eventName)
    {
        if (AudioManager.Instance == null || string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        if (!AudioEvent.AudioEventNameToGuid.TryGetValue(eventName, out FMOD.GUID audioEvent))
        {
            return;
        }

        Vector3 position = audioOrigin != null ? audioOrigin.position : transform.position;
        AudioManager.Instance.Play3DAudio(audioEvent, position);
    }
}
