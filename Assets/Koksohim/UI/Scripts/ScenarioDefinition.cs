using System;
using System.Collections.Generic;
using UnityEngine;

namespace KoksUI
{
    /// <summary>Тип задания: от него зависит, как контроллер его считает.</summary>
    public enum TaskKind
    {
        /// <summary>Выбор СИЗ в раздевалке (результат приходит из ExamMode).</summary>
        Ppe,
        /// <summary>Осмотр деталей сцены: держать луч на детали (точки InspectPoint, подходящие под правила).</summary>
        Inspect,
        /// <summary>Запись в эксплуатационный журнал.</summary>
        Journal,
        /// <summary>Кран сточной линии: открыть и закрыть (DrainValve).</summary>
        Drain,
        /// <summary>Осмотр точки в сцене лучом (объект ExamCondensateTarget): для деталей с огромным коллайдером.</summary>
        RayPoint
    }

    /// <summary>Правило «эта точка осмотра относится к заданию»: все заполненные условия должны выполняться.</summary>
    [Serializable]
    public class MatchRule
    {
        [Tooltip("Группа точки осмотра равна этому значению")]
        public string groupEquals;
        [Tooltip("Группа точки осмотра начинается с этого значения")]
        public string groupStartsWith;
        [Tooltip("Название точки осмотра начинается с этого значения")]
        public string titleStartsWith;

        public MatchRule() { }
        public MatchRule(string groupEquals, string groupStartsWith = null, string titleStartsWith = null)
        {
            this.groupEquals = groupEquals; this.groupStartsWith = groupStartsWith; this.titleStartsWith = titleStartsWith;
        }

        public bool Matches(string group, string title)
        {
            group = group ?? ""; title = title ?? "";
            bool any = false;
            if (!string.IsNullOrEmpty(groupEquals)) { any = true; if (group != groupEquals) return false; }
            if (!string.IsNullOrEmpty(groupStartsWith)) { any = true; if (!group.StartsWith(groupStartsWith)) return false; }
            if (!string.IsNullOrEmpty(titleStartsWith)) { any = true; if (!title.StartsWith(titleStartsWith)) return false; }
            return any;
        }
    }

    [Serializable]
    public class ScenarioTask
    {
        [Tooltip("Название в итоговом окне")]
        public string name;
        [Tooltip("Короткое название для записи в журнале («не осмотрено: …»)")]
        public string shortName;
        public TaskKind kind = TaskKind.Inspect;
        [Tooltip("Без этого задания запись в журнале не засчитывается")]
        public bool requiredForJournal;
        [Tooltip("Для Inspect: какие точки осмотра входят в задание. Задания проверяются по порядку, побеждает первое подходящее")]
        public List<MatchRule> rules = new List<MatchRule>();
    }

    /// <summary>
    /// Описание сценария экзамена: задания, их привязка к деталям сцены, условия зачёта и тексты журнала.
    /// ExamController читает его. Если ассета Resources/Scenarios/TunnelInspection нет — берётся встроенный (TunnelInspection()).
    /// Новый ассет: Koks → Scenario → Создать ассет «Осмотр тоннелей» (правится в Inspector).
    /// </summary>
    [CreateAssetMenu(fileName = "Scenario", menuName = "Koks/Сценарий")]
    public class ScenarioDefinition : ScriptableObject
    {
        public const string ResourcePath = "Scenarios/TunnelInspection";

        [Tooltip("Латиницей, без пробелов: попадает в записи результатов")]
        public string id = "tunnel-inspection";
        public string title = "Осмотр газового тоннеля";
        public List<ScenarioTask> tasks = new List<ScenarioTask>();

        [Header("Зачёт")]
        [Tooltip("Сколько заданий нужно выполнить для зачёта (0 = все)")]
        public int passMinTasks = 0;

        [Header("Запись в журнале")]
        public string journalOk = "Осмотр тоннелей выполнен.";
        public string journalOkRemarks = "Замечаний нет";
        public string journalMissingPrefix = "Не осмотрено";
        public string journalRejectedRemarks = "Не принято";

