using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace KoksUI
{
    /// <summary>
    /// Пока на экране окно шага или журнал, объекты сцены (точки осмотра и прочие интерактивные предметы) временно
    /// не реагируют на луч. Иначе триггер «выбирает» предмет за окном (луч изгибается к нему) и до кнопки окна не доходит.
    /// Создаётся сам при загрузке сцены, настраивать ничего не нужно.
    /// </summary>
    public class WindowRayGate : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<WindowRayGate>() == null) new GameObject("WindowRayGate").AddComponent<WindowRayGate>();
        }

        readonly List<XRBaseInteractable> off = new List<XRBaseInteractable>();
        bool blocked;

        void Update()
        {
            bool b = StepWindow.AnyOpen;
            if (b == blocked) return;
            blocked = b;
            if (b)
            {
                foreach (var i in FindObjectsByType<XRBaseInteractable>(FindObjectsSortMode.None))
                    if (i != null && i.enabled) { i.enabled = false; off.Add(i); }
            }
            else Restore();
        }

        void Restore()
        {
            foreach (var i in off) if (i != null) i.enabled = true;
            off.Clear();
        }

        void OnDestroy() { Restore(); }
    }
}
