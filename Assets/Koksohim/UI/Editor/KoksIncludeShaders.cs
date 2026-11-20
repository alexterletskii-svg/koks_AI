using UnityEditor;
using UnityEngine;

namespace KoksUI.Editor
{
    /// <summary>Добавляет шейдер текста «поверх геометрии» в Always Included Shaders, чтобы он попал в сборку.</summary>
    [InitializeOnLoad]
    static class KoksIncludeShaders
    {
        static KoksIncludeShaders()
        {
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            var sh = Shader.Find("TextMeshPro/Distance Field Overlay");
            if (sh == null) return;
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var gs = new SerializedObject(assets[0]);
            var list = gs.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) return;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = sh;
            gs.ApplyModifiedProperties();
        }
    }
}
