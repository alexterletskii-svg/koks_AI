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
    }

    /// <summary>Объект в MenuScene: кнопки выбора режима вызывают SetLearn / SetExam.</summary>
    public class ExamModeSetter : MonoBehaviour
    {
        void Awake() { ExamMode.Active = false; }   // меню загрузилось — режим сброшен, по умолчанию обучение
        public void SetLearn() { ExamMode.Active = false; }
        public void SetExam() { ExamMode.Active = true; }
    }
}
