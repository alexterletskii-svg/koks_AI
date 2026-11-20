using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Окна в мире рисуются поверх всего, но луч руки упирается в стену/дверь/трубы раньше окна —
    /// и кнопки за преградой не нажимаются. Здесь ищется свободное расстояние перед игроком:
    /// окно ставится ближе преграды и пропорционально уменьшается (на глаз остаётся того же размера).
    /// </summary>
    public static class WindowFit
    {
        /// <summary>Свободное расстояние по направлению forward (с запасом по ширине окна: halfAngle влево/вправо и чуть вниз).</summary>
        public static float Free(Transform eye, Vector3 forward, float wanted, float halfAngle, float min = 0.6f)
        {
            float d = wanted;
            Transform rig = eye.root;
            float[] yaws = { 0f, -halfAngle, halfAngle, -halfAngle * 0.5f, halfAngle * 0.5f };
            float[] pitches = { 0f, 8f };
            foreach (float pitch in pitches)
                foreach (float yaw in yaws)
                {
                    Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * forward;
                    Vector3 right = Vector3.Cross(Vector3.up, dir);
                    if (right.sqrMagnitude > 0.0001f) dir = Quaternion.AngleAxis(pitch, right.normalized) * dir;
                    // к краям окна расстояние больше, чем до центра
                    float k = 1f / Mathf.Max(0.5f, Mathf.Cos(yaw * Mathf.Deg2Rad));
                    foreach (var h in Physics.RaycastAll(eye.position, dir, wanted * k + 0.3f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (h.collider == null) continue;
                        if (h.collider.transform.IsChildOf(rig)) continue;               // руки и тело игрока
                        if (h.collider.GetComponentInParent<Canvas>() != null) continue; // другие окна
                        d = Mathf.Min(d, h.distance / k - 0.2f);
                    }
                }
            return Mathf.Clamp(d, min, wanted);
        }
    }
}
