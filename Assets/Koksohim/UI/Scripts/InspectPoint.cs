using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace KoksUI
{
    /// <summary>
    /// Точка осмотра на оборудовании. Над ней висит жёлтый маркер. Держите луч на точке fillSeconds секунд:
    /// идёт кольцо прогресса, затем точка считается осмотренной (маркер зелёный), а при наведении показывается карточка.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class InspectPoint : MonoBehaviour
    {
        [Header("Тексты (правятся здесь)")]
        public string group = "Клапан 1";
        public string title = "Крышка";
        [TextArea(3, 8)]
        public string text = "Здесь будет ваш текст: что это за узел, каким он должен быть и на что обратить внимание при осмотре.";

        [Header("Осмотр")]
        [Tooltip("Сколько секунд держать луч на точке")]
        public float fillSeconds = 2f;
        [Tooltip("Размер маркера, м")]
        public float markerSize = 0.1f;
        [Tooltip("Зазор между верхней точкой детали и маркером, м")]
        public float markerGap = 0.08f;
        [Tooltip("Ручной сдвиг маркера в мировых координатах, м (если нужно подвинуть)")]
        public Vector3 markerOffset = Vector3.zero;

        [Tooltip("Насколько маркер выдвинут к игроку, м. Больше — реже закрывается своей деталью, но и сквозь соседние предметы виден чуть чаще")]
        public float markerPull = 0.25f;

        [Tooltip("Маркер в центре детали (а не над ней) — для объектов под потолком и за ограждением")]
        public bool markerAtCenter = false;
        [Tooltip("Наведение на сам ромбик: луч, прошедший рядом с ромбиком (даже сквозь ограждение), засчитывается как наведение на деталь")]
        public bool aimAtMarker = true;

        [Tooltip("Писать в Console наведение/осмотр (для отладки)")]
        public bool debugLog = true;

        [Header("Ссылки (заполняются билдером)")]
        public InspectHud hud;
        public Transform marker;
        public Image markerOuter;
        public GameObject markerCenter;
        public GameObject markerCheck;

        /// <summary>Последние 1–3 точки: маркер крупнее и бледно виден сквозь предметы.</summary>
        public void SetSeeThrough(bool on)
        {
            // последние неосмотренные точки: маркер крупнее (сквозь предметы он больше не просвечивает)
            seeThrough = on;
        }
        bool seeThrough;

        public bool Inspected { get; private set; }
        public Vector3 MarkerPosition { get; private set; }

        XRSimpleInteractable interactable;
        bool hovered;
        float held;
        Vector3 markerBase = Vector3.one;

        static readonly Color Heat = new Color(1f, 0.768f, 0f, 1f);
        static readonly Color Ok = new Color(0.294f, 0.827f, 0.482f, 1f);

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
        }

        Renderer[] rends;

        void Start()
        {
            var own = GetComponent<Renderer>();
            rends = own != null ? new[] { own } : GetComponentsInChildren<Renderer>();
            UpdateMarkerPosition();
            if (marker != null)
            {
                markerBase = Vector3.one * (markerSize / 120f);
                marker.localScale = markerBase;
                MakeMarkerVisibleThroughGeometry();
            }
            SetMarkerState();
        }

        // объект может двигаться анимацией — маркер едет вместе с ним
        void UpdateMarkerPosition()
        {
            if (rends == null || rends.Length == 0)
                MarkerPosition = transform.position + Vector3.up * markerGap + markerOffset;
            else
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                // строго над деталью: по центру и над самой верхней точкой (или в центре детали)
                MarkerPosition = markerAtCenter ? b.center + markerOffset
                    : new Vector3(b.center.x, b.max.y, b.center.z) + Vector3.up * markerGap + markerOffset;
            }
            if (marker != null)
            {
                // маркер чуть ближе к игроку, чем деталь: свой корпус его не закрывает, а стены и трубы между ними — закрывают
                Vector3 pos = MarkerPosition;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 to = cam.transform.position - pos;
                    if (to.sqrMagnitude > 0.01f) pos += to.normalized * Mathf.Min(markerPull, to.magnitude * 0.5f);
                }
                marker.position = pos;
            }
        }

        // маркер рисуется с проверкой глубины: стены, трубы и корпуса других узлов его закрывают
        void MakeMarkerVisibleThroughGeometry()
        {
            foreach (var g in marker.GetComponentsInChildren<Graphic>(true))
            {
                g.material = UIDepth.MarkerMaterial;
                g.raycastTarget = false;    // ромбик не должен перехватывать луч у кнопок окна
            }
        }

        static UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor[] rays;
        static float raysTime = -100f;
        Transform lastRay;
        float nextBlockLog;

        // луч (любой руки) проходит рядом с ромбиком: допуск растёт с расстоянием, чтобы попадать было несложно
        bool AimedAt()
        {
            if (marker == null || !marker.gameObject.activeInHierarchy) return false;
            if (rays == null || Time.unscaledTime - raysTime > 1f)
            {
                rays = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsSortMode.None);
                raysTime = Time.unscaledTime;
            }
            Vector3 m = marker.position;
            float radius = Mathf.Max(0.15f, markerSize * 0.9f);
            foreach (var r in rays)
            {
                if (r == null || !r.isActiveAndEnabled) continue;
                Transform t = r.rayOriginTransform != null ? r.rayOriginTransform : r.transform;
                Vector3 to = m - t.position;
                float along = Vector3.Dot(to, t.forward);
                if (along < 0.2f || along > 15f) continue;
                float off = Vector3.Cross(t.forward, to).magnitude;
                if (off <= radius + along * 0.03f) { lastRay = t; return true; }
            }
            return false;
        }

        void EnterHover()
        {
            hovered = true;
            if (debugLog) Debug.Log("[Inspect] наведение: " + name + " (" + group + ", " + title + ")");
            if (hud == null || StepWindow.AnyOpen) return;
            if (Inspected) hud.ShowCard(this);
            else hud.ShowProgress(this, held / Mathf.Max(0.1f, fillSeconds));
        }

        void ExitHover()
        {
            hovered = false;
            if (debugLog) Debug.Log("[Inspect] луч ушёл: " + name);
            if (hud != null && Inspected) hud.Hide(this);
        }

        void Update()
        {
            // наведение: луч попал в деталь или прошёл рядом с ромбиком
            bool want = false;
            if (hud == null || (hud.Active && !(hud.AllDone && InspectHud.OtherRunning(hud))))
            {
                want = interactable != null && interactable.isHovered;
                if (!want && aimAtMarker && hud != null && !hud.AllDone)
                {
                    want = AimedAt();
                    // диагностика: луч у ромбика, а деталь он не касается — пишем, что стоит на пути
                    if (want && debugLog && lastRay != null && Time.unscaledTime > nextBlockLog)
                    {
                        nextBlockLog = Time.unscaledTime + 4f;
                        RaycastHit hit;
                        if (Physics.Raycast(lastRay.position, lastRay.forward, out hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                            Debug.Log("[Inspect] " + name + ": луч у ромбика, но первым упирается в «" + hit.collider.name + "»" +
                                      (hit.collider.transform.parent != null ? " (в " + hit.collider.transform.parent.name + ")" : "") +
                                      ", слой " + LayerMask.LayerToName(hit.collider.gameObject.layer) + ", " + hit.distance.ToString("0.0") + " м");
                        else Debug.Log("[Inspect] " + name + ": луч у ромбика, но коллайдеров на пути нет");
                    }
                }
            }
            if (want && !hovered) EnterHover();
            else if (!want && hovered) ExitHover();
            // пока открыто окно шага или журнал, осмотр «на паузе»; карточка осмотренной точки вернётся после закрытия окна
            bool blocked = StepWindow.AnyOpen;
            if (hovered && Inspected && !blocked && hud != null && hud.IsFree(this) && !hud.CardShownFor(this) && !(hud.AllDone && InspectHud.OtherRunning(hud)))
                hud.ShowCard(this);
            if (hud == null || Inspected || !hud.Active) return;
            float fill = Mathf.Max(0.1f, fillSeconds);
            if (blocked) return;
            if (hovered && hud.IsFree(this))
            {
                held += Time.deltaTime;
                hud.ShowProgress(this, held / fill);
                if (held >= fill) Complete();
            }
            else if (held > 0f)
            {
                // луч в руке дрожит: прогресс не обнуляется мгновенно, а плавно спадает
                held -= Time.deltaTime * 1.5f;
                if (held <= 0f) { held = 0f; hud.Hide(this); }
                else if (hud.IsFree(this)) hud.ShowProgress(this, held / fill);
            }
        }

        void LateUpdate()
        {
            if (marker == null) return;
            // маркеры видны только пока идёт осмотр и пока на экране нет окон шага
            bool show = hud == null || (hud.Active && !hud.AllDone && !StepWindow.AnyOpen);
            if (marker.gameObject.activeSelf != show) marker.gameObject.SetActive(show);
            Camera cam = Camera.main;
            UpdateMarkerPosition();
            if (cam != null) marker.rotation = cam.transform.rotation;
            float k = Inspected ? 0.8f : (seeThrough ? 1.5f : 1f) * (1f + 0.12f * Mathf.Sin(Time.time * 4f));
            marker.localScale = markerBase * k;
        }

        /// <summary>Точку нужно осмотреть ещё раз (например, закрыть кран): новое название, прогресс и маркер сброшены.</summary>
        public void ResetForRepeat(string newTitle, string newText)
        {
            Inspected = false;
            held = 0f;
            if (!string.IsNullOrEmpty(newTitle)) title = newTitle;
            if (!string.IsNullOrEmpty(newText)) text = newText;
            SetMarkerState();
            if (hud != null) hud.Reopen();
        }

        void Complete()
        {
            Inspected = true;
            if (debugLog) Debug.Log("[Inspect] осмотрено: " + name + " (" + group + ", " + title + ")");
            SetMarkerState();
            hud.NotifyInspected(this);
            if (hovered && !hud.AllDone) hud.ShowCard(this);
        }

        void SetMarkerState()
        {
            if (markerOuter != null) markerOuter.color = Inspected ? Ok : Heat;
            if (markerCenter != null) markerCenter.SetActive(!Inspected);
            if (markerCheck != null) markerCheck.SetActive(Inspected);
        }
    }
}
