using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KoksUI
{
    /// <summary>Клик по всей плитке (а не только по маленькой иконке) переключает Toggle.</summary>
    public class UIToggleProxy : MonoBehaviour, IPointerClickHandler
    {
        public Toggle toggle;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (toggle != null && toggle.interactable) toggle.isOn = !toggle.isOn;
        }
    }
}
