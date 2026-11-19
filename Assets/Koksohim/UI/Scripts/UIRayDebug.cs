using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace KoksUI
{
    /// <summary>
    /// Отладка: пишет в Console, на какой элемент интерфейса попал луч и нажат ли курок (UI Press).
    /// Вешается на XR Ray Interactor через меню Koks → Debug (в режиме Play).
    /// </summary>
    [RequireComponent(typeof(XRRayInteractor))]
    public class UIRayDebug : MonoBehaviour
    {
        XRRayInteractor ray;
        GameObject lastHit;
        bool lastPress;
        bool lastSelect;

        void Start()
        {
            ray = GetComponent<XRRayInteractor>();
            Debug.Log("[UIRayDebug] " + name + ": enableUIInteraction=" + ray.enableUIInteraction
                      + ", maxRaycastDistance=" + ray.maxRaycastDistance, this);
        }

        void Update()
        {
            if (ray == null) return;

            GameObject hit = null;
            RaycastResult result;
            if (ray.TryGetCurrentUIRaycastResult(out result))
                hit = result.gameObject;

            if (hit != lastHit)
            {
                lastHit = hit;
                Debug.Log("[UIRayDebug] " + name + ": луч на UI -> " + (hit != null ? hit.name : "ничего"), this);
            }

            bool press = ray.uiPressInput.ReadIsPerformed();
            if (press != lastPress)
            {
                lastPress = press;
                Debug.Log("[UIRayDebug] " + name + ": курок (UI Press) = " + press, this);
            }

            bool select = ray.selectInput.ReadIsPerformed();
            if (select != lastSelect)
            {
                lastSelect = select;
                Debug.Log("[UIRayDebug] " + name + ": боковая (Select) = " + select, this);
            }
        }
    }
}
