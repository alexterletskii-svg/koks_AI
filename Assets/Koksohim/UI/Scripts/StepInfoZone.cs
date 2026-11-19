using UnityEngine;
using UnityEngine.Events;

namespace KoksUI
{
    /// <summary>
    /// Вешается на синий круг (PointPosition). Когда игрок встаёт в круг, показывает окно шага осмотра.
    /// Всё содержимое окна правится в Inspector этого компонента.
    /// </summary>
    public class StepInfoZone : MonoBehaviour
    {
        [Header("Содержимое окна (правится здесь)")]
        public int stepNumber = 1;
        [Tooltip("Всего шагов в этом осмотре. 0 — показывать просто «Шаг N».")]
        public int totalSteps = 0;
        public string title = "Газовоздушные клапаны";
        [TextArea(2, 6)]
        public string description = "Здесь находится [название оборудования и для чего оно]. Его нужно осмотреть при приёмке смены.";
        public string checkTitle = "Что проверить";
        [Tooltip("До трёх пунктов проверки. Пустой пункт скрывается.")]
        public string[] bullets = { "Пункт проверки 1", "Пункт проверки 2", "Пункт проверки 3" };
        public Sprite icon;

        [Header("Срабатывание")]
        [Tooltip("Радиус круга по горизонтали, м (от центра круга до головы игрока)")]
        public float radius = 1.2f;
        public StepWindow window;

        [Header("После нажатия «Приступить к осмотру»")]
        [Tooltip("Погасить (отключить) этот круг")]
        public bool hideCircleOnConfirm = true;
        [Tooltip("Сюда можно подвесить действия: включить следующий круг, запустить осмотр и т.п.")]
        public UnityEvent onConfirmed;

        bool inside;
        Camera cam;

        void Start()
        {
            if (window == null) window = FindFirstObjectByType<StepWindow>(FindObjectsInactive.Include);
        }

        void Update()
        {
            if (window == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 d = cam.transform.position - transform.position;
            bool near = Mathf.Abs(d.y) < 4f && new Vector2(d.x, d.z).magnitude <= radius;

            if (near && !inside)
            {
                inside = true;
                window.Show(this);
            }
            else if (!near && inside)
            {
                inside = false;
                window.Hide(this);
            }
        }

        void OnDisable()
        {
            if (window != null) window.Hide(this);
            inside = false;
        }

        /// <summary>Вызывает окно, когда игрок нажал «Приступить к осмотру».</summary>
        public void Confirm()
        {
            onConfirmed?.Invoke();
            if (hideCircleOnConfirm) gameObject.SetActive(false);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.77f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
