using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>
    /// Чинит потерянные связи шагов в FirstScene: у наборов осмотра (CanvasInspect…) пустое «окно после осмотра»,
    /// «окно перед осмотром» или «что включить после осмотра». Заполняет только пустое, ничего не пересоздаёт.
    /// </summary>
    internal static class KoksLinkRepair
    {
        // набор осмотра → окно перед осмотром, окно после осмотра, круг следующего шага
        static readonly string[][] Map =
        {
            new[] { "CanvasInspect",  "CanvasStep1", "CanvasStep1Done", "PointPosition_Step2" },
            new[] { "CanvasInspect2", "CanvasStep4", "CanvasStep4Done", "PointPosition_Step5" },
            new[] { "CanvasInspect3", "CanvasStep6", "CanvasStep6Done", "PointPosition_Step8" },
        };

        static T Find<T>(string name) where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.name == name && c.gameObject.scene.IsValid()) return c;
            return null;
        }

        static GameObject FindGo(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
            return null;
        }

        [MenuItem("Koks/Fix/Починить связи шагов осмотра (FirstScene)")]
        static void Repair()
        {
            int fixedCount = 0;
            foreach (var row in Map)
            {
                var hud = Find<InspectHud>(row[0]);
                if (hud == null) { Debug.LogWarning("[Koks Fix] нет набора осмотра " + row[0]); continue; }
                if (hud.startWindow == null)
                {
                    var w = Find<StepWindow>(row[1]);
                    if (w != null) { hud.startWindow = w; fixedCount++; Debug.Log("[Koks Fix] " + row[0] + ": окно перед осмотром = " + w.name); }
                }
                if (hud.completionWindow == null)
                {
                    var w = Find<StepWindow>(row[2]);
                    if (w != null) { hud.completionWindow = w; fixedCount++; Debug.Log("[Koks Fix] " + row[0] + ": окно после осмотра = " + w.name); }
                    else Debug.LogWarning("[Koks Fix] " + row[0] + ": не найдено окно " + row[2]);
                }
                if (row[3] != null)
                {
                    bool empty = hud.activateOnComplete == null || hud.activateOnComplete.Length == 0 || hud.activateOnComplete.All(g => g == null);
                    if (empty)
                    {
                        var c = FindGo(row[3]);
                        if (c != null) { hud.activateOnComplete = new[] { c }; fixedCount++; Debug.Log("[Koks Fix] " + row[0] + ": после осмотра включится " + c.name); }
                        else Debug.LogWarning("[Koks Fix] " + row[0] + ": не найден объект " + row[3]);
                    }
                }
                EditorUtility.SetDirty(hud);
            }
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Koks Fix] готово, исправлено связей: " + fixedCount + ". Сцена сохранена.");
        }
    }
}
