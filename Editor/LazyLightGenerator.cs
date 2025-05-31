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
        Light[] existingLights;

#if UNITY_2023_1_OR_NEWER
        existingLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
        existingLights = Object.FindObjectsOfType<Light>();
#endif

        if (existingLights.Length > 0)
        {
            Debug.Log("Scene already has lighting.");
            return;
        }

        LightType randomType = (LightType)Random.Range(0, 3);
        GameObject lightGO = new GameObject("LazyGeneratedLight");
        Light light = lightGO.AddComponent<Light>();
        light.type = randomType;
        light.intensity = Random.Range(1f, 3f);
        light.color = Random.ColorHSV();

        if (randomType != LightType.Directional)
        {
            light.transform.position = Random.insideUnitSphere * 5f;
        }
        else
        {
            light.transform.rotation = Quaternion.Euler(Random.Range(20, 60), Random.Range(0, 360), 0);
        }

        Debug.Log($"Generated {randomType} light with intensity {light.intensity} and color {light.color}");
    }
}
