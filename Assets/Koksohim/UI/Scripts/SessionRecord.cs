using System;
using System.Collections.Generic;

namespace KoksUI
{
    [Serializable]
    public class TaskResult
    {
        public string name;
        public bool done;
    }

    /// <summary>Ошибка или нарушение во время прохождения (код — для статистики, текст — для людей).</summary>
    [Serializable]
    public class SessionError
    {
        public string code;
        public string text;
        public float atSeconds;
    }

    /// <summary>Одно прохождение сценария. Любой сценарий в конце формирует такую запись.</summary>
    [Serializable]
    public class SessionRecord
    {
        public string scenarioId;
        public string scenarioTitle;
        public string mode;            // "exam" | "learn" | "practice"
        public string employee;        // ФИО или табельный номер; пока пусто, пока не сделан выбор сотрудника
        public string startedAt;       // ISO 8601, локальное время
        public float seconds;
        public int tasksDone;
        public int tasksTotal;
        public bool passed;
        public List<TaskResult> tasks = new List<TaskResult>();
        public List<SessionError> errors = new List<SessionError>();
    }

    /// <summary>Кто проходит сценарий (задаётся окном выбора сотрудника, когда оно появится).</summary>
    public static class SessionInfo
    {
        public static string Employee = "";
    }

    /// <summary>Куда уходят записи. Сейчас — файлы на диске; позже можно подставить отправку на сервер/в LMS.</summary>
    public interface IResultSink
    {
        void Save(SessionRecord record);
    }
}
