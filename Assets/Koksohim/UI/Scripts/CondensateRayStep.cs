using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace KoksUI
{
    /// <summary>
    /// Обучение, шаг 9 «Конденсатоотводчик»: вместо таймера «Осматриваю…» нужно навести луч на точку
    /// ExamCondensateTarget (пустой объект в яме) и держать несколько секунд — как в экзамене.
    /// Создаётся сам при загрузке сцены; если окна CanvasStep9 или точки нет, шаг остаётся по таймеру, как раньше.
    /// В экзамене не работает (там осмотр считает ExamController).
    /// </summary>
    public class CondensateRayStep : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (ExamMode.Active) return;
            if (FindFirstObjectByType<CondensateRayStep>(FindObjectsInactive.Include) != null) return;
            new GameObject("CondensateRayStep").AddComponent<CondensateRayStep>();
        }

        StepWindow window;
        Transform target;
        float radius = 0.6f, seconds = 3f, held;
        XRRayInteractor[] rays;
        float raysTime = -100f;
        string waitText = "Наведите луч на конденсатоотводчик";

        void Start()
        {
            foreach (var w in FindObjectsByType<StepWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (w.name == "CanvasStep9") { window = w; break; }
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "ExamCondensateTarget") { target = t; break; }
            if (window == null || target == null)
            {
                Debug.Log("[Training] шаг 9 остаётся по таймеру (" + (window == null ? "нет окна CanvasStep9" : "нет точки ExamCondensateTarget") + ")");
                enabled = false;
                return;
            }
            var ec = FindFirstObjectByType<ExamController>(FindObjectsInactive.Include);
            if (ec != null) { radius = ec.condensateRadius; seconds = ec.condensateSeconds; }
            window.autoConfirmSeconds = 0f;          // таймера больше нет: окно закроется, когда луч продержан на точке
            Debug.Log("[Training] шаг 9: навести луч на точку ExamCondensateTarget на " + seconds + " с");
        }

        void RefreshRays()
        {
            if (rays == null || Time.unscaledTime - raysTime > 1f)
            {
                rays = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
                raysTime = Time.unscaledTime;
            }
        }

        static bool RayVisible(XRRayInteractor r)
        {
            if (r == null || !r.isActiveAndEnabled) return false;
            var lr = r.GetComponent<LineRenderer>();
            return lr == null || (lr.enabled && lr.gameObject.activeInHierarchy);
        }

        bool RayNearTarget()
        {
            RefreshRays();
            Vector3 p = target.position;
            foreach (var r in rays)
            {
                if (!RayVisible(r)) continue;
                Transform t = r.rayOriginTransform != null ? r.rayOriginTransform : r.transform;
                Vector3 to = p - t.position;
                float along = Vector3.Dot(to, t.forward);
                if (along < 0.2f || along > 12f) continue;
                if (Vector3.Cross(t.forward, to).magnitude <= radius + along * 0.02f) return true;
            }
            return false;
        }

        void Update()
        {
            if (window == null || !window.IsOpen) { held = 0f; return; }
            if (window.confirmButton != null) window.confirmButton.interactable = false;
            held = RayNearTarget() ? held + Time.deltaTime : Mathf.Max(0f, held - Time.deltaTime * 1.5f);
            if (window.buttonLabel != null)
                window.buttonLabel.text = held > 0.05f ? "Осматриваю… " + Mathf.RoundToInt(Mathf.Clamp01(held / seconds) * 100f) + "%" : waitText;
            if (held >= seconds)
            {
                Debug.Log("[Training] конденсатоотводчик осмотрен лучом");
                held = 0f;
                window.Confirm();
            }
        }
    }
}
