using UnityEngine;

[ExecuteAlways]
public class SeesawGlowFeedback : MonoBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color glowColor = new(1f, 0.78f, 0.25f, 1f);
    [SerializeField, Min(0f)] private float emissionMultiplier = 2f;
    [SerializeField, Range(0f, 1f)] private float intensity;

    private MaterialPropertyBlock propertyBlock;
    private Color[] baseColors;

    private void Awake()
    {
        CacheRenderers();
        Apply();
    }

    private void OnValidate()
    {
        CacheRenderers();
        Apply();
    }

    public void SetIntensity(float normalizedIntensity)
    {
        intensity = Mathf.Clamp01(normalizedIntensity);
        Apply();
    }

    private void CacheRenderers()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        propertyBlock ??= new MaterialPropertyBlock();
        baseColors = new Color[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
            {
                baseColors[i] = Color.white;
                continue;
            }

            if (renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                baseColors[i] = renderer.sharedMaterial.GetColor("_BaseColor");
            }
            else if (renderer.sharedMaterial.HasProperty("_Color"))
            {
                baseColors[i] = renderer.sharedMaterial.GetColor("_Color");
            }
            else
            {
                baseColors[i] = Color.white;
            }
        }
    }

    private void Apply()
    {
        if (targetRenderers == null || baseColors == null)
        {
            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);

            Color baseColor = baseColors.Length > i ? baseColors[i] : Color.white;
            Color finalColor = Color.Lerp(baseColor, glowColor, intensity);

            if (HasProperty(renderer, "_BaseColor"))
            {
                propertyBlock.SetColor("_BaseColor", finalColor);
            }

            if (HasProperty(renderer, "_Color"))
            {
                propertyBlock.SetColor("_Color", finalColor);
            }

            if (HasProperty(renderer, "_EmissionColor"))
            {
                propertyBlock.SetColor("_EmissionColor", glowColor * (intensity * emissionMultiplier));
            }

            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private static bool HasProperty(Renderer renderer, string propertyName)
    {
        return renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(propertyName);
    }
}
