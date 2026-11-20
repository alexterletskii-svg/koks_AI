using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Писк газоанализатора. До прибытия в газовый тоннель — тишина. В тоннеле — тихий писк;
    /// по мере приближения к клапанам (точка valvesPoint) плавно становится громче.
    /// Записи (clips) играют по очереди по кругу. Громкость и расстояния правятся в Inspector.
    /// </summary>
    public class GasAnalyzerBeep : MonoBehaviour
    {
        public AudioClip[] clips;
        public ArrivalTrigger tunnelTrigger;
        [Tooltip("Точка клапанов (круг PointPosition_Step2)")]
        public Transform valvesPoint;
        [Range(0f, 1f)] public float tunnelVolume = 0.08f;
        [Range(0f, 1f)] public float valvesVolume = 0.3f;
        [Tooltip("Дальше этого расстояния до клапанов (м) — тихий писк")]
        public float farDistance = 10f;
        [Tooltip("Ближе этого расстояния (м) — максимально громкий")]
        public float nearDistance = 2.5f;
        public float rampSeconds = 2f;

        AudioSource src;
        bool inTunnel;
        float vol;
        int idx = -1;

        void Start()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 0f; src.playOnAwake = false; src.loop = false; src.volume = 0f;
            if (tunnelTrigger != null) tunnelTrigger.onArrived.AddListener(() => inTunnel = true);
        }

        void Update()
        {
            float target = 0f;
            if (inTunnel)
            {
                target = tunnelVolume;
                var cam = Camera.main;
                if (valvesPoint != null && cam != null)
                {
                    Vector3 d = cam.transform.position - valvesPoint.position; d.y = 0f;
                    float k = Mathf.InverseLerp(farDistance, nearDistance, d.magnitude);
                    target = Mathf.Lerp(tunnelVolume, valvesVolume, Mathf.SmoothStep(0f, 1f, k));
                }
            }
            float step = Mathf.Max(0.0005f, Mathf.Abs(valvesVolume - tunnelVolume) / Mathf.Max(0.1f, rampSeconds) * Time.unscaledDeltaTime);
            vol = Mathf.MoveTowards(vol, target, step);
            src.volume = vol;
            if (inTunnel && clips != null && clips.Length > 0 && !src.isPlaying)
            {
                idx = (idx + 1) % clips.Length;
                if (clips[idx] != null) { src.clip = clips[idx]; src.Play(); }
            }
        }
    }
}
