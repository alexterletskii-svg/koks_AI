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
    /// завершает экзамен, когда игрок нажимает ручку двери выхода и подтверждает завершение, и показывает итоговое окно.
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
        [Tooltip("Дверь выхода, на которую вешается журнал (ищется по имени door.003 / Door1)")]
        public Transform journalDoor;
        public Transform condensate;
        [Tooltip("Точка, куда смотреть при осмотре конденсатоотводчика (пустой объект в яме). Если пусто — ищется объект ExamCondensateTarget")]
        public Transform condensateTarget;
        [Tooltip("Радиус зоны взгляда вокруг точки конденсатоотводчика, м")]
        public float condensateRadius = 0.6f;
        public DrainValve drain;
        public JournalWindow journal;
        public ArrivalTrigger tunnelTrigger;

        [Header("Окна и значки (собирает Koks → Build UI → 20)")]
        public ExamResultWindow result;
        public ExamFinishWindow finishWindow;
        public ExamJournalIcon journalIcon;
        public GameObject ringRoot;
        public Image ringFill;

        [Header("Вводные тексты экзамена")]
        public string introStatus = "Экзамен начался";
        public string introTitle = "Экзамен: осмотр тоннелей коксовой батареи";
        [TextArea(2, 5)]
        public string introDescription = "Осмотр проводится самостоятельно, без подсказок. Время засчитывается с момента входа в газовый тоннель.";
        public string[] introBullets =
        {
            "Держите луч на детали 2 секунды",
            "Результаты осмотра запишите в журнал",
            "Завершите осмотр у двери выхода"
        };
        public string tunnelStep = "Экзамен";
        public string tunnelTitle = "Вы в газовом тоннеле";
        [TextArea(2, 5)]
        public string tunnelDescription = "Осмотрите оборудование самостоятельно: подсказок нет. Когда закончите, нажмите на ручку двери выхода.";
        public string tunnelChecksHeader = "Как действовать";
        public string[] tunnelChecks =
        {
            "Держите луч на детали 2 секунды",
            "Результаты запишите в журнал",
            "Завершите осмотр у двери выхода"
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
        readonly List<GameObject> exitCanvases = new List<GameObject>();
        float finishCooldown;
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
            if (g == "Сточные линии") return Drain;
            if (g.StartsWith("Конденсат")) return Condensate;
            // кантовочный механизм: штанги и канаты — одно задание, блочки (ролики) и опорные ролики — другое
            if (g == "Кантовочный механизм" && (t.StartsWith("Штанг") || t.StartsWith("Канат"))) return 6;
            if (g == "Кантовочный механизм" || g.StartsWith("Ролик")) return 7;
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

            // голосовое приветствие обучения в экзамене не нужно
            foreach (var w in FindObjectsByType<WelcomeAudioPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                w.enabled = false;
                var a = w.GetComponent<AudioSource>();
                if (a != null) { a.playOnAwake = false; a.Stop(); }
            }

            // окна «Перейти в другое помещение» (ручка двери): в экзамене вместо них — подтверждение завершения
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "CanvasExit") exitCanvases.Add(t.gameObject);

            // подсказки обучения: синие круги и всё, что от них зависит
            foreach (var m in FindObjectsByType<StepMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                m.enabled = false;
                m.gameObject.SetActive(false);
            }

            // остальные круги-указатели обучения (в комнате и т.п.): всё, что названо PointPosition*
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name.StartsWith("PointPosition")) t.gameObject.SetActive(false);

            // точки осмотра: ромбики и карточки обучения выключены, считаем осмотр сами
            foreach (var ip in FindObjectsByType<InspectPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                int task = TaskOf(ip);
                ip.enabled = false;
                if (ip.marker != null) ip.marker.gameObject.SetActive(false);
                if (task < 0 || task == Condensate) continue;   // конденсатоотводчик считается взглядом (его коллайдер на весь тоннель)
                var p = new Pt { ip = ip, task = task, isDrain = task == Drain };
                p.xi = ip.GetComponent<XRSimpleInteractable>();
                p.rends = ip.GetComponentsInChildren<Renderer>();
                pts.Add(p);
                if (p.isDrain) drainPt = p;
            }
            // какие детали входят в задания (для проверки в Console)
            for (int t = 0; t < TaskNames.Length; t++)
            {
                var names = new List<string>();
                foreach (var q in pts) if (q.task == t && !names.Contains(Key(q))) names.Add(Key(q));
                if (names.Count > 0) Debug.Log("[Exam] " + TaskNames[t] + ": " + string.Join("; ", names));
            }

            drain = drain != null ? drain : FindFirstObjectByType<DrainValve>(FindObjectsInactive.Include);
            if (drain != null) { drain.examMode = true; drain.closeWindow = null; drain.window = null; drain.activateAfter = null; }
            journal = journal != null ? journal : FindFirstObjectByType<JournalWindow>(FindObjectsInactive.Include);
            if (journal != null) { journal.Written += OnJournalWritten; journal.entryProvider = JournalEntry; }
        }

        static readonly int[] JournalNeeds = { 1, 2, 3, 4, 6, 7, 9 };
        static readonly string[] JournalShort = { "", "клапаны", "кантовочные краны", "обезграфичивание", "газопроводы", "", "штанги и канаты", "блочки и ролики", "", "конденсатоотводчик" };

        JournalWindow.Entry JournalEntry()
        {
            var missing = new List<string>();
            foreach (int t in JournalNeeds) if (!done[t]) missing.Add(JournalShort[t]);
            var e = new JournalWindow.Entry();
            if (missing.Count == 0) { e.valid = true; e.content = "Осмотр тоннелей выполнен."; e.remarks = "Замечаний нет"; }
            else { e.valid = false; e.content = "Не осмотрено (" + missing.Count + "): " + string.Join(", ", missing); e.remarks = "Не принято"; }
            return e;
        }

        void Start()
        {
            if (!enabled) return;
            if (door == null)
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == "door.004") { door = t; break; }

            if (condensateTarget == null)
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == "ExamCondensateTarget") { condensateTarget = t; break; }
            if (condensateTarget == null) Debug.LogWarning("[Exam] нет точки ExamCondensateTarget: конденсатоотводчик считается по всей модели. Создайте пустой объект ExamCondensateTarget в яме у конденсатоотводчика.");
            else Debug.Log("[Exam] конденсатоотводчик: смотреть на точку " + condensateTarget.position);
            if (condensate == null)
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == "CondensateCollector") { condensate = t; break; }
            if (condensate == null)
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name.IndexOf("ondensat", System.StringComparison.OrdinalIgnoreCase) >= 0 && t.GetComponentInChildren<Renderer>() != null) { condensate = t; break; }
            if (condensate == null)
            {
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == "PointPosition_Step9") { condensate = t; break; }
                Debug.LogWarning("[Exam] не найден конденсатоотводчик: осмотр считается по кругу PointPosition_Step9 (" + (condensate != null ? "найден" : "не найден") + ").");
            }

            if (journalDoor == null)
            {
                foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t.name == "door.003") { journalDoor = t; break; }
                if (journalDoor == null)
                    foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (t.name == "Door1") { journalDoor = t; break; }
                if (journalDoor == null) Debug.LogWarning("[Exam] не найдена дверь door.003 / Door1: журнал некуда повесить. Назначьте Journal Door в Inspector.");
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
            if (journalIcon != null) journalIcon.PlaceOnDoor(journalDoor);
            Debug.Log("[Exam] экзамен начался (вход в тоннель)");
        }

        void OnJournalWritten()
        {
            if (!started || finished) return;
            done[Journal] = true;
            Debug.Log("[Exam] запись в журнал сделана");
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

        // видимый луч руки: интерактор включён и его линия отображается
        static bool RayVisible(XRRayInteractor r)
        {
            if (r == null || !r.isActiveAndEnabled) return false;
            var lr = r.GetComponent<LineRenderer>();
            if (lr != null && (!lr.enabled || !lr.gameObject.activeInHierarchy)) return false;
            return true;
        }

        // первое попадание луча в сцену, не считая коллайдеров самого игрока (руки, тело)
        static bool FirstHit(Transform origin, out RaycastHit first)
        {
            first = default(RaycastHit);
            var hits = Physics.RaycastAll(origin.position, origin.forward, 15f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; bool any = false;
            Transform rig = origin.root;
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.transform.IsChildOf(rig)) continue;
                if (h.distance < best) { best = h.distance; first = h; any = true; }
            }
            return any;
        }

        // луч наведён на объект: попал в его коллайдер, либо проходит рядом с ним и до него ничего не заслоняет (сетка/ограждение у самой детали не мешает)
        bool RayAt(Transform root, Collider[] own, Bounds b)
        {
            RefreshRays();
            float rad = Mathf.Clamp(b.extents.magnitude * 0.5f, 0.12f, 0.45f);
            foreach (var r in rays)
            {
                if (!RayVisible(r)) continue;
                Transform t = r.rayOriginTransform != null ? r.rayOriginTransform : r.transform;
                RaycastHit hit;
                bool has = FirstHit(t, out hit);
                if (has)
                {
                    Transform h = hit.collider.transform;
                    if (h == root || h.IsChildOf(root)) return true;
                    if (own != null) foreach (var c in own) if (c == hit.collider) return true;
                }
                Vector3 to = b.center - t.position;
                float along = Vector3.Dot(to, t.forward);
                if (along < 0.2f || along > 12f) continue;
                if (Vector3.Cross(t.forward, to).magnitude > rad + along * 0.02f) continue;
                if (has && hit.distance < along - b.extents.magnitude - 0.7f) continue;   // заслонено стеной/другим оборудованием
                return true;
            }
            return false;
        }

        // взгляд: центр модели почти по центру поля зрения, не дальше 6 м и не за стеной
        static bool GazeAt(Transform root, Bounds b)
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Transform c = cam.transform;
            Vector3 to = b.center - c.position;
            float dist = to.magnitude;
            if (dist > 6f || Vector3.Angle(c.forward, to) > 12f) return false;
            RaycastHit hit;
            if (FirstHit(c, out hit))
            {
                Transform h = hit.collider.transform;
                if (h == root || h.IsChildOf(root)) return true;
                if (hit.distance < dist - b.extents.magnitude - 0.5f) return false;   // заслонено
            }
            return true;
        }

        // взгляд на точку: она в центре поля зрения (с допуском на радиус), не дальше 5 м; решётки над ямой не мешают
        static bool GazePoint(Vector3 point, float radius)
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Transform c = cam.transform;
            Vector3 to = point - c.position;
            float dist = to.magnitude;
            if (dist > 5f) return false;
            float maxAngle = Mathf.Max(10f, Mathf.Atan2(radius, Mathf.Max(0.1f, dist)) * Mathf.Rad2Deg);
            return Vector3.Angle(c.forward, to) <= maxAngle;
        }

        static Bounds BoundsOf(Renderer[] rs, Vector3 fallback)
        {
            if (rs == null || rs.Length == 0) return new Bounds(fallback, Vector3.one * 0.3f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static Bounds BoundsOf(Pt p)
        {
            bool any = false; Bounds b = new Bounds(p.ip.transform.position, Vector3.one * 0.3f);
            if (p.xi != null)
                foreach (var c in p.xi.colliders)
                {
                    if (c == null) continue;
                    if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds);
                }
            if (any) return b;
            return BoundsOf(p.rends, p.ip.transform.position);
        }

        bool IsAimed(Pt p)
        {
            if (p.xi != null && p.xi.isHovered)
                foreach (var r in rays ?? new XRRayInteractor[0]) if (RayVisible(r)) return true;
            return RayAt(p.ip.transform, p.xi != null ? p.xi.colliders.ToArray() : null, BoundsOf(p));
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
            return RayAt(target, null, BoundsOf(target.GetComponentsInChildren<Renderer>(), target.position));
        }

        // ------------------------------------------------------------------ цикл

        void Update()
        {
            if (finished) return;
            bool blocked = StepWindow.AnyOpen;
            if (journalIcon != null) journalIcon.SetVisible(started && !blocked);
            if (!started) return;

            // ручка двери открыла окно перехода — показываем своё: 2 перехода и «Завершить экзамен»
            foreach (var ec in exitCanvases)
            {
                if (ec == null || !ec.activeSelf) continue;
                var rooms = ExamFinishWindow.ReadRooms(new[] { ec });   // переходы именно из этого помещения
                ec.SetActive(false);
                if (Time.unscaledTime < finishCooldown) continue;
                if (finishWindow != null && !finishWindow.IsOpen)
                {
                    Debug.Log("[Exam] ручка двери: окно выбора (переходов: " + rooms.Count + ")");
                    finishWindow.Show(rooms, Finish, () => finishCooldown = Time.unscaledTime + 1.5f);
                }
                else if (finishWindow == null) { Finish(); return; }
            }

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

                // конденсатоотводчик (CondensateCollector): как в обучении — посмотреть на него 3 секунды (или навести луч)
                if (!done[Condensate] && (condensate != null || condensateTarget != null))
                {
                    bool look;
                    if (condensateTarget != null) look = GazePoint(condensateTarget.position, condensateRadius);
                    else
                    {
                        Bounds cb = BoundsOf(condensate.GetComponentsInChildren<Renderer>(), condensate.position);
                        look = GazeAt(condensate, cb) || RayAt(condensate, null, cb);
                    }
                    condHeld = look ? condHeld + dt : Mathf.Max(0f, condHeld - dt * 1.5f);
                    if (cand == null && condHeld > 0f) ring = Mathf.Max(ring, condHeld / condensateSeconds);
                    if (condHeld >= condensateSeconds) { done[Condensate] = true; Debug.Log("[Exam] конденсатоотводчик осмотрен"); }
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
            // одна и та же деталь в нескольких местах сцены (например, опорные ролики) — достаточно осмотреть одну
            foreach (var q in pts) if (!q.done && q.task == p.task && Key(q) == Key(p)) q.done = true;
            RecountTask(p.task);
        }

        static string Key(Pt p)
        {
            string t = (p.ip.title ?? "").Trim();
            return p.task == 7 ? t : (p.ip.group ?? "") + "/" + t;   // ролики: «Опорный ролик» в двух группах — одна деталь
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
