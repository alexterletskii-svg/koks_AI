using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Показывает окно, когда осмотрены все точки одной группы
    /// (например, после газопроводов — «Запишите результат в эксплуатационный журнал»). Один раз.
    /// </summary>
    public class GroupCompleteWindow : MonoBehaviour
    {
        public InspectHud hud;
        [Tooltip("Название группы (поле Group у точек осмотра)")]
        public string group = "Газопроводы";
        public StepWindow window;

        bool subscribed;
        bool shown;

        void OnEnable() { Subscribe(); }
        void Start() { Subscribe(); }

        void OnDisable()
        {
            if (hud != null && subscribed) hud.PointInspected -= OnInspected;
            subscribed = false;
        }

        void Subscribe()
        {
            if (subscribed || hud == null) return;
            hud.PointInspected += OnInspected;
            subscribed = true;
        }

        void OnInspected(InspectPoint p)
        {
            if (shown || window == null || p.group != group) return;
            foreach (var x in hud.points)
                if (x != null && x.group == group && x != p && !x.Inspected) return;
            shown = true;
            window.Show();
        }
    }
}
