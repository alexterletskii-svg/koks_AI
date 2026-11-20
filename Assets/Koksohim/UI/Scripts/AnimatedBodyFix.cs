using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Объекты с Animator и обычным (не кинематическим) Rigidbody «воюют» с анимацией: стоит рукам или телу игрока
    /// задеть коллайдер, физика останавливает движение. При старте сцены такие тела переводятся в кинематические.
    /// </summary>
    public static class AnimatedBodyFix
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Run()
        {
            int n = 0;
            foreach (var a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var rb = a.GetComponent<Rigidbody>();
                if (rb == null || rb.isKinematic) continue;
                rb.isKinematic = true;
                rb.useGravity = false;
                n++;
                Debug.Log("[Koks UI] Анимированный объект «" + a.name + "» переведён в кинематический (Rigidbody), чтобы физика не останавливала анимацию.");
            }
            if (n > 0) Debug.Log("[Koks UI] Исправлено анимированных Rigidbody: " + n);
        }
    }
}
