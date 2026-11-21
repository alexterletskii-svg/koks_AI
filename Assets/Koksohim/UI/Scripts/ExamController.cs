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
    /// детали сам — держит луч на детали 2 секунды. Считает задания сценария (ScenarioDefinition) и время (с входа в газовый тоннель),
    /// завершает экзамен, когда игрок нажимает ручку двери выхода и подтверждает завершение, и показывает итоговое окно.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ExamController : MonoBehaviour
    {
        [Tooltip("Для проверки в редакторе: запустить FirstScene сразу в режиме экзамена, минуя меню")]
        public bool forceExam;

        [Header("Сценарий")]
        [Tooltip("Описание заданий. Пусто — берётся Resources/Scenarios/TunnelInspection, а если его нет — встроенный «Осмотр тоннелей»")]
        public ScenarioDefinition scenario;

        [Header("Сколько секунд держать")]
        public float inspectSeconds = 2f;
        public float doorSeconds = 1.5f;
        public float condensateSeconds = 3f;

        [Header("Объекты (находятся сами, можно назначить вручную)")]
        public Transform door;
        [Tooltip("Дверь выхода, на которую вешается журнал (ищется по имени door.003 / Door1)")]
        public Transform journalDoor;
        public Transform condensate;
        [Tooltip("Точка, на которую наводят луч при осмотре конденсатоотводчика (пустой объект в яме). Если пусто — ищется объект ExamCondensateTarget")]
        public Transform condensateTarget;
        [Tooltip("Радиус зоны вокруг точки конденсатоотводчика, в которой луч считается наведённым, м")]
        public float condensateRadius = 0.6f;
        [Tooltip("Радиус наведения луча на точку-якорь (CoverTarget_obezgr и т.п.), м")]
        public float anchorRadius = 0.5f;
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

        /// <summary>Названия заданий текущего сценария (для итогового окна и сборщика меню 20).</summary>
        public static string[] TaskNames { get { return ScenarioDefinition.Load().Names(); } }

        ScenarioDefinition sc;
        int ppeIdx, journalIdx, drainIdx, condIdx;
        bool condHasPoints;

        class Pt
        {
            public InspectPoint ip;
            public XRSimpleInteractable xi;
            public Transform anchor;
            public Renderer[] rends;
            public int task;
            public bool done, locked, isDrain;
            public int stage;
            public float held;
        }

        readonly List<Pt> pts = new List<Pt>();
        readonly List<GameObject> exitCanvases = new List<GameObject>();
        float finishCooldown;
        bool[] done;
        readonly List<SessionError> errors = new List<SessionError>();
        bool started, finished;
        float startTime;
        System.DateTime startedAt;
        float condHeld, doorHeld;
        float ringShown;
        Pt drainPt;
        XRRayInteractor[] rays;
        float raysTime = -100f;

        int TaskOf(InspectPoint p) { return sc.TaskOf(p.group, p.title); }

        void AddError(string code, string text)
        {
            errors.Add(new SessionError { code = code, text = text, atSeconds = started ? Time.time - startTime : 0f });
            Debug.Log("[Exam] ошибка: " + code + " — " + text);
        }

        void Awake()
        {
            if (forceExam) ExamMode.Active = true;
            if (!ExamMode.Active) { Debug.Log("[Exam] режим обучения (ExamMode.Active=false). Для экзамена запускайте из MenuScene или включите forceExam."); enabled = false; return; }
            Debug.Log("[Exam] режим экзамена включён");
            ScenarioDefinition.ClearCache();
            sc = scenario != null ? scenario : ScenarioDefinition.Load();
            ppeIdx = sc.IndexOf(TaskKind.Ppe); journalIdx = sc.IndexOf(TaskKind.Journal);
            drainIdx = sc.IndexOf(TaskKind.Drain); condIdx = sc.IndexOf(TaskKind.RayPoint);
            done = new bool[sc.Count];
            if (ppeIdx >= 0) done[ppeIdx] = ExamMode.PpeChecked && ExamMode.PpeOk;
            if (!ExamMode.PpeChecked) AddError("ppe_not_checked", "Средства защиты не проверялись");
            else
            {
                if (!ExamMode.PpeOk) AddError("ppe_wrong", "Неверный набор средств защиты");
                if (!ExamMode.GasAnalyzerOn) AddError("no_gas_analyzer", "Газоанализатор не выбран");
            }

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
                if (task < 0) continue;
                var p = new Pt { ip = ip, task = task, isDrain = task == drainIdx };
                p.xi = ip.GetComponent<XRSimpleInteractable>();
                p.anchor = ip.anchor != null ? ip.anchor : InspectPoint.ResolveAnchor(ip.group);
                p.rends = ip.GetComponentsInChildren<Renderer>();
                pts.Add(p);
                if (p.isDrain) drainPt = p;
            }
            condHasPoints = false;
            foreach (var q in pts) if (q.task == condIdx) condHasPoints = true;   // есть обычная точка осмотра — запасной счёт по лучу не нужен
            // какие детали входят в задания (для проверки в Console)
            for (int t = 0; t < sc.Count; t++)
            {
                var names = new List<string>();
                foreach (var q in pts) if (q.task == t && !names.Contains(Key(q))) names.Add(Key(q));
                if (names.Count > 0) Debug.Log("[Exam] " + sc.tasks[t].name + ": " + string.Join("; ", names));
            }

            drain = drain != null ? drain : FindFirstObjectByType<DrainValve>(FindObjectsInactive.Include);
            if (drain != null) { drain.examMode = true; drain.closeWindow = null; drain.window = null; drain.activateAfter = null; }
            journal = journal != null ? journal : FindFirstObjectByType<JournalWindow>(FindObjectsInactive.Include);
            if (journal != null) { journal.Written += OnJournalWritten; journal.entryProvider = JournalEntry; }
        }

        JournalWindow.Entry JournalEntry()
        {
            var missing = new List<string>();
            for (int t = 0; t < sc.Count; t++)
                if (sc.tasks[t].requiredForJournal && !done[t]) missing.Add(sc.tasks[t].shortName);
            var e = new JournalWindow.Entry();
            if (missing.Count == 0) { e.valid = true; e.content = sc.journalOk; e.remarks = sc.journalOkRemarks; }
            else
            {
                e.valid = false;
                e.content = sc.journalMissingPrefix + " (" + missing.Count + "): " + string.Join(", ", missing);
                e.remarks = sc.journalRejectedRemarks;
                AddError("journal_without_inspection", "Запись в журнал до осмотра: " + string.Join(", ", missing));
            }
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
            else Debug.Log("[Exam] конденсатоотводчик: навести луч на точку " + condensateTarget.position);
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
            startedAt = System.DateTime.Now;
            if (journalIcon != null) journalIcon.PlaceOnDoor(journalDoor);
            Debug.Log("[Exam] экзамен начался (вход в тоннель)");
        }

        void OnJournalWritten()
        {
            if (!started || finished) return;
            if (journalIdx >= 0) done[journalIdx] = true;
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

        // луч любой руки проходит рядом с точкой (ограждение у точки не мешает), не дальше 12 м
        bool RayNearPoint(Vector3 point, float radius)
        {
            RefreshRays();
            foreach (var r in rays)
            {
                if (!RayVisible(r)) continue;
                Transform t = r.rayOriginTransform != null ? r.rayOriginTransform : r.transform;
                Vector3 to = point - t.position;
                float along = Vector3.Dot(to, t.forward);
                if (along < 0.2f || along > 12f) continue;
                if (Vector3.Cross(t.forward, to).magnitude <= radius + along * 0.02f) return true;
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
            if (p.anchor != null) return RayNearPoint(p.anchor.position, anchorRadius);   // деталь с огромным коллайдером: только луч у точки
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
            bool journalOpen = journal != null && journal.IsOpen;
            if (journalIcon != null) { journalIcon.SetVisible(started && (!blocked || journalOpen)); journalIcon.SetCloseMode(journalOpen); }
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

                // конденсатоотводчик: держать луч на точке ExamCondensateTarget 3 секунды
                if (condIdx >= 0 && !condHasPoints && !done[condIdx] && (condensate != null || condensateTarget != null))
                {
                    bool look;
                    if (condensateTarget != null) look = RayNearPoint(condensateTarget.position, condensateRadius);
                    else look = RayAt(condensate, null, BoundsOf(condensate.GetComponentsInChildren<Renderer>(), condensate.position));
                    condHeld = look ? condHeld + dt : Mathf.Max(0f, condHeld - dt * 1.5f);
                    if (cand == null && condHeld > 0f) ring = Mathf.Max(ring, condHeld / condensateSeconds);
                    if (condHeld >= condensateSeconds) { done[condIdx] = true; Debug.Log("[Exam] конденсатоотводчик осмотрен"); }
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
            if (task == drainIdx) return;
            foreach (var q in pts) if (q.task == task && !q.done) return;
            done[task] = true;
            Debug.Log("[Exam] задание выполнено: " + sc.tasks[task].name);
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
            if (drain != null && drainIdx >= 0) done[drainIdx] = drain.Closed;
            float seconds = Time.time - startTime;
            int ok = 0;
            foreach (bool d in done) if (d) ok++;
            Debug.Log("[Exam] экзамен завершён: " + ok + " из " + done.Length + ", время " + seconds.ToString("0") + " с");
            SaveRecord(ok, seconds);
            if (result != null) result.Show(sc.Names(), done, seconds);
            else Debug.LogError("[Exam] нет итогового окна: запустите Koks → Build UI → 20.");
        }

        void SaveRecord(int ok, float seconds)
        {
            var r = new SessionRecord
            {
                scenarioId = sc.id, scenarioTitle = sc.title, mode = "exam", employee = SessionInfo.Employee ?? "",
                startedAt = startedAt.ToString("yyyy-MM-dd'T'HH:mm:ss"), seconds = seconds,
                tasksDone = ok, tasksTotal = done.Length, passed = ok >= sc.PassNeeded
            };
            for (int i = 0; i < done.Length; i++) r.tasks.Add(new TaskResult { name = sc.tasks[i].name, done = done[i] });
            r.errors.AddRange(errors);
            ResultStore.Save(r);
        }
    }
}
