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

        if (GUILayout.Button("Generate Light If Scene Is Dark"))
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

        for (int i = 0; i < lightCount; i++)
        {
            GameObject lightGO = new GameObject($"SmartLight_{i}");
            Light light = lightGO.AddComponent<Light>();

            // Use Spot or Point light
            light.type = (Random.value > 0.5f) ? LightType.Spot : LightType.Point;
            light.intensity = Random.Range(1.5f, 3.5f);
            light.range = Mathf.Max(bounds.size.x, bounds.size.z) * 0.8f;
            light.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.9f, 1f);

            Vector3 center = bounds.center;
            float ceilingY = bounds.max.y + 1.5f;

            Vector3 randomOffset = new Vector3(
                Random.Range(-bounds.extents.x * 0.5f, bounds.extents.x * 0.5f),
                0f,
                Random.Range(-bounds.extents.z * 0.5f, bounds.extents.z * 0.5f)
            );

            light.transform.position = center + randomOffset + Vector3.up * bounds.extents.y;

            if (light.type == LightType.Spot)
            {
                light.spotAngle = Random.Range(40f, 80f);
                light.transform.LookAt(center);
            }
        }

        Debug.Log($" placed {lightCount} lights within scene bounds.");
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

