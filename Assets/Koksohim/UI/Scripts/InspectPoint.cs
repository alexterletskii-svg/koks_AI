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
        [Tooltip("Высота маркера над объектом, м")]
        public float markerHeight = 0.18f;

        [Tooltip("Писать в Console наведение/осмотр (для отладки)")]
        public bool debugLog = true;

        [Header("Ссылки (заполняются билдером)")]
        public InspectHud hud;
        public Transform marker;
        public Image markerOuter;
        public GameObject markerCenter;
        public GameObject markerCheck;

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

        void OnEnable()
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
        }

        void OnDisable()
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
        }

        Renderer[] rends;

        void Start()
        {
            rends = GetComponentsInChildren<Renderer>();
            UpdateMarkerPosition();
            if (marker != null) markerBase = marker.localScale;
            SetMarkerState();
        }

        // объект может двигаться анимацией — маркер едет вместе с ним
        void UpdateMarkerPosition()
        {
            if (rends == null || rends.Length == 0) { MarkerPosition = transform.position + Vector3.up * markerHeight; }
            else
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                MarkerPosition = b.center + Vector3.up * (b.extents.y + markerHeight);
            }
            if (marker != null) marker.position = MarkerPosition;
        }

        void OnHoverEnter(HoverEnterEventArgs args)
        {
            hovered = true;
            if (debugLog) Debug.Log("[Inspect] наведение: " + name + " (" + group + ", " + title + ")");
            if (hud == null) return;
            if (Inspected) hud.ShowCard(this);
            else hud.ShowProgress(this, held / Mathf.Max(0.1f, fillSeconds));
        }

        void OnHoverExit(HoverExitEventArgs args)
        {
            hovered = false;
            if (debugLog) Debug.Log("[Inspect] луч ушёл: " + name);
            if (hud != null && Inspected) hud.Hide(this);
        }

        void Update()
        {
            if (hud == null || Inspected) return;
            float fill = Mathf.Max(0.1f, fillSeconds);
            if (hovered)
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
            Camera cam = Camera.main;
            UpdateMarkerPosition();
            if (cam != null) marker.rotation = cam.transform.rotation;
            float k = Inspected ? 0.8f : 1f + 0.12f * Mathf.Sin(Time.time * 4f);
            marker.localScale = markerBase * k;
        }

        void Complete()
        {
            Inspected = true;
            if (debugLog) Debug.Log("[Inspect] осмотрено: " + name + " (" + group + ", " + title + ")");
            SetMarkerState();
            hud.NotifyInspected(this);
            if (hovered) hud.ShowCard(this);
        }

        void SetMarkerState()
        {
            if (markerOuter != null) markerOuter.color = Inspected ? Ok : Heat;
            if (markerCenter != null) markerCenter.SetActive(!Inspected);
            if (markerCheck != null) markerCheck.SetActive(Inspected);
        }
    }
}