        public int Count { get { return tasks.Count; } }

        public string[] Names()
        {
            var r = new string[tasks.Count];
            for (int i = 0; i < r.Length; i++) r[i] = tasks[i].name;
            return r;
        }

        public int IndexOf(TaskKind kind)
        {
            for (int i = 0; i < tasks.Count; i++) if (tasks[i].kind == kind) return i;
            return -1;
        }

        /// <summary>Номер задания для точки осмотра или -1.</summary>
        public int TaskOf(string group, string title)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].kind != TaskKind.Inspect && tasks[i].kind != TaskKind.Drain && tasks[i].kind != TaskKind.RayPoint) continue;
                foreach (var r in tasks[i].rules) if (r.Matches(group, title)) return i;
            }
            return -1;
        }

        public int PassNeeded { get { return passMinTasks <= 0 ? tasks.Count : Mathf.Min(passMinTasks, tasks.Count); } }

        static ScenarioDefinition cached;

        /// <summary>Сбросить кэш (при каждом запуске сцены, чтобы правки ассета подхватывались).</summary>
        public static void ClearCache() { cached = null; }

        /// <summary>Сценарий из Resources или встроенный.</summary>
        public static ScenarioDefinition Load()
        {
            if (cached != null) return cached;
            cached = Resources.Load<ScenarioDefinition>(ResourcePath);
            if (cached == null) cached = TunnelInspection();
            return cached;
        }

        /// <summary>Встроенный сценарий «Осмотр тоннелей коксовой батареи» (то, что было зашито в коде).</summary>
        public static ScenarioDefinition TunnelInspection()
        {
            var s = CreateInstance<ScenarioDefinition>();
            s.hideFlags = HideFlags.DontSave;
            s.id = "tunnel-inspection";
            s.title = "Осмотр газового тоннеля";
            s.tasks = new List<ScenarioTask>
            {
                new ScenarioTask { name = "Выбор средств индивидуальной защиты", kind = TaskKind.Ppe },
                new ScenarioTask { name = "Осмотр газовоздушных клапанов", shortName = "клапаны", requiredForJournal = true,
                    rules = { new MatchRule(null, "Клапан") } },
                new ScenarioTask { name = "Осмотр кантовочных кранов", shortName = "кантовочные краны", requiredForJournal = true,
                    rules = { new MatchRule("Кантовочные краны") } },
                new ScenarioTask { name = "Крышки устройства обезграфичивания", shortName = "обезграфичивание", requiredForJournal = true,
                    rules = { new MatchRule("Обезграфичивание") } },
                new ScenarioTask { name = "Осмотр газопроводов", shortName = "газопроводы", requiredForJournal = true,
                    rules = { new MatchRule("Газопроводы") } },
                new ScenarioTask { name = "Запись в эксплуатационный журнал", kind = TaskKind.Journal },
                // штанги и канаты — одно задание, блочки (ролики) и опорные ролики — другое; правило штанг стоит раньше общего правила механизма
                new ScenarioTask { name = "Штанги и канаты кантовочного механизма", shortName = "штанги и канаты", requiredForJournal = true,
                    rules = { new MatchRule("Кантовочный механизм", null, "Штанг"), new MatchRule("Кантовочный механизм", null, "Канат") } },
                new ScenarioTask { name = "Блочки и опорные ролики", shortName = "блочки и ролики", requiredForJournal = true,
                    rules = { new MatchRule("Кантовочный механизм"), new MatchRule(null, "Ролик") } },
                new ScenarioTask { name = "Сточная линия: открыть и закрыть кран", kind = TaskKind.Drain,
                    rules = { new MatchRule("Сточные линии") } },
                new ScenarioTask { name = "Осмотр конденсатоотводчика", shortName = "конденсатоотводчик", kind = TaskKind.RayPoint, requiredForJournal = true,
                    rules = { new MatchRule(null, "Конденсат") } },
            };
            return s;
        }
    }
}
