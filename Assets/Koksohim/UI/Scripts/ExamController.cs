using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace KoksUI
{
    /// <summary>
    /// Режим «Экзамен» в FirstScene. В обучении (ExamMode.Active == false) выключается и ничего не делает.
    /// В экзамене: прячет подсказки обучения (синие круги, окна шагов, ромбики и карточки), игрок осматривает
    /// детали сам — держит луч на детали 2 секунды. Считает 9 заданий и время (с входа в газовый тоннель),
    /// завершает экзамен, когда игрок наводит луч на дверь door.004, и показывает итоговое окно.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ExamController : MonoBehaviour
    {
        [Tooltip("Для проверки в редакторе: запустить FirstScene сразу в режиме экзамена, минуя меню")]
        public bool forceExam;

        [Header("Сколько секунд держать")]
        public float inspectSeconds = 2f;
        public float doorSeconds = 1.5f;
        public float condensateSeconds = 3f;

        [Header("Объекты (находятся сами, можно назначить вручную)")]
        public Transform door;
        public Transform condensate;
        public DrainValve drain;
        public JournalWindow journal;
        public ArrivalTrigger tunnelTrigger;

        [Header("Окна и значки (собирает Koks → Build UI → 20)")]
        public ExamResultWindow result;
        public ExamJournalIcon journalIcon;
        public GameObject ringRoot;
        public Image ringFill;

        [Header("Вводные тексты экзамена")]
        public string introStatus = "Экзамен начался";
        public string introTitle = "Экзамен: осмотр оборудования тоннелей коксовой батареи";
        [TextArea(2, 5)]
        public string introDescription = "Осмотр проводится самостоятельно, без подсказок. Время засчитывается с момента входа в газовый тоннель.";
        public string[] introBullets =
        {
            "Осмотр засчитывается, если держать луч на детали 2 секунды",
            "Результаты осмотра запишите в журнал",
            "Закончите осмотр, наведя луч на дверь выхода"
        };
        public string tunnelStep = "Экзамен";
        public string tunnelTitle = "Вы в газовом тоннеле";
        [TextArea(2, 5)]
        public string tunnelDescription = "Осмотрите оборудование самостоятельно: подсказок нет. Когда закончите, наведите луч на дверь выхода.";
        public string tunnelChecksHeader = "Как действовать";
        public string[] tunnelChecks =
        {
            "Держите луч на детали 2 секунды",
            "Результаты запишите в журнал",
            "Завершите осмотр у двери"
        };
        public string tunnelButton = "Начать осмотр";

        public static readonly string[] TaskNames =
        {
            "Выбор средств индивидуальной защиты",
            "Осмотр газовоздушных клапанов",
            "Осмотр кантовочных кранов",
            "Крышки устройства обезграфичивания",
            "Осмотр газопроводов",
            "Запись в эксплуатационный журнал",
            "Штанги и канаты кантовочного механизма",
            "Блочки и опорные ролики",
            "Сточная линия: открыть и закрыть кран",
            "Осмотр конденсатоотводчика"
        };
        const int Ppe = 0, Journal = 5, Drain = 8, Condensate = 9, GasPipe = 4;

        class Pt
        {
            public InspectPoint ip;
            public XRSimpleInteractable xi;
            public Renderer[] rends;
            public int task;
            public bool done, locked, isDrain;
            public int stage;
            public float held;
        }

        readonly List<Pt> pts = new List<Pt>();
        readonly bool[] done = new bool[TaskNames.Length];
        bool started, finished;
        float startTime;
        float condHeld, doorHeld;
        float ringShown;
        Pt drainPt;
        XRRayInteractor[] rays;
        float raysTime = -100f;

        static int TaskOf(InspectPoint p)
        {
            string g = p.group ?? "", t = p.title ?? "";
            if (g.StartsWith("Клапан")) return 1;
            if (g == "Кантовочные краны") return 2;
            if (g == "Обезграфичивание") return 3;
            if (g == "Газопроводы") return GasPipe;
            if (g == "Кантовочный механизм") return t == "Опорный ролик" ? 7 : 6;
            if (g == "Сточные линии") return Drain;
            return -1;
        }

        void Awake()
        {
            if (forceExam) ExamMode.Active = true;
            if (!ExamMode.Active) { Debug.Log("[Exam] режим обучения (ExamMode.Active=false). Для экзамена запускайте из MenuScene или включите forceExam."); enabled = false; return; }
            Debug.Log("[Exam] режим экзамена включён");
            done[Ppe] = ExamMode.PpeChecked && ExamMode.PpeOk;

            // вводное окно и окно входа в тоннель — тексты экзамена (до того, как окна применят свои тексты)
            var intro = FindFirstObjectByType<IntroWindow>(FindObjectsInactive.Include);
            if (intro != null)
            {
                intro.statusText = introStatus; intro.title = introTitle;
                intro.description = introDescription; intro.bullets = introBullets;
            }
            tunnelTrigger = tunnelTrigger != null ? tunnelTrigger : FindFirstObjectByType<ArrivalTrigger>(FindObjectsInactive.Include);
            if (tunnelTrigger != null && tunnelTrigger.window != null)
            {
                var w = tunnelTrigger.window;
                w.stepText = tunnelStep; w.title = tunnelTitle; w.description = tunnelDescription;
                w.checksHeader = tunnelChecksHeader; w.checks = tunnelChecks; w.buttonText = tunnelButton;
                w.autoConfirmSeconds = 0f;
            }

            // подсказки обучения: синие круги и всё, что от них зависит
            foreach (var m in FindObjectsByType<StepMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                m.enabled = false;
                m.gameObject.SetActive(false);
            }

            // точки осмотра: ромбики и карточки обучения выключены, считаем осмотр сами
            foreach (var ip in FindObjectsByType<InspectPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                int task = TaskOf(ip);
                ip.enabled = false;
                if (ip.marker != null) ip.marker.gameObject.SetActive(false);
                if (task < 0) continue;
                var p = new Pt { ip = ip, task = task, isDrain = task == Drain };
                p.xi = ip.GetComponent<XRSimpleInteractable>();
                p.rends = ip.GetComponentsInChildren<Renderer>();
                pts.Add(p);
                if (p.isDrain) drainPt = p;
            }

            drain = drain != null ? drain : FindFirstObjectByType<DrainValve>(FindObjectsInactive.Include);
            if (drain != null) { drain.examMode = true; drain.closeWindow = null; drain.window = null; drain.activateAfter = null; }
            journal = journal != null ? journal : FindFirstObjectByType<JournalWindow>(FindObjectsInactive.Include);
            if (journal != null) journal.Written += OnJournalWritten;
        }

        void Start()
        {
            if (!enabled) return;
            if (door == null)
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == "door.004") { door = t; break; }
            if (door == null) Debug.LogError("[Exam] не найден объект door.004: экзамен нельзя будет завершить. Назначьте Door в Inspector.");

            if (condensate == null)
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name.IndexOf("ondensat", System.StringComparison.OrdinalIgnoreCase) >= 0 && t.GetComponentInChildren<Renderer>() != null) { condensate = t; break; }
            if (condensate == null)
            {
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == "PointPosition_Step9") { condensate = t; break; }
                Debug.LogWarning("[Exam] не найден конденсатоотводчик: осмотр считается по кругу PointPosition_Step9 (" + (condensate != null ? "найден" : "не найден") + ").");
            }

            if (tunnelTrigger != null) tunnelTrigger.onArrived.AddListener(StartExam);
            else StartExam();   // триггера нет — считаем с начала сцены
            if (ringRoot != null) { UIDepth.AlwaysOnTop(ringRoot); ringRoot.SetActive(false); }
        }

        void StartExam()
        {
            if (started) return;
            started = true;
            startTime = Time.time;
            Debug.Log("[Exam] экзамен начался (вход в тоннель)");
        }

        void OnJournalWritten()
        {
            if (!started || finished) return;
            // запись имеет смысл после осмотра газопроводов
            if (done[GasPipe]) { done[Journal] = true; Debug.Log("[Exam] запись в журнал сделана"); }
            else Debug.Log("[Exam] запись в журнал сделана до осмотра газопроводов: не засчитана");
        }

        // ------------------------------------------------------------------ луч

        void RefreshRays()
        {
            if (rays == null || Time.unscaledTime - raysTime > 1f)
            {
                rays = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
                raysTime = Time.unscaledTime;
            }
        }

        // луч любой руки проходит рядом с центром объекта и ничто крупное не заслоняет
        bool RayNear(Bounds b)
        {
            RefreshRays();
            float rad = Mathf.Clamp(b.extents.magnitude * 0.6f, 0.15f, 0.6f);
            foreach (var r in rays)
            {
                if (r == null || !r.isActiveAndEnabled) continue;
                Transform t = r.rayOriginTransform != null ? r.rayOriginTransform : r.transform;
                Vector3 to = b.center - t.position;
                float along = Vector3.Dot(to, t.forward);
                if (along < 0.2f || along > 12f) continue;
                if (Vector3.Cross(t.forward, to).magnitude > rad + along * 0.03f) continue;
                RaycastHit hit;
                if (Physics.Linecast(t.position, b.center, out hit, ~0, QueryTriggerInteraction.Ignore)
                    && hit.distance < along - Mathf.Max(0.6f, b.extents.magnitude)) continue;
                return true;
            }
            return false;
        }

        static Bounds BoundsOf(Renderer[] rs, Vector3 fallback)
        {
            if (rs == null || rs.Length == 0) return new Bounds(fallback, Vector3.one * 0.3f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        bool IsAimed(Pt p)
        {
            if (p.xi != null && p.xi.isHovered) return true;
            return RayNear(BoundsOf(p.rends, p.ip.transform.position));
        }

        // луч попал в объект (или в его дочерние) — для двери
        bool RayHitsObject(Transform target)
        {
            RefreshRays();
            foreach (var r in rays)
            {
                if (r == null || !r.isActiveAndEnabled) continue;
                Transform t = r.rayOriginTransform != null ? r.rayOriginTransform : r.transform;
                RaycastHit hit;
                if (Physics.Raycast(t.position, t.forward, out hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                {
                    Transform h = hit.collider.transform;
                    if (h == target || h.IsChildOf(target) || target.IsChildOf(h) && h != target.root) return true;
                }
            }
            return RayNear(BoundsOf(target.GetComponentsInChildren<Renderer>(), target.position));
        }

        // ------------------------------------------------------------------ цикл

        void Update()
        {
            if (finished) return;
            bool blocked = StepWindow.AnyOpen;
            if (journalIcon != null) journalIcon.SetVisible(started && !blocked);
            if (!started) return;

            float dt = Time.deltaTime;
            float ring = 0f;
            if (!blocked)
            {
                // кран сточной линии: после открытия доступен снова (чтобы закрыть)
                if (drainPt != null && drain != null)
                    drainPt.locked = drainPt.stage == 1 ? !drain.Opened : drainPt.stage >= 2;

                Pt cand = null;
                foreach (var p in pts)
                {
                    if (p.done || p.locked) continue;
                    if (IsAimed(p)) { cand = p; break; }
                }
                foreach (var p in pts)
                {
                    if (p == cand) p.held += dt;
                    else if (p.held > 0f) p.held = Mathf.Max(0f, p.held - dt * 1.5f);
                }
                if (cand != null)
                {
                    ring = cand.held / inspectSeconds;
                    if (cand.held >= inspectSeconds) Complete(cand);
                }

                // конденсатоотводчик: смотреть на него 3 секунды
                if (!done[Condensate] && condensate != null)
                {
                    Camera cam = Camera.main;
                    bool look = false;
                    if (cam != null)
                    {
                        Bounds b = BoundsOf(condensate.GetComponentsInChildren<Renderer>(), condensate.position);
                        Vector3 to = b.center - cam.transform.position;
                        look = to.magnitude < 6f && Vector3.Angle(cam.transform.forward, to) < 15f;
                    }
                    if (!look) look = RayNear(BoundsOf(condensate.GetComponentsInChildren<Renderer>(), condensate.position));
                    condHeld = look ? condHeld + dt : Mathf.Max(0f, condHeld - dt * 1.5f);
                    if (cand == null && condHeld > 0f) ring = Mathf.Max(ring, condHeld / condensateSeconds);
                    if (condHeld >= condensateSeconds) { done[Condensate] = true; Debug.Log("[Exam] конденсатоотводчик осмотрен"); }
                }

                // дверь: завершение экзамена
                if (door != null)
                {
                    bool at = RayHitsObject(door);
                    doorHeld = at ? doorHeld + dt : Mathf.Max(0f, doorHeld - dt * 2f);
                    if (at && ring <= 0f) ring = doorHeld / doorSeconds;
                    if (doorHeld >= doorSeconds) { Finish(); return; }
                }
            }
            UpdateRing(ring, blocked);
        }

        void Complete(Pt p)
        {
            p.held = 0f;
            Debug.Log("[Exam] осмотрено: " + p.ip.group + " / " + p.ip.title);
            if (p.isDrain)
            {
                if (drain != null) drain.ExamInspected(p.ip);
                p.stage++;
                if (p.stage >= 2) p.done = true;     // кран закрывается; задание зачтётся, когда закроется
                return;
            }
            p.done = true;
            RecountTask(p.task);
        }

        void RecountTask(int task)
        {
            if (task == Drain) return;
            foreach (var q in pts) if (q.task == task && !q.done) return;
            done[task] = true;
            Debug.Log("[Exam] задание выполнено: " + TaskNames[task]);
        }

        void UpdateRing(float v, bool blocked)
        {
            bool show = !blocked && v > 0.02f;
            if (ringRoot == null) return;
            if (ringRoot.activeSelf != show) ringRoot.SetActive(show);
            if (!show) return;
            if (ringFill != null) ringFill.fillAmount = Mathf.Clamp01(v);
            Camera cam = Camera.main;
            if (cam == null) return;
            Transform c = cam.transform;
            ringRoot.transform.position = c.position + c.forward * 1.0f - c.up * 0.14f;
            ringRoot.transform.rotation = c.rotation;
        }

        void Finish()
        {
            finished = true;
            if (ringRoot != null) ringRoot.SetActive(false);
            if (journalIcon != null) journalIcon.SetVisible(false);
            if (drain != null) done[Drain] = drain.Closed;
            float seconds = Time.time - startTime;
            int ok = 0;
            foreach (bool d in done) if (d) ok++;
            Debug.Log("[Exam] экзамен завершён: " + ok + " из " + done.Length + ", время " + seconds.ToString("0") + " с");
            if (result != null) result.Show(TaskNames, done, seconds);
            else Debug.LogError("[Exam] нет итогового окна: запустите Koks → Build UI → 20.");
        }
    }
}
