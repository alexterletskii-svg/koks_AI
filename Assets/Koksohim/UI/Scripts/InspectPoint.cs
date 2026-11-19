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

        void Start()
        {
            var rs = GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in rs)
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            MarkerPosition = b.center + Vector3.up * (b.extents.y + markerHeight);
            if (marker != null)
            {
                marker.position = MarkerPosition;
                markerBase = marker.localScale;
            }
            SetMarkerState();
        }

        void OnHoverEnter(HoverEnterEventArgs args)
        {
            hovered = true;
            held = 0f;
            if (hud == null) return;
            if (Inspected) hud.ShowCard(this);
            else hud.ShowProgress(this, 0f);
        }

        void OnHoverExit(HoverExitEventArgs args)
        {
            hovered = false;
            held = 0f;
            if (hud != null) hud.Hide(this);
        }

        void Update()
        {
            if (!hovered || Inspected || hud == null) return;
            held += Time.deltaTime;
            hud.ShowProgress(this, held / Mathf.Max(0.1f, fillSeconds));
            if (held >= fillSeconds) Complete();
        }

        void LateUpdate()
        {
            if (marker == null) return;
            Camera cam = Camera.main;
            if (cam != null) marker.rotation = cam.transform.rotation;
            float k = Inspected ? 0.8f : 1f + 0.12f * Mathf.Sin(Time.time * 4f);
            marker.localScale = markerBase * k;
        }

        void Complete()
        {
            Inspected = true;
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
