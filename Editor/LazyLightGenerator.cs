using UnityEngine;
using UnityEditor;
using System.Linq;

public class LazyLightGenerator : EditorWindow
{
    [MenuItem("Tools/Lazy Light Generator")]
    public static void ShowWindow()
    {
        GetWindow<LazyLightGenerator>("Lazy Light Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Scene Lighting Generator", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Light"))
        {
            GenerateLightIfNoneExists();
        }
    }

    static void GenerateLightIfNoneExists()
    {
#if UNITY_2023_1_OR_NEWER
        Light[] existingLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
        Light[] existingLights = Object.FindObjectsOfType<Light>();
#endif

        if (existingLights.Any(l => l != null && l.enabled && l.intensity > 0.5f))
        {
            Debug.Log("Scene already has sufficient lighting.");
            return;
        }

        Bounds bounds = CalculateSceneBounds();
        if (bounds.size == Vector3.zero)
        {
            Debug.LogWarning("Scene has no renderers to light.");
            return;
        }

        int lightCount = Mathf.Clamp((int)(bounds.size.magnitude / 10f), 1, 3);
        Vector3 center = bounds.center;
        float ceilingY = bounds.max.y + 1.0f;

        for (int i = 0; i < lightCount; i++)
        {
            GameObject lightGO = new GameObject($"SmartLight_{i}");
            Light light = lightGO.AddComponent<Light>();

            // Choose light type
            light.type = (Random.value > 0.5f) ? LightType.Spot : LightType.Point;

            // Color: either warm yellow or cool white
            light.color = (Random.value > 0.5f)
                ? new Color(1.0f, 0.95f, 0.8f) // warm indoor
                : Color.white;

            // Realistic intensity and falloff
            light.intensity = Random.Range(3.0f, 6.0f); // stronger for real lighting
            light.range = Mathf.Max(bounds.size.x, bounds.size.z) * 1.2f;

            // Shadows
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;

            // Position inside ceiling area
            Vector3 offset = new Vector3(
                Random.Range(-bounds.extents.x * 0.4f, bounds.extents.x * 0.4f),
                0f,
                Random.Range(-bounds.extents.z * 0.4f, bounds.extents.z * 0.4f)
            );

            light.transform.position = center + offset + Vector3.up * (bounds.extents.y + 1.0f);

            if (light.type == LightType.Spot)
            {
                light.spotAngle = Random.Range(45f, 70f);
                light.innerSpotAngle = light.spotAngle * 0.6f;
                light.transform.LookAt(center);
            }
        }

        Debug.Log($" placed {lightCount} lights in the scene");
    }

    static Bounds CalculateSceneBounds()
    {
#if UNITY_2023_1_OR_NEWER
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
#else
        var renderers = Object.FindObjectsOfType<Renderer>();
#endif

        if (renderers.Length == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
                bounds.Encapsulate(r.bounds);
        }

        return bounds;
    }
}

