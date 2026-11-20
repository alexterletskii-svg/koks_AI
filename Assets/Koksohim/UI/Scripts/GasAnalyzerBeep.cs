using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Писк газоанализатора. До прибытия в газовый тоннель — тишина. В тоннеле — тихий писк;
    /// по мере приближения к клапанам (точка valvesPoint) плавно становится громче.
    /// Писк не монотонный: между записями случайные паузы (у клапанов короче), каждая запись со своей громкостью,
    /// плюс медленное «дыхание» громкости. Всё правится в Inspector.
    /// </summary>
    public class GasAnalyzerBeep : MonoBehaviour
    {
        public AudioClip[] clips;
        public ArrivalTrigger tunnelTrigger;
        [Tooltip("Точка клапанов (круг PointPosition_Step2)")]
        public Transform valvesPoint;
        [Range(0f, 1f)] public float tunnelVolume = 0.025f;
        [Range(0f, 1f)] public float valvesVolume = 0.1f;
        [Tooltip("Дальше этого расстояния до клапанов (м) — тихий писк")]
        public float farDistance = 10f;
        [Tooltip("Ближе этого расстояния (м) — максимально громкий")]
        public float nearDistance = 2.5f;
        public float rampSeconds = 2f;
        [Tooltip("Пауза между записями, секунд (случайная от и до) — в тоннеле вдали от клапанов")]
        public Vector2 gapSeconds = new Vector2(3f, 9f);
        [Tooltip("Во сколько раз паузы короче у самих клапанов")]
        [Range(0.1f, 1f)] public float gapScaleAtValves = 0.35f;
        [Tooltip("Разброс громкости отдельных записей (множитель от и до)")]
        public Vector2 clipGain = new Vector2(0.4f, 1f);
        [Tooltip("Глубина медленного «дыхания» громкости (0 — нет)")]
        [Range(0f, 0.9f)] public float breathDepth = 0.5f;
        public float breathSpeed = 0.15f;

        AudioSource src;
        bool inTunnel;
        float vol;
        int idx = -1;
        float proximity;        // 0 — далеко от клапанов, 1 — у клапанов
        float gain = 1f;
        float nextAt = -1f;
        bool wasPlaying;

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
                    proximity = Mathf.SmoothStep(0f, 1f, k);
                    target = Mathf.Lerp(tunnelVolume, valvesVolume, proximity);
                }
            }
            float breath = 1f - breathDepth * (1f - Mathf.PerlinNoise(Time.time * breathSpeed, 7.3f));
            float step = Mathf.Max(0.0005f, Mathf.Abs(valvesVolume - tunnelVolume) / Mathf.Max(0.1f, rampSeconds) * Time.unscaledDeltaTime);
            vol = Mathf.MoveTowards(vol, target, step);
            src.volume = vol * gain * breath;

            if (!inTunnel || clips == null || clips.Length == 0) return;
            if (src.isPlaying) { wasPlaying = true; return; }
            if (wasPlaying)
            {
                wasPlaying = false;
                float g = Random.Range(gapSeconds.x, Mathf.Max(gapSeconds.x, gapSeconds.y));
                nextAt = Time.time + g * Mathf.Lerp(1f, gapScaleAtValves, proximity);
            }
            if (Time.time < nextAt) return;
            idx = (idx + 1) % clips.Length;
            if (clips[idx] == null) return;
            gain = Random.Range(clipGain.x, Mathf.Max(clipGain.x, clipGain.y));
            src.pitch = Random.Range(0.97f, 1.03f);
            src.clip = clips[idx];
            src.Play();
        }
    }
}
