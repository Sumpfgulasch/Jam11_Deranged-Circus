using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class SeesawBoardRiderZone : MonoBehaviour
{
    [SerializeField] private SeesawController controller;
    [SerializeField] private Transform beamTransform;
    [SerializeField, Min(0.01f)] private float standingCheckDistance = 0.35f;

    private readonly Dictionary<SeesawWeightSource, int> overlapCounts = new();
    private readonly HashSet<SeesawWeightSource> registeredSources = new();

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        beamTransform = transform.parent;
    }

    private void Awake()
    {
        beamTransform ??= transform.parent;
    }

    private void OnTriggerEnter(Collider other)
    {
        SeesawWeightSource weightSource = other.GetComponentInParent<SeesawWeightSource>();
        if (controller == null || weightSource == null)
        {
            return;
        }

        overlapCounts.TryGetValue(weightSource, out int count);
        overlapCounts[weightSource] = count + 1;
        RefreshRegistration(weightSource);
    }

    private void OnTriggerStay(Collider other)
    {
        SeesawWeightSource weightSource = other.GetComponentInParent<SeesawWeightSource>();
        if (controller == null || weightSource == null)
        {
            return;
        }

        RefreshRegistration(weightSource);
    }

    private void OnTriggerExit(Collider other)
    {
        SeesawWeightSource weightSource = other.GetComponentInParent<SeesawWeightSource>();
        if (controller == null || weightSource == null)
        {
            return;
        }

        if (!overlapCounts.TryGetValue(weightSource, out int count))
        {
            return;
        }

        count--;
        if (count <= 0)
        {
            overlapCounts.Remove(weightSource);
            Unregister(weightSource);
            return;
        }

        overlapCounts[weightSource] = count;
        RefreshRegistration(weightSource);
    }

    private void OnDisable()
    {
        foreach (SeesawWeightSource source in registeredSources)
        {
            if (source != null)
            {
                controller?.UnregisterBoardWeight(source);
            }
        }

        registeredSources.Clear();
        overlapCounts.Clear();
    }

    private void RefreshRegistration(SeesawWeightSource weightSource)
    {
        if (weightSource == null)
        {
            return;
        }

        if (ShouldCountWeight(weightSource))
        {
            registeredSources.Add(weightSource);
            controller.RegisterBoardWeight(weightSource);
        }
        else
        {
            Unregister(weightSource);
        }
    }

    private void Unregister(SeesawWeightSource weightSource)
    {
        if (weightSource == null)
        {
            return;
        }

        if (registeredSources.Remove(weightSource))
        {
            controller.UnregisterBoardWeight(weightSource);
        }
    }

    private bool ShouldCountWeight(SeesawWeightSource weightSource)
    {
        if (weightSource.IsSeated || beamTransform == null || !TryGetCombinedBounds(weightSource, out Bounds bounds))
        {
            return false;
        }

        Collider beamCollider = beamTransform.GetComponent<Collider>();
        if (beamCollider == null)
        {
            return false;
        }

        Vector3 rayOrigin = new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z);
        Ray ray = new Ray(rayOrigin, Vector3.down);
        return beamCollider.Raycast(ray, out RaycastHit hit, standingCheckDistance) && hit.normal.y > 0.35f;
    }

    private static bool TryGetCombinedBounds(SeesawWeightSource weightSource, out Bounds bounds)
    {
        Collider[] colliders = weightSource.GetComponentsInChildren<Collider>(false);
        bool foundAny = false;
        bounds = default;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger || !collider.enabled)
            {
                continue;
            }

            if (!foundAny)
            {
                bounds = collider.bounds;
                foundAny = true;
                continue;
            }

            bounds.Encapsulate(collider.bounds);
        }

        return foundAny;
    }
}
