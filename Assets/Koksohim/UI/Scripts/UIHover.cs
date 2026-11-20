using UnityEngine;
using UnityEngine.EventSystems;

namespace KoksUI
{
    /// <summary>Показывает объект-подсветку, пока на элемент наведён луч.</summary>
    public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject target;

        void OnDisable()
        {
            if (target != null) target.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (target != null) target.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (target != null) target.SetActive(false);
        }
    }
}
