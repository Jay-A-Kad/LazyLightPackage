using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
#if UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

public class LazyLightGenerator : EditorWindow
{
    private LightingPreset[] presets;
    private int selectedPresetIndex = 0;

    [MenuItem("Tools/Lazy Light Generator")]
    public static void ShowWindow()
    {
        GetWindow<LazyLightGenerator>("Lazy Light Generator");
    }

    void OnEnable()
    {
        CreateDefaultPresets();
        LoadPresets();
    }

    void OnGUI()
    {
        GUILayout.Label("Scene Lighting Generator", EditorStyles.boldLabel);

        if (presets != null && presets.Length > 0)
        {
            selectedPresetIndex = EditorGUILayout.Popup("Lighting Preset", selectedPresetIndex, presets.Select(p => p.name).ToArray());
        }

        if (GUILayout.Button("Generate Light"))
        {
            GenerateLightWithPreset(presets[selectedPresetIndex]);
        }

        if (GUILayout.Button("Delete All Lights"))
        {
            DeleteAllLights();
        }

        if (GUILayout.Button("Export Lighting Setup to JSON"))
        {
            ExportLightingToJson();
        }

        if (GUILayout.Button("Import Lighting Setup from JSON"))
        {
            ImportLightingFromJson();
        }
    }

    void CreateDefaultPresets()
    {
        string[] defaultNames = { "StudioSetup", "IndoorWarm", "Dramatic", "Showcase" };
        foreach (string presetName in defaultNames)
        {
            string path = $"Assets/LightingPresets/{presetName}.asset";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory("Assets/LightingPresets");
                LightingPreset preset = ScriptableObject.CreateInstance<LightingPreset>();
                preset.name = presetName;
                switch (presetName)
                {
                    case "StudioSetup":
                        preset.type = LightType.Spot; preset.color = Color.white; preset.intensity = 5f; preset.range = 15f; preset.angle = 60f; break;
                    case "IndoorWarm":
                        preset.type = LightType.Point; preset.color = new Color(1.0f, 0.95f, 0.8f); preset.intensity = 3f; preset.range = 10f; break;
                    case "Dramatic":
                        preset.type = LightType.Directional; preset.color = Color.white; preset.intensity = 6f; preset.range = 20f; break;
                    case "Showcase":
                        preset.type = LightType.Spot; preset.color = Color.white; preset.intensity = 4f; preset.range = 12f; preset.angle = 45f; break;
                }
                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
            }
        }
    }

    void LoadPresets()
    {
        string[] guids = AssetDatabase.FindAssets("t:LightingPreset");
        presets = guids.Select(guid => AssetDatabase.LoadAssetAtPath<LightingPreset>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
    }

    static void DeleteAllLights()
    {
#if UNITY_2023_1_OR_NEWER
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
        var lights = Object.FindObjectsOfType<Light>();
#endif
        foreach (var light in lights)
        {
            Undo.DestroyObjectImmediate(light.gameObject);
        }
    }

    static void GenerateLightWithPreset(LightingPreset preset)
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

        var heroObjects = GameObject.FindGameObjectsWithTag("HeroObject");
        Bounds bounds = CalculateSceneBounds(heroObjects.Length > 0 ? heroObjects : null);

        if (bounds.size == Vector3.zero)
        {
            Debug.LogWarning("Scene has no renderers or key objects to light.");
            return;
        }

        int lightCount = Mathf.Clamp((int)(bounds.size.magnitude / 10f), 1, 3);
        Vector3 center = bounds.center;

        for (int i = 0; i < lightCount; i++)
        {
            GameObject lightGO = new GameObject($"SmartLight_{i}");
            Undo.RegisterCreatedObjectUndo(lightGO, "Create Light");
            Light light = lightGO.AddComponent<Light>();

            light.type = preset.type;
            light.color = preset.color;
            light.intensity = preset.intensity;
            light.range = preset.range;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;

#if UNITY_HDRP
            var hdLight = lightGO.AddComponent<HDAdditionalLightData>();
            hdLight.intensity = preset.intensity * 100f;
            hdLight.enableSpotReflector = true;
#endif

            Vector3 offset = new Vector3(
                Random.Range(-bounds.extents.x * 0.4f, bounds.extents.x * 0.4f),
                0f,
                Random.Range(-bounds.extents.z * 0.4f, bounds.extents.z * 0.4f)
            );

            light.transform.position = center + offset + Vector3.up * (bounds.extents.y + 1.0f);

            if (light.type == LightType.Spot)
            {
                light.spotAngle = preset.angle;
                light.innerSpotAngle = preset.angle * 0.6f;
                light.transform.LookAt(center);
            }
        }

        Debug.Log($"\u2728 Placed {lightCount} lights using preset: {preset.name}");
    }

    static Bounds CalculateSceneBounds(GameObject[] targetObjects = null)
    {
        List<Renderer> renderers = new List<Renderer>();

        if (targetObjects != null)
        {
            foreach (var obj in targetObjects)
            {
                renderers.AddRange(obj.GetComponentsInChildren<Renderer>());
            }
        }
        else
        {
#if UNITY_2023_1_OR_NEWER
            renderers.AddRange(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None));
#else
            renderers.AddRange(Object.FindObjectsOfType<Renderer>());
#endif
        }

        if (renderers.Count == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        return bounds;
    }

    void ExportLightingToJson()
    {
        Light[] lights = FindObjectsOfType<Light>();
        List<SerializedLight> serializedLights = lights.Select(l => new SerializedLight(l)).ToList();
        string json = JsonUtility.ToJson(new SerializedLightList { lights = serializedLights }, true);
        File.WriteAllText("lighting_setup.json", json);
        Debug.Log("Lighting setup exported to lighting_setup.json");
    }

    void ImportLightingFromJson()
    {
        if (!File.Exists("lighting_setup.json")) return;
        string json = File.ReadAllText("lighting_setup.json");
        var list = JsonUtility.FromJson<SerializedLightList>(json);
        DeleteAllLights();
        foreach (var sLight in list.lights)
        {
            GameObject lightGO = new GameObject("ImportedLight");
            Light light = lightGO.AddComponent<Light>();
            light.type = sLight.type;
            light.color = sLight.color;
            light.intensity = sLight.intensity;
            light.range = sLight.range;
            light.transform.position = sLight.position;
            light.transform.rotation = sLight.rotation;
        }
        Debug.Log("Lighting setup imported from lighting_setup.json");
    }

    [System.Serializable]
    public class SerializedLight
    {
        public LightType type;
        public Color color;
        public float intensity;
        public float range;
        public Vector3 position;
        public Quaternion rotation;

        public SerializedLight(Light light)
        {
            type = light.type;
            color = light.color;
            intensity = light.intensity;
            range = light.range;
            position = light.transform.position;
            rotation = light.transform.rotation;
        }
    }

    [System.Serializable]
    public class SerializedLightList
    {
        public List<SerializedLight> lights;
    }
}

[CreateAssetMenu(fileName = "LightingPreset", menuName = "Lighting/LightingPreset")]
public class LightingPreset : ScriptableObject
{
    public LightType type;
    public Color color = Color.white;
    public float intensity = 3.0f;
    public float range = 10f;
    public float angle = 60f;
}
