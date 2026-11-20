using UnityEngine;
using UnityEngine.Events;

namespace KoksUI
{
    /// <summary>
    /// Синий круг шага осмотра. Когда игрок (камера) встаёт в круг, показывается окно шага.
    /// После «Приступить к осмотру» круг гаснет. Физические коллайдеры не нужны:
    /// расстояние считается от головы игрока до центра круга.
    /// </summary>
    public class StepMarker : MonoBehaviour
    {
        public StepWindow window;

        [Tooltip("Радиус срабатывания по горизонтали, м")]
        public float radius = 1.5f;
        [Tooltip("Допуск по высоте: голова не ниже круга на столько и не выше, чем на maxHeight")]
        public float minHeight = -0.5f;
        public float maxHeight = 3.0f;

        [Tooltip("Что включить после «Приступить к осмотру» (например, следующий круг)")]
        public GameObject[] activateOnConfirm;

        [Header("События")]
        public UnityEvent onEntered;
        [Tooltip("Игрок нажал «Приступить к осмотру» — сюда можно подключить дальнейшие действия")]
        public UnityEvent onConfirmed;

        bool shown;
        bool subscribed;

        float nextLog;

        void Awake()
        {
            // старые скрипты уничтожают круг при входе игрока — для шага осмотра это не нужно
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                string n = mb.GetType().Name;
                if (n == "DestroyOnTriggerByTag" || n == "DestroyOnCollisionByTag") Destroy(mb); // выключения мало: OnTrigger вызывается и у выключенных
            }
        }

        void Start()
        {
            Subscribe();
            Debug.Log("[StepMarker] старт на '" + name + "', окно: " + (window != null ? window.name : "НЕ НАЗНАЧЕНО") + ", радиус " + radius);
        }

        void Subscribe()
        {
            if (subscribed || window == null) return;
            window.onConfirmed.AddListener(OnConfirmed);
            subscribed = true;
        }

        void Update()
        {
            if (shown || window == null) return;
            Subscribe();

            Camera cam = Camera.main;
            if (cam == null)
            {
                if (Time.time > nextLog) { nextLog = Time.time + 5f; Debug.LogWarning("[StepMarker] нет камеры с тегом MainCamera"); }
                return;
            }

            Vector3 d = cam.transform.position - transform.position;
            float dy = d.y;
            d.y = 0f;
            if (Time.time > nextLog)
            {
                nextLog = Time.time + 3f;
                Debug.Log("[StepMarker] до круга по горизонтали " + d.magnitude.ToString("F2") + " м, по высоте " + dy.ToString("F2") + " м");
            }
            if (d.magnitude > radius) return;
            if (dy < minHeight || dy > maxHeight) return;

            shown = true;
            onEntered.Invoke();
            window.Show();
        }

        void OnConfirmed()
        {
            onConfirmed.Invoke();
            if (activateOnConfirm != null)
                foreach (var g in activateOnConfirm) if (g != null) g.SetActive(true);
            gameObject.SetActive(false); // круг гаснет
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
