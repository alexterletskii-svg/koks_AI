using UnityEngine;

namespace KoksUI
{
    /// <summary>Объект в MenuScene: кнопки выбора режима вызывают SetLearn / SetExam.</summary>
    public class ExamModeSetter : MonoBehaviour
    {
        void Awake() { ExamMode.Active = false; }   // меню загрузилось — режим сброшен, по умолчанию обучение
        public void SetLearn() { ExamMode.Active = false; ExamMode.ResetPpe(); }
        public void SetExam() { ExamMode.Active = true; ExamMode.ResetPpe(); Debug.Log("[Exam] выбран экзамен: ExamMode.Active = true"); }
    }
}
