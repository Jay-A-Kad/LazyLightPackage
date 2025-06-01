using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
#if UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
#elif UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

public class LazyLightGenerator : EditorWindow
{
    private LightType lightType = LightType.Point;
    private Color lightColor = Color.white;
    private float intensity = 3.0f;
    private float range = 10f;
    private float angle = 60f;
    private int lightCount = 3;
    private string groupTag = "GeneratedLight";
    private bool useAreaLight = false;
    private bool enableGI = false;
    private bool previewLightPlacement = true;

    [MenuItem("Tools/Advanced Light Generator")]
    public static void ShowWindow()
    {
        GetWindow<LazyLightGenerator>("Advanced Light Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("\ud83d\udd27 Pro Lighting Tool", EditorStyles.boldLabel);

        lightType = (LightType)EditorGUILayout.EnumPopup("Light Type", lightType);
        lightColor = EditorGUILayout.ColorField("Light Color", lightColor);
        intensity = EditorGUILayout.FloatField("Intensity", intensity);
        range = EditorGUILayout.FloatField("Range", range);

        if (lightType == LightType.Spot)
            angle = EditorGUILayout.Slider("Spot Angle", angle, 1, 179);

        useAreaLight = EditorGUILayout.Toggle("Use Area Lights (HDRP Only)", useAreaLight);
        enableGI = EditorGUILayout.Toggle("Enable Global Illumination", enableGI);
        previewLightPlacement = EditorGUILayout.Toggle("Preview Light Placement", previewLightPlacement);

        lightCount = EditorGUILayout.IntSlider("Light Count", lightCount, 1, 10);
        groupTag = EditorGUILayout.TextField("Group Tag", groupTag);

        if (GUILayout.Button("Generate Lights"))
        {
            GenerateAdvancedLights();
        }

        if (GUILayout.Button("Delete All Tagged Lights"))
        {
            DeleteAllTaggedLights();
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

    static Bounds CalculateSceneBounds()
    {
        Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }

    List<Vector3> PoissonDiskSample(Vector3 center, float radius, int count)
    {
        List<Vector3> points = new List<Vector3>();
        int attempts = 0;
        while (points.Count < count && attempts < count * 20)
        {
            Vector3 candidate = center + Random.insideUnitSphere * radius;
            if (points.All(p => Vector3.Distance(p, candidate) > radius * 0.8f))
            {
                points.Add(candidate);
            }
            attempts++;
        }
        return points;
    }

    void GenerateAdvancedLights()
    {
        Bounds bounds = CalculateSceneBounds();
        if (bounds.size == Vector3.zero)
        {
            Debug.LogWarning("Scene has no renderers to calculate bounds.");
            return;
        }

        GameObject root = new GameObject("LightingRig_" + groupTag);
        Vector3 center = bounds.center;
        List<Vector3> positions = PoissonDiskSample(center, range, lightCount);

        foreach (Vector3 pos in positions)
        {
            Vector3 placementPos = GetCeilingPosition(pos);
            GameObject lightGO = new GameObject(groupTag + "_" + positions.IndexOf(pos));
            lightGO.tag = groupTag;
            lightGO.transform.parent = root.transform;
            Undo.RegisterCreatedObjectUndo(lightGO, "Create Light");

            Light light = lightGO.AddComponent<Light>();

#if UNITY_HDRP
            if (useAreaLight)
            {
                light.type = LightType.Rectangle;
                var hdLight = lightGO.AddComponent<HDAdditionalLightData>();
                hdLight.intensity = intensity * 100f;
                hdLight.areaLightShape = AreaLightShape.Rectangle;
                hdLight.enableSpotReflector = true;
            }
            else
            {
                light.type = lightType;
                var hdLight = lightGO.AddComponent<HDAdditionalLightData>();
                hdLight.intensity = intensity * 100f;
                hdLight.enableSpotReflector = true;
            }
#elif UNITY_URP
            light.type = lightType;
            var urp = lightGO.AddComponent<UniversalAdditionalLightData>();
            urp.usePipelineSettings = true;
#else
            light.type = lightType;
#endif

            light.color = lightColor;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            light.lightmapBakeType = enableGI ? LightmapBakeType.Mixed : LightmapBakeType.Realtime;

            if (light.type == LightType.Spot)
            {
                light.spotAngle = angle;
                light.innerSpotAngle = angle * 0.6f;
            }

            light.transform.position = placementPos;
            light.transform.LookAt(center);

            if (previewLightPlacement)
            {
                SceneView.lastActiveSceneView.LookAt(placementPos);
            }

            GameObject probe = new GameObject("ReflectionProbe_" + positions.IndexOf(pos));
            probe.transform.position = placementPos;
            var reflection = probe.AddComponent<ReflectionProbe>();
            reflection.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            reflection.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
            probe.transform.parent = root.transform;
        }

        Debug.Log($"Generated {lightCount} lights under root '{root.name}'.");
    }

    Vector3 GetCeilingPosition(Vector3 origin)
    {
        Ray ray = new Ray(origin + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            return hit.point + Vector3.up * 0.1f;
        }
        return origin;
    }

    void DeleteAllTaggedLights()
    {
        GameObject[] tagged = GameObject.FindGameObjectsWithTag(groupTag);
        foreach (var go in tagged)
        {
            Undo.DestroyObjectImmediate(go);
        }
        GameObject root = GameObject.Find("LightingRig_" + groupTag);
        if (root != null) Undo.DestroyObjectImmediate(root);
        Debug.Log($"Deleted all lights tagged '{groupTag}'.");
    }

    void ExportLightingToJson()
    {
        Light[] lights = Object.FindObjectsOfType<Light>();
        var export = lights.Where(l => l.gameObject.tag == groupTag).Select(l => new SerializedLight(l)).ToList();
        File.WriteAllText("lighting_setup.json", JsonUtility.ToJson(new SerializedLightList { lights = export }, true));
        Debug.Log("Exported tagged lighting setup.");
    }

    void ImportLightingFromJson()
    {
        if (!File.Exists("lighting_setup.json")) return;
        var list = JsonUtility.FromJson<SerializedLightList>(File.ReadAllText("lighting_setup.json"));
        DeleteAllTaggedLights();
        foreach (var s in list.lights)
        {
            GameObject go = new GameObject("ImportedLight");
            go.tag = groupTag;
            var light = go.AddComponent<Light>();
            light.type = s.type;
            light.color = s.color;
            light.intensity = s.intensity;
            light.range = s.range;
            light.transform.position = s.position;
            light.transform.rotation = s.rotation;
        }
        Debug.Log("Imported lighting setup from JSON.");
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

        public SerializedLight(Light l)
        {
            type = l.type;
            color = l.color;
            intensity = l.intensity;
            range = l.range;
            position = l.transform.position;
            rotation = l.transform.rotation;
        }
    }

    [System.Serializable]
    public class SerializedLightList
    {
        public List<SerializedLight> lights;
    }
}