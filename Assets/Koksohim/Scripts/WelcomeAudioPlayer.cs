using System.Collections;
using UnityEngine;

public class WelcomeAudioPlayer : MonoBehaviour
{
    [Tooltip("Задержка перед воспроизведением в секундах")]
    public float delay = 3.0f;

    private AudioSource audioSource;

    void Start()
    {
        // Получаем компонент AudioSource на этом же объекте
        audioSource = GetComponent<AudioSource>();

        // Если компонента нет — добавляем его автоматически
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Запускаем корутину, которая подождет и включит звук
        StartCoroutine(PlayWelcomeAudioWithDelay());
    }

    IEnumerator PlayWelcomeAudioWithDelay()
    {
        // Ждем указанное количество секунд
        yield return new WaitForSeconds(delay);

        // Проверяем, что аудио назначено, и играем
        if (audioSource.clip != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("Не назначен AudioClip для приветственного аудио!");
        }
    }
}