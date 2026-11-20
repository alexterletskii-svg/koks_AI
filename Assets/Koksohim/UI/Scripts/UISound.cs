using UnityEngine;

namespace KoksUI
{
    /// <summary>
    /// Короткий звук появления окна («динь»), собирается кодом — файлов и импорта не нужно.
    /// </summary>
    public static class UISound
    {
        static AudioClip pop;
        static AudioSource source;

        public static void WindowOpened(float volume = 0.5f)
        {
            if (pop == null) pop = Build();
            if (source == null)
            {
                var go = new GameObject("UISound");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.spatialBlend = 0f;   // 2D: слышно одинаково в любой точке
                source.playOnAwake = false;
            }
            source.PlayOneShot(pop, volume);
        }

        // два коротких тона (ми → ля), мягкая атака и затухание
        static AudioClip Build()
        {
            const int rate = 44100;
            float[] f = { 659.25f, 987.77f };
            float noteLen = 0.11f, tail = 0.16f;
            int n = (int)(rate * (noteLen * f.Length + tail));
            var data = new float[n];
            for (int k = 0; k < f.Length; k++)
            {
                int start = (int)(rate * noteLen * k);
                int len = (int)(rate * (noteLen + tail));
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = i / (float)rate;
                    float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-t * 14f);
                    float s = Mathf.Sin(2f * Mathf.PI * f[k] * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * f[k] * t);
                    data[start + i] += s * env * 0.35f;
                }
            }
            var clip = AudioClip.Create("WindowPop", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
