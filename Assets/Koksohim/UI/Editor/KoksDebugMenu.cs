using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace KoksUI.Editor
{
    public static class KoksDebugMenu
    {
        [MenuItem("Koks/Debug/Подключить отладку луча (только в режиме Play)")]
        public static void AttachRayDebug()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Koks UI] Сначала нажмите Play, потом этот пункт.");
                return;
            }

            var rays = Object.FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var r in rays)
            {
                if (r.GetComponent<UIRayDebug>() == null)
                {
                    r.gameObject.AddComponent<UIRayDebug>();
                    count++;
                }
            }
            Debug.Log("[Koks UI] Отладка подключена к лучам: " + count + " (всего лучей в сцене: " + rays.Length + ")");
        }
    }
}
