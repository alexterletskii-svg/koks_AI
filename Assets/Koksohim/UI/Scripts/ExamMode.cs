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
        public static bool PpeChecked, PpeOk, GasAnalyzerOn;
        public static void ResetPpe() { PpeChecked = false; PpeOk = false; GasAnalyzerOn = false; }
    }
}
