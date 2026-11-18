using UnityEngine;
using UnityEngine.Rendering;

public class DisableBakedLights : MonoBehaviour
{
    void Start()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        foreach (Light light in allLights)
        {
            if (light.lightmapBakeType == LightmapBakeType.Baked)
            {
                light.enabled = false;
                Debug.Log($"Отключена запечённая лампа: {light.name}");
            }
        }
    }
}
