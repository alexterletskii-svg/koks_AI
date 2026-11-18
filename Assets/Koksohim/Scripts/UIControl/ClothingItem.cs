using UnityEngine;
using UnityEngine.UI;

public class ClothingItem : MonoBehaviour
{
    [Header("Связи")]
    [SerializeField] private Toggle toggle;      // галочка элемента
    [SerializeField] private GameObject layer;   // слой на человечке

    [Header("Настройки")]
    [SerializeField] private bool isCorrect;     // правильный ли это СИЗ

    public bool IsCorrect => isCorrect;
    public bool IsOn => toggle.isOn;

    private void Start()
    {
        Apply(toggle.isOn);
        toggle.onValueChanged.AddListener(Apply);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(Apply);
    }

    private void Apply(bool isOn)
    {
        layer.SetActive(isOn);   // галочка стоит -> слой виден
    }
}
