using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Прячет при старте всё, что должно появиться позже по ходу сценария (круги и окна следующих шагов).
    /// Так не страшно, если во время расстановки в редакторе какой-то круг остался включённым.
    /// </summary>
    public class StepFlow : MonoBehaviour
    {
        public GameObject[] hideAtStart;

        void Awake()
        {
            if (hideAtStart == null) return;
            foreach (var g in hideAtStart) if (g != null) g.SetActive(false);
        }
    }
}
