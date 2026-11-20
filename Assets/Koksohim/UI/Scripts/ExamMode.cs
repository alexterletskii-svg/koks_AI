using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Режим прохождения. Хранится в статическом поле, поэтому «доживает» до загрузки FirstScene:
    /// в меню игрок выбирает «Обучение» или «Экзамен», а сцена осмотра смотрит на Active.
    /// </summary>
    public static class ExamMode
    {
        public static bool Active;

        // результат выбора СИЗ в экзамене (заполняет DressingChecker, показывается в итоговом окне)
        public static bool PpeChecked, PpeOk;
        public static void ResetPpe() { PpeChecked = false; PpeOk = false; }
    }

    /// <summary>Объект в MenuScene: кнопки выбора режима вызывают SetLearn / SetExam.</summary>
    public class ExamModeSetter : MonoBehaviour
    {
        void Awake() { ExamMode.Active = false; }   // меню загрузилось — режим сброшен, по умолчанию обучение
        public void SetLearn() { ExamMode.Active = false; ExamMode.ResetPpe(); }
        public void SetExam() { ExamMode.Active = true; ExamMode.ResetPpe(); Debug.Log("[Exam] выбран экзамен: ExamMode.Active = true"); }
    }
}
