using UnityEngine;
using UnityEngine.UI;

public class DressingChecker : MonoBehaviour
{
    [SerializeField] private ClothingItem[] allItems;  // все элементы
    [SerializeField] private Button checkButton;       // кнопка "Проверить"
    [SerializeField] private Button returnButton;       // кнопка "Вернуться"
    [SerializeField] private Button nextButton;       // кнопка "Далее"
    [SerializeField] private Text resultText;          // куда выводить результат
    [SerializeField] private CanvasGroup panelGroup;  // CanvasGroup на панели

    private void Start()
    {
        checkButton.onClick.AddListener(Check);
    }

    private void Check()
    {
        int correctOn = 0;
        int correctTotal = 0;
        int wrongOn = 0;

        foreach (var item in allItems)
        {
            if (item.IsCorrect)
            {
                correctTotal++;
                if (item.IsOn) correctOn++;
            }
            else
            {
                if (item.IsOn) wrongOn++;
            }
        }

        // Все правильные надеты и ни одного неправильного?
        if (correctOn == correctTotal && wrongOn == 0)
        {
            resultText.text = "✅ Всё верно! СИЗ надеты правильно.";
            resultText.color = Color.green;
            nextButton.gameObject.SetActive(true);
        }
        else
        {
            resultText.text = $"❌ Ошибки. Надето правильных: {correctOn}/{correctTotal}, лишних: {wrongOn}";
            resultText.color = Color.red;
            returnButton.gameObject.SetActive(true);
        }

        resultText.gameObject.SetActive(true);
    }
}
