using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Запись результатов на диск: Документы/KoksTrainer/results.jsonl (полная запись, по строке на прохождение)
    /// и results.csv (краткая таблица для Excel). Ошибки записи не мешают тренажёру — только пишутся в Console.
    /// </summary>
    public class FileResultSink : IResultSink
    {
        public static string Folder()
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrEmpty(docs)) docs = Application.persistentDataPath;
            return Path.Combine(docs, "KoksTrainer");
        }

        static string Csv(string s)
        {
            s = (s ?? "").Replace("\"", "\"\"");
            return "\"" + s + "\"";
        }

        public void Save(SessionRecord r)
        {
            try
            {
                string dir = Folder();
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "results.jsonl"), JsonUtility.ToJson(r) + "\n", new UTF8Encoding(false));

                string csv = Path.Combine(dir, "results.csv");
                bool header = !File.Exists(csv);
                var sb = new StringBuilder();
                if (header) sb.Append("﻿Дата;Сотрудник;Сценарий;Режим;Время, с;Выполнено;Всего;Зачёт;Ошибки\n");
                var errs = new StringBuilder();
                foreach (var e in r.errors) { if (errs.Length > 0) errs.Append(" | "); errs.Append(e.code); }
                sb.Append(Csv(r.startedAt)).Append(';').Append(Csv(r.employee)).Append(';').Append(Csv(r.scenarioTitle)).Append(';')
                  .Append(Csv(r.mode)).Append(';').Append(Mathf.RoundToInt(r.seconds)).Append(';').Append(r.tasksDone).Append(';')
                  .Append(r.tasksTotal).Append(';').Append(r.passed ? "да" : "нет").Append(';').Append(Csv(errs.ToString())).Append('\n');
                File.AppendAllText(csv, sb.ToString(), new UTF8Encoding(false));
                Debug.Log("[Results] результат сохранён: " + dir);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Results] не удалось сохранить результат: " + e.Message);
            }
        }
    }

    public static class ResultStore
    {
        public static IResultSink Sink = new FileResultSink();

        public static void Save(SessionRecord r)
        {
            if (r == null || Sink == null) return;
            Sink.Save(r);
        }
    }
}
