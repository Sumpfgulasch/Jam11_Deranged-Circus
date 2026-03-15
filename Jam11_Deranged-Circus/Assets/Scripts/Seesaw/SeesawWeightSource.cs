using UnityEngine;

public class SeesawWeightSource : MonoBehaviour
{
    [SerializeField, Min(0f)] private float weight = 1f;
    [SerializeField] private bool useRigidbodyMass;
    [SerializeField] private Rigidbody sourceRigidbody;
    [SerializeField, Min(0f)] private float rigidbodyMassMultiplier = 1f;
    [SerializeField, Min(0f)] private float seatedWeightMultiplier = 1f;

    public float Weight
    {
        get
        {
            if (!enabled)
            {
                return 0f;
            }

            float resolvedWeight = ResolveBaseWeight();
            if (IsSeated)
            {
                resolvedWeight *= seatedWeightMultiplier;
            }

            return resolvedWeight;
        }
    }

    public bool IsSeated { get; private set; }

    private void Reset()
    {
        sourceRigidbody = GetComponent<Rigidbody>();
    }

    public void SetWeight(float newWeight)
    {
        weight = Mathf.Max(0f, newWeight);
    }

    public void SetSeated(bool seated)
    {
        IsSeated = seated;
    }

    private float ResolveBaseWeight()
    {
        if (!useRigidbodyMass)
        {
            return weight;
        }

        if (sourceRigidbody == null)
        {
            sourceRigidbody = GetComponent<Rigidbody>();
        }

        return sourceRigidbody != null
            ? Mathf.Max(0f, sourceRigidbody.mass * rigidbodyMassMultiplier)
            : weight;
    }
}
